using Newtonsoft.Json;

[Serializable]
public class EmailMessage
{
    [JsonProperty(PropertyName="html_body")]
    public string HtmlBody { get; set; }
    [JsonProperty(PropertyName="subject")]
    public string Subject { get; set; }
    [JsonProperty(PropertyName="importance")]
    public string Importance { get; set; }
}