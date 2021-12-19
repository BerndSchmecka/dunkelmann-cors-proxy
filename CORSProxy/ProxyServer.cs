using System;
using System.Net;
using System.Threading.Tasks;
using System.Text;
using System.Collections.Generic;
using System.Threading;
using Newtonsoft.Json;
using Rebex.Security.Cryptography;

namespace CORSProxy {
        public struct ResponseData {
        public HttpListenerResponse Response {get;}
        public byte[] Data {get;}
        public string ContentType {get;}
        public int StatusCode {get;}

        public ResponseData(HttpListenerResponse resp, string data_to_send, string contentType, int statusCode){
            this.Response = resp;
            this.Data = Encoding.UTF8.GetBytes(data_to_send);
            this.ContentType = contentType;
            this.StatusCode = statusCode;

            this.Response.ContentType = this.ContentType;
            this.Response.ContentEncoding = Encoding.UTF8;
            this.Response.ContentLength64 = this.Data.LongLength;
            this.Response.StatusCode = this.StatusCode;
        }
    }

    class ResponseException : Exception {
        public ResponseData ResponseMessage {get; private set;}

        public ResponseException(ResponseData rd){
            this.ResponseMessage = rd;
            //this.Message = JsonConvert.SerializeObject(rd);
        }
    }

    class ProxyServer {
        private static string url = "http://*:7779/";

        private async Task Listen(string prefix, int maxConcurrentRequests, CancellationToken token){
            HttpListener listener = new HttpListener();
            listener.Prefixes.Add(prefix);
            listener.Start();

            var requests = new HashSet<Task>();
            for(int i=0; i < maxConcurrentRequests; i++)
                requests.Add(listener.GetContextAsync());

            while (!token.IsCancellationRequested){
                Task t = await Task.WhenAny(requests);
                requests.Remove(t);

                if (t is Task<HttpListenerContext>){
                    var context = (t as Task<HttpListenerContext>).Result;
                    requests.Add(HandleInboundConnections(context));
                    requests.Add(listener.GetContextAsync());
                }
            }
        }

        private async Task HandleInboundConnections(HttpListenerContext context) {
                HttpListenerRequest request = context.Request;
                HttpListenerResponse response = context.Response;

                response.AddHeader("Access-Control-Allow-Origin", "*");
                response.AddHeader("X-Powered-By", Program.displayableVersion);

                ResponseData rd = new ResponseData(response, Program.ERROR_TEMPLATE("400 Bad Request"), "text/html", 400);

                if(request.HttpMethod == "GET"){
                    try {
                        var decodedObj = System.Convert.FromBase64String(request.Url.AbsolutePath.Substring(1));
                        TokenObject? deserializedObj = JsonConvert.DeserializeObject<TokenObject>(Encoding.UTF8.GetString(decodedObj));

                        Ed25519 ed = new Ed25519();
                        ed.FromPublicKey(System.Convert.FromBase64String(Program.PUBLIC_KEY));
 
                        if(!ed.VerifyMessage(Encoding.UTF8.GetBytes(deserializedObj.url), System.Convert.FromBase64String(deserializedObj.signature))){
                            rd = new ResponseData(response, Program.ERROR_TEMPLATE("401 Invalid Signature"), "text/html", 401);
                            throw new ResponseException(rd);
                        }

                        HttpWebRequest req = HttpWebRequest.CreateHttp(String.Format(deserializedObj.url, deserializedObj.args));

                        req.UserAgent = "Mozilla/5.0";

                        using(HttpWebResponse resp = (HttpWebResponse)req.GetResponse()){
                            StreamReader reader = new StreamReader(resp.GetResponseStream(), Encoding.UTF8);
                            String responseString = reader.ReadToEnd();

                            rd = new ResponseData(response, responseString, resp.ContentType, (int)resp.StatusCode);
                        }
                    } catch (ResponseException rex) {
                        rd = rex.ResponseMessage;
                    } catch (Exception ex) {
                        rd = new ResponseData(response, "{\"errorMessage\": \"" + ex.Message +"\"}", "application/json", 500);
                    }
                } else if (request.HttpMethod == "OPTIONS"){
                    rd = new ResponseData(response, Program.ERROR_TEMPLATE("405 Method Not Allowed"), "text/html", 405);
                } else if (request.HttpMethod == "POST") {
                    rd = new ResponseData(response, Program.ERROR_TEMPLATE("405 Method Not Allowed"), "text/html", 405);
                } else {
                    rd = new ResponseData(response, Program.ERROR_TEMPLATE("405 Method Not Allowed"), "text/html", 405);
                }
                
                await response.OutputStream.WriteAsync(rd.Data, 0, rd.Data.Length);
                response.Close();
        }

        public void StartServer(){
            CancellationToken token = new CancellationToken();
            Task listenTask = Listen(url, 32, token);
            listenTask.GetAwaiter().GetResult();
        }
    }
}