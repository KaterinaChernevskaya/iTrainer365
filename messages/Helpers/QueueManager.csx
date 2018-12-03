using Microsoft.Bot.Builder.Azure;
using Microsoft.WindowsAzure.Storage; 
using Microsoft.WindowsAzure.Storage.Queue;

public class QueueManager
{
    public static async Task AddMessageToQueueAsync(string Message, string QueueName)
    {
        CloudStorageAccount StorageAccount = CloudStorageAccount.Parse(Utils.GetAppSetting("AzureWebJobsStorage"));
        CloudQueueClient QueueClient = StorageAccount.CreateCloudQueueClient();
        CloudQueue Queue = QueueClient.GetQueueReference(QueueName);
        
        await Queue.CreateIfNotExistsAsync();
        
        CloudQueueMessage QueueMessage = new CloudQueueMessage(Message);
        await Queue.AddMessageAsync(QueueMessage);
    }
}