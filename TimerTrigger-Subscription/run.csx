using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using Microsoft.Bot.Builder.Azure;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using Newtonsoft.Json;


public enum Direction { ToAdmin, ToUser }

public enum MessageType { Feedback, Subscription }

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

public static void Run(TimerInfo myTimer, TraceWriter log)
{
    log.Info($"C# Timer trigger function executed at: {DateTime.Now}");
    
    string UserListQuery = @"
        BEGIN
            SELECT 
                [dbo].[Users].[Id] as [Id],
                [dbo].[Users].[ChannelId] as [ChannelId],
                [dbo].[Users].[ChannelAccountId] as [ChannelAccountId],
                [dbo].[Users].[ChannelAccountName] as [ChannelAccountName],
                [dbo].[Subscription].[Conversation] as [ConversationId],
                [dbo].[Subscription].[ServiceUrl] as [ServiceUrl]
            FROM 
                [dbo].[Subscription]
            LEFT JOIN
                [dbo].[Users] ON [dbo].[Users].[Id] = [dbo].[Subscription].[User]
            WHERE
                [dbo].[Subscription].[Hour] IS NOT NULL
                AND
                ((DATEPART(HOUR, GETDATE()) + [dbo].[Users].[Timezone] = 25 AND [dbo].[Subscription].[Hour] = 1) OR [dbo].[Subscription].[Hour] = DATEPART(HOUR, GETDATE()) + [dbo].[Users].[Timezone])
                AND
                [dbo].[Subscription].[Locale] = @Locale
            ;
        END
    ";

    string ChannelDataListQuery = @"
        BEGIN
            SELECT 
                [dbo].[ChannelData].[ChannelDataKey] as [ChannelDataKey],
                [dbo].[ChannelData].[ChannelDataValue] as [ChannelDataValue]
            FROM 
                [dbo].[ChannelData]
            WHERE
                [dbo].[ChannelData].[User] = @User
            ;
        END
    ";

    string SQLConnectionString = ConfigurationManager.ConnectionStrings["SQLConnectionString"].ConnectionString;

    using (SqlConnection conn = new SqlConnection(SQLConnectionString))
    {
        conn.Open();
        try
        {
            using (SqlCommand UserListCommand = new SqlCommand{
                CommandType = CommandType.Text,
                Connection = conn,
                CommandTimeout = 300,
                CommandText = UserListQuery})
            {
                UserListCommand.Parameters.Add("@Locale", SqlDbType.NVarChar, 255).Value = (object)Utils.GetAppSetting("Locale") ?? DBNull.Value;
                using (SqlDataReader UserListReader = UserListCommand.ExecuteReader())
                {
                    if(UserListReader.HasRows)
                    {
                        while(UserListReader.Read())
                        {  
                            Contact To = new Contact{
                                Id = !UserListReader.IsDBNull(UserListReader.GetOrdinal("ChannelAccountId")) ? UserListReader.GetString(UserListReader.GetOrdinal("ChannelAccountId")) : "",
                                Name = !UserListReader.IsDBNull(UserListReader.GetOrdinal("ChannelAccountName")) ? UserListReader.GetString(UserListReader.GetOrdinal("ChannelAccountName")) : "",
                                ConversationId = !UserListReader.IsDBNull(UserListReader.GetOrdinal("ConversationId")) ? UserListReader.GetString(UserListReader.GetOrdinal("ConversationId")) : "",
                                ServiceUrl = !UserListReader.IsDBNull(UserListReader.GetOrdinal("ServiceUrl")) ? UserListReader.GetString(UserListReader.GetOrdinal("ServiceUrl")) : "",
                                ChannelId = !UserListReader.IsDBNull(UserListReader.GetOrdinal("ChannelId")) ? UserListReader.GetString(UserListReader.GetOrdinal("ChannelId")) : ""
                            };

                            using (SqlConnection conn2 = new SqlConnection(SQLConnectionString))
                            {
                                conn2.Open();
                                try
                                {
                                    using (SqlCommand ChannelDataListCommand = new SqlCommand{
                                        CommandType = CommandType.Text,
                                        Connection = conn2,
                                        CommandTimeout = 300,
                                        CommandText = ChannelDataListQuery})
                                    {
                                        ChannelDataListCommand.Parameters.Add("@User", SqlDbType.Int).Value = !UserListReader.IsDBNull(UserListReader.GetOrdinal("Id")) ? UserListReader.GetInt32(UserListReader.GetOrdinal("Id")) : 0;
                                        
                                        using (SqlDataReader ChannelDataListReader = ChannelDataListCommand.ExecuteReader())
                                        {
                                            if(ChannelDataListReader.HasRows)
                                            {
                                                while(ChannelDataListReader.Read())
                                                {
                                                    To.ChannelData = new Dictionary<string, string>(){ { !ChannelDataListReader.IsDBNull(ChannelDataListReader.GetOrdinal("ChannelDataKey")) ? ChannelDataListReader.GetString(ChannelDataListReader.GetOrdinal("ChannelDataKey")) : "NoKey", !ChannelDataListReader.IsDBNull(ChannelDataListReader.GetOrdinal("ChannelDataValue")) ? ChannelDataListReader.GetString(ChannelDataListReader.GetOrdinal("ChannelDataValue")) : "NoKey" } };
                                                }
                                            }
                                        }
                                    }
                                }
                                catch (System.Data.SqlClient.SqlException ex)
                                {
                                    throw new Exception(ex.Message);
                                }
                                conn2.Close();
                            }

                            Message QueueMessage = new Message
                            {
                                To = To,
                                Type = MessageType.Subscription
                            };

                            log.Info($"The message was sent to: {To.Name} {To.Id}@{To.ChannelId}");

                            AddMessageToQueue(JsonConvert.SerializeObject(QueueMessage));
                        }
                    }
                    else
                    {
                            
                    }
                }
            }
        }
        catch (System.Data.SqlClient.SqlException ex)
        {
            throw new Exception(ex.Message);
        }
        conn.Close();
    }
}

private static void AddMessageToQueue(string Message)
{
    CloudStorageAccount StorageAccount = CloudStorageAccount.Parse(Utils.GetAppSetting("AzureWebJobsStorage"));
    CloudQueueClient QueueClient = StorageAccount.CreateCloudQueueClient();
    CloudQueue Queue = QueueClient.GetQueueReference(Utils.GetAppSetting("MessageSubscriptionQueue"));
        
    Queue.CreateIfNotExists();
        
    CloudQueueMessage QueueMessage = new CloudQueueMessage(Message);
    Queue.AddMessage(QueueMessage);
}