#load "..\Models\Message.csx"

using Microsoft.Bot.Builder.Azure;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Connector.Teams.Models;
using System.Resources;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public class Proactive
{
    public static async Task<(ConnectorClient, IMessageActivity)> CreateMessageActivityAsync(Contact To, Contact BotChannel = null)
    {
        string ContactConversationId;
        ChannelAccount BotAccount;
        
        if(BotChannel != null)
        {
            BotAccount = new ChannelAccount(
                name: BotChannel.Name, 
                id: BotChannel.Id
            );
        }
        else
        {
            BotAccount = new ChannelAccount(
                name: Utils.GetAppSetting("BotAccountName_" + To.ChannelId), 
                id: Utils.GetAppSetting("BotAccountId_" + To.ChannelId)
            );
        }

        ChannelAccount UserAccount = new ChannelAccount(
            name: To.Name, 
            id: To.Id
        );
        
        if (!MicrosoftAppCredentials.IsTrustedServiceUrl(To.ServiceUrl))
        {
            MicrosoftAppCredentials.TrustServiceUrl(To.ServiceUrl);
        }
        
        ConnectorClient client = new ConnectorClient(new Uri(To.ServiceUrl));
        
        ConversationParameters parameters = new ConversationParameters
        {
            Bot = BotAccount,
            Members = new ChannelAccount[] { UserAccount }
        };
        
        if(To.ChannelData != null && To.ChannelData.Count > 0)
        {
            foreach( KeyValuePair<string, string> ContactChannelDataItem in To.ChannelData )
            {
                switch (ContactChannelDataItem.Key)
                {
                    case "TenantId":
                        parameters.ChannelData = new TeamsChannelData{ Tenant = new TenantInfo(ContactChannelDataItem.Value) };
                        break;
                    default:
                        break;
                }
            }
        }

        if(string.IsNullOrEmpty(To.ConversationId))
        {
            ConversationResourceResponse conversation = await client.Conversations.CreateConversationAsync(parameters);
            ContactConversationId = conversation.Id;
        }
        else
        {
            ContactConversationId = To.ConversationId;
        }
        
        IMessageActivity messageActivity = null;
        messageActivity = Activity.CreateMessageActivity();
        messageActivity.From = BotAccount;
        messageActivity.Recipient = UserAccount;
        messageActivity.Conversation = new ConversationAccount(id: ContactConversationId);
        messageActivity.ServiceUrl = To.ServiceUrl;
        messageActivity.ChannelId = To.ChannelId;
        messageActivity.Locale = Utils.GetAppSetting("Locale");

        return (client, messageActivity);
    }

    public static async Task SendSubscriptionMessageAsync(Contact To, string functionDirectory)
    {
        (ConnectorClient, IMessageActivity) MessageActivity = await Proactive.CreateMessageActivityAsync(To);
        ConnectorClient client = MessageActivity.Item1;
        IMessageActivity messageActivity = MessageActivity.Item2;

        Exercise TodayExcercise = new Exercise(functionDirectory);
        await TodayExcercise.GetTodayExcerciseAsync();

        if(TodayExcercise.Id > 0)
        {
            List<CardAction> Buttons = new List<CardAction>();

            foreach (ExerciseAnswer Answer in TodayExcercise.Answers)
            {
                Buttons.Add(Answer.Button);
            }

            Attachment CardAttachment = new HeroCard
            {
                Title = TodayExcercise.Title,
                Subtitle = TodayExcercise.Subtitle,
                Text = Formatter.Format(TodayExcercise.TaskText, messageActivity.ChannelId, messageActivity.ChannelId == "telegram" || messageActivity.ChannelId == "webchat" ? false : true),
                Buttons = Buttons
            }.ToAttachment();

            messageActivity.Attachments.Add(CardAttachment);
        }

        await client.Conversations.SendToConversationAsync((Activity)messageActivity);
    } 

    public static async Task SendFeedbackAdminOutMessageAsync(Message Message, string functionDirectory)
    {
        ResourceManager LocalizationFeedbackAdminOut = ResourceManager.CreateFileBasedResourceManager("FeedbackAdminOut", Path.Combine(functionDirectory, "Resources\\Localization"), null);
        
        (ConnectorClient, IMessageActivity) MessageActivity = await Proactive.CreateMessageActivityAsync(Message.To, Message.BotChannel);
        ConnectorClient client = MessageActivity.Item1;
        IMessageActivity messageActivity = MessageActivity.Item2;

        messageActivity.Text = string.Format(LocalizationFeedbackAdminOut.GetString("MessageText"), Message.From.Name, Message.From.Id, Message.From.ChannelId, Message.Text);
        messageActivity.Locale = LocalizationFeedbackAdminOut.GetString("MessageLocale");
        messageActivity.ChannelData = JsonConvert.SerializeObject(
            new JObject(
                new JProperty(
                "subject",
                string.Format(LocalizationFeedbackAdminOut.GetString("MessageSubject"), Message.FeedbackQuestion)
                )
            )
        );

        await client.Conversations.SendToConversationAsync((Activity)messageActivity);
    }

    public static async Task SendFeedbackUserOutMessageAsync(Message Message, string functionDirectory)
    {
        ResourceManager LocalizationFeedbackUserOut = ResourceManager.CreateFileBasedResourceManager("FeedbackUserOut", Path.Combine(functionDirectory, "Resources\\Localization"), null);
        
        FeedbackQuestion CurrentQuestion = new FeedbackQuestion();
        await CurrentQuestion.GetQuestionByIdAsync(Message.FeedbackQuestion);

        (ConnectorClient, IMessageActivity) MessageActivity = await Proactive.CreateMessageActivityAsync(Message.To);
        ConnectorClient client = MessageActivity.Item1;
        IMessageActivity messageActivity = MessageActivity.Item2;

        messageActivity.Text = string.Format(LocalizationFeedbackUserOut.GetString("MessageTextQuestion"), CurrentQuestion.Id, CurrentQuestion.Question);
        await client.Conversations.SendToConversationAsync((Activity)messageActivity);

        messageActivity.Text = string.Format(LocalizationFeedbackUserOut.GetString("MessageTextAnswer"), Message.Text);
        await client.Conversations.SendToConversationAsync((Activity)messageActivity);
    }
}