using Newtonsoft.Json;

[Serializable]
public class Contact
{
    [JsonProperty(PropertyName="id")]
    public string Id { get; set; }
    [JsonProperty(PropertyName="name")]
    public string Name { get; set; }
    [JsonProperty(PropertyName="conversation_id")]
    public string ConversationId { get; set; }
    [JsonProperty(PropertyName="service_url")]
    public string ServiceUrl { get; set; }
    [JsonProperty(PropertyName="channel_id")]
    public string ChannelId { get; set; }
    [JsonProperty(PropertyName="channel_data")]
    public Dictionary<string, string> ChannelData { get; set; }
}