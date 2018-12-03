using Microsoft.Bot.Builder.Azure;

public static async Task Run(TimerInfo myTimer, TraceWriter log)
{
    log.Info($"C# Timer trigger function executed at: {DateTime.Now}");
    using(HttpClient client = new HttpClient())
    {
        client.BaseAddress = new Uri(Utils.GetAppSetting("TimerTriggerPingUrl"));
        HttpResponseMessage result = await client.GetAsync("");
        log.Info($"Server Response Code: {result.StatusCode}");
    }
}