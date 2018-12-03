using Newtonsoft.Json;

public enum Direction { ToAdmin, ToUser }

public enum MessageType { Feedback, Subscription }

[Serializable]
public class Message
{
    [JsonProperty(PropertyName="to")]
    public Contact To { get; set; }
    [JsonProperty(PropertyName="from")]
    public Contact From { get; set; }
    [JsonProperty(PropertyName="bot_channel")]
    public Contact BotChannel { get; set; }
    [JsonProperty(PropertyName="feedback_question_id")]
    public int FeedbackQuestion { get; set; }
    [JsonProperty(PropertyName="text")]
    public string Text { get; set; }
    [JsonProperty(PropertyName="direction")]
    public Direction Direction { get; set; }
    [JsonProperty(PropertyName="type")]
    public MessageType Type { get; set; }
}