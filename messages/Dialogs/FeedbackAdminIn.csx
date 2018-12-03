#load "..\Models\Contact.csx"
#load "..\Models\Message.csx"
#load "..\Models\FeedbackQuestion.csx"
#load "..\Models\FeedbackAnswer.csx"
#load "..\Helpers\QueueManager.csx"

using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using Microsoft.Bot.Builder.Azure;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using Newtonsoft.Json;

// For more information about this template visit http://aka.ms/azurebots-csharp-basic
[Serializable]
public class FeedbackAdminInDialog : IDialog<object>
{
    private FeedbackQuestion CurrentFeedbackQuestion;
    private FeedbackAnswer CurrentFeedbackAnswer;
    private int FeedbackQuestionId;
    private string FeedbackMessage;
    
    public FeedbackAdminInDialog(int FeedbackQuestionId, string FeedbackMessage)
    {
        this.FeedbackQuestionId = FeedbackQuestionId;
        this.FeedbackMessage = FeedbackMessage; 
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

        this.CurrentFeedbackQuestion = new FeedbackQuestion();
        await this.CurrentFeedbackQuestion.GetQuestionByIdAsync(FeedbackQuestionId);

        if(this.CurrentFeedbackQuestion.Id == 0)
        {
            throw new Exception("Incorrect FeedbackQuestion");
        }

        this.CurrentFeedbackAnswer = new FeedbackAnswer();
        this.CurrentFeedbackAnswer.Question = this.FeedbackQuestionId;
        this.CurrentFeedbackAnswer.Answer = this.FeedbackMessage;
        await this.CurrentFeedbackAnswer.CreateFeedbackAnswerAsync();

        StateClient StateClient = message.GetStateClient();
        BotData ConversationData = await StateClient.BotState.GetConversationDataAsync(CurrentFeedbackQuestion.ChannelId, CurrentFeedbackQuestion.Conversation);
        Contact To = ConversationData.GetProperty<Contact>("UserData");
        Contact BotUserChannel = ConversationData.GetProperty<Contact>("BotUserData");
        
        Contact From = new Contact
        {
            Id = message.From.Id,
            Name = message.From.Name,
            ConversationId = message.Conversation.Id,
            ServiceUrl = message.ServiceUrl,
            ChannelId = message.ChannelId
        };

        Message QueueMessage = new Message
        {
            To = To,
            From = From,
            BotChannel = BotUserChannel,
            FeedbackQuestion = this.CurrentFeedbackQuestion.Id,
            Text = this.FeedbackMessage,
            Direction = Direction.ToUser
        };

        await QueueManager.AddMessageToQueueAsync(JsonConvert.SerializeObject(QueueMessage), Utils.GetAppSetting("MessageFeedbackQueue"));
        
        context.Done(String.Empty);
    }
}