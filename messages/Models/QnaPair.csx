using System.Text;
using System.Web;
using System.Net.Http.Headers;
using Microsoft.Bot.Builder.Azure;
using Microsoft.Bot.Builder.CognitiveServices.QnAMaker;
using Newtonsoft.Json;

[Serializable]
public class QnaPairs
{
    [JsonProperty(PropertyName="qnaList")]
    public List<QnaPair> QnaPairList { get; set; }
}

[Serializable]
public class QnaPair
{
    [JsonProperty(PropertyName="answer")]
    public string Answer { get; set; }
    [JsonProperty(PropertyName="questions")]
    public List<string> Questions { get; set; }
    [JsonProperty(PropertyName="id")]
    public int Id { get; set; }
    [JsonProperty(PropertyName="source")]
    public string Source { get; set; }
    [JsonProperty(PropertyName="metadata")]
    public List<string> Metadata { get; set; }
}

[Serializable]
public class QnaPairAdd
{
    [JsonProperty(PropertyName="add")]
    public QnaPairs Add { get; set; }
    private Uri UriQnaPairsToAdd;

    public QnaPairAdd(List<QnaPair> QnaPairList)
    {
        this.UriQnaPairsToAdd = new Uri($"https://westus.api.cognitive.microsoft.com/qnamaker/v4.0/knowledgebases/{Utils.GetAppSetting("QnAKnowledgebaseId")}");
        this.Add = new QnaPairs(){
            QnaPairList = QnaPairList
        };
    }

    public async Task AddQnaPairsAsync()
    {
        using (HttpClient client = new HttpClient())
        {
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", Utils.GetAppSetting("QnASubscriptionKey"));
            HttpRequestMessage request = new HttpRequestMessage(){
                Method = new HttpMethod("PATCH"), 
                RequestUri = this.UriQnaPairsToAdd,
                Content = new StringContent(
                    JsonConvert.SerializeObject(this),
                    Encoding.UTF8, 
                    "application/json"
                )
            };
            
            try
            {
                await client.SendAsync(request);
            }
            catch (HttpRequestException)
            {
            }
        }
    }
}