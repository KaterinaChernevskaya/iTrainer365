using System;
using System.Net;
using System.Net.Http;
using Microsoft.Azure.WebJobs.Host;


public class BotMessage
{
    public string Source { get; set; } 
    public string Message { get; set; }
}

// Bot Storage: Register the optional private state storage for your bot. 

// For Azure Table Storage, set the following environment variables in your bot app:
// -UseTableStorageForConversationState set to 'true'
// -AzureWebJobsStorage set to your table connection string

// For CosmosDb, set the following environment variables in your bot app:
// -UseCosmosDbForConversationState set to 'true'
// -CosmosDbEndpoint set to your cosmos db endpoint
// -CosmosDbKey set to your cosmos db key

public static HttpResponseMessage Run(string QueueItem, out BotMessage message, TraceWriter log)
{
    log.Info($"Sending Bot message {QueueItem}");  

    message = new BotMessage
    { 
        Source = "Azure Functions (C#)!", 
        Message = QueueItem
    };

    return new HttpResponseMessage(HttpStatusCode.OK); 
}