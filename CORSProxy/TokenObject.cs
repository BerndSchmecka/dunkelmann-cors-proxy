using System.Text.Json.Serialization;

namespace CORSProxy {

[Serializable]
public class TokenObject {

    [JsonPropertyName("url")]
    public string url {get; set;}

    [JsonPropertyName("signature")]
    public string signature {get; set;}

    [JsonPropertyName("args")]
    public string[] args {get; set;}

    public TokenObject(string url, string signature, string[] args) {
        this.url = url;
        this.signature = signature;
        this.args = args;
    }
}

}