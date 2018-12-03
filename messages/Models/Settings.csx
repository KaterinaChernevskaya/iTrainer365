using Newtonsoft.Json;

[Serializable]
public class Settings
{
    [JsonProperty(PropertyName="timezone")]
    public string Timezone { get; set; }
}