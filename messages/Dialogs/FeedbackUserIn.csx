#load "..\Models\Contact.csx"
#load "..\Models\Message.csx"
#load "..\Helpers\QueueManager.csx"

using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Resources;
using Microsoft.Bot.Builder.Azure;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Connector.Teams.Models;
using Newtonsoft.Json;

// For more information about this template visit http://aka.ms/azurebots-csharp-basic
[Serializable]
public class FeedbackUserInDialog : IDialog<object>
{
    private ResourceManager LocalizationFeedbackUserInDialog;
    private string FeedbackMessage;
    private User user;
    private FeedbackQuestion CurrentFeedbackQuestion;
    
    public FeedbackUserInDialog(string FeedbackMessage, string functionDirectory, User user)
    {
        this.FeedbackMessage = FeedbackMessage.Trim();
        this.user = user;
        this.LocalizationFeedbackUserInDialog = ResourceManager.CreateFileBasedResourceManager("FeedbackUserInDialog", Path.Combine(functionDirectory, "Resources\\Localization"), null);
    }

    public Task StartAsync(IDialogContext context)
    {
        try
        {
            context.Wait(MessageReceivedAsync);
        }
        catch (OperationCanceledException error)
        {
            return Task.FromCanceled(error.CancellationToken);
        }
        catch (Exception error)
        {
            return Task.FromException(error);
        }

        return Task.CompletedTask;
    }

    public virtual async Task MessageReceivedAsync(IDialogContext context, IAwaitable<IMessageActivity> argument)
    {
        IMessageActivity message = await argument;

        if(!String.IsNullOrEmpty(this.FeedbackMessage))
        {
            await ProceedFeedbackQuestionAsync(context);
        }
        else
        {
            PromptDialog.Text(context, GetFeedbackMessageAsync, this.LocalizationFeedbackUserInDialog.GetString("QuestionListening"));
        }
    }

    private async Task GetFeedbackMessageAsync(IDialogContext context, IAwaitable<string> prompt)
    {
        string reply  = (await prompt).Trim();

        if(reply.ToLower() == this.LocalizationFeedbackUserInDialog.GetString("Cancel") || String.IsNullOrEmpty(reply))
        {
            await context.PostAsync(this.LocalizationFeedbackUserInDialog.GetString("QuestionCanceled"));
            context.Done(String.Empty);
            return;
        }
        
        this.FeedbackMessage = reply;
        await ProceedFeedbackQuestionAsync(context);
    }

    private async Task ProceedFeedbackQuestionAsync(IDialogContext context)
    {
        this.CurrentFeedbackQuestion = new FeedbackQuestion();
        this.CurrentFeedbackQuestion.Question = this.FeedbackMessage;
        this.CurrentFeedbackQuestion.Conversation = context.Activity.Conversation.Id;
        await this.CurrentFeedbackQuestion.CreateFeedbackQuestionAsync(this.user);

        Contact To = new Contact
        {
            Id = Utils.GetAppSetting("AdminChannelAccountId"),
            Name = Utils.GetAppSetting("AdminChannelAccountName"),
            ConversationId = null,
            ServiceUrl = Utils.GetAppSetting("AdminServiceUrl"),
            ChannelId = Utils.GetAppSetting("AdminChannelId")
        };

        Contact BotAdminChannel = new Contact
        {
            Id = Utils.GetAppSetting("BotAccountId_" + Utils.GetAppSetting("AdminChannelId")), 
            Name = Utils.GetAppSetting("BotAccountName_" + Utils.GetAppSetting("AdminChannelId"))
        };

        Contact From = new Contact
        {
            Id = context.Activity.From.Id,
            Name = context.Activity.From.Name,
            ConversationId = context.Activity.Conversation.Id,
            ServiceUrl = context.Activity.ServiceUrl,
            ChannelId = context.Activity.ChannelId
        };

        Contact BotUserChannel = new Contact
        {
            Id = context.Activity.Recipient.Id,
            Name = context.Activity.Recipient.Name,
            ConversationId = context.Activity.Conversation.Id,
            ServiceUrl = context.Activity.ServiceUrl,
            ChannelId = context.Activity.ChannelId
        };

        Message QueueMessage = new Message
        {
            To = To,
            From = From,
            BotChannel = BotAdminChannel,
            FeedbackQuestion = this.CurrentFeedbackQuestion.Id,
            Text = this.FeedbackMessage,
            Direction = Direction.ToAdmin
        };

        StateClient StateClient = context.Activity.GetStateClient();
        BotData ConversationData = await StateClient.BotState.GetConversationDataAsync(context.Activity.ChannelId, context.Activity.Conversation.Id);
        ConversationData.SetProperty<Contact>("UserData", From);
        ConversationData.SetProperty<Contact>("BotUserData", BotUserChannel);
        await StateClient.BotState.SetConversationDataAsync(context.Activity.ChannelId, context.Activity.Conversation.Id, ConversationData);

        await QueueManager.AddMessageToQueueAsync(JsonConvert.SerializeObject(QueueMessage), Utils.GetAppSetting("MessageFeedbackQueue"));
        
        await context.PostAsync(string.Format(this.LocalizationFeedbackUserInDialog.GetString("QuestionFinished"), this.CurrentFeedbackQuestion.Id));
        context.Done(String.Empty);
    }
}