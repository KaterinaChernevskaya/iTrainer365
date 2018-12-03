#load "..\Models\Exercise.csx"

using System.Resources;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;

// For more information about this template visit http://aka.ms/azurebots-csharp-basic
[Serializable]
public class ExerciseDialog : IDialog<object>
{
    private string functionDirectory;
    private ResourceManager LocalizationExerciseDialog;
    private Exercise TodayExcercise;
    private User user;
    ExerciseAnswerValue CurrentExerciseAnswerValue;
    
    public ExerciseDialog(string functionDirectory, User user, ExerciseAnswerValue CurrentExerciseAnswerValue = null)
    {
        this.functionDirectory = functionDirectory;
        this.user = user;
        this.CurrentExerciseAnswerValue = CurrentExerciseAnswerValue;
        this.LocalizationExerciseDialog = ResourceManager.CreateFileBasedResourceManager("ExerciseDialog", Path.Combine(functionDirectory, "Resources\\Localization"), null);
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

        if(this.CurrentExerciseAnswerValue == null)
        {
            await context.PostAsync(this.LocalizationExerciseDialog.GetString("Welcome"));
            if(await PostExerciseAsync(context) == false)
            {
                await context.PostAsync(this.LocalizationExerciseDialog.GetString("NoExercise"));
            }
        }
        else
        {
            await this.PostResultAsync(context);
        }

        context.Done(String.Empty);
    }

    private async Task<bool> PostExerciseAsync(IDialogContext context)
    {
        this.TodayExcercise = new Exercise(this.functionDirectory);
        await this.TodayExcercise.GetTodayExcerciseAsync();

        if(this.TodayExcercise.Id > 0)
        {
            IMessageActivity ExcerciseMessage = context.MakeMessage();

            List<CardAction> Buttons = new List<CardAction>();

            foreach (ExerciseAnswer Answer in this.TodayExcercise.Answers)
            {
                Buttons.Add(Answer.Button);
            }

            Attachment CardAttachment = new HeroCard
            {
                Title = this.TodayExcercise.Title,
                Subtitle = this.TodayExcercise.Subtitle,
                Text = Formatter.Format(this.TodayExcercise.TaskText, this.user.ChannelId, this.user.ChannelId == "telegram" || this.user.ChannelId == "webchat" ? false : true),
                Buttons = Buttons
            }.ToAttachment();

            ExcerciseMessage.Attachments.Add(CardAttachment);
            await context.PostAsync(ExcerciseMessage);
            return true;
        }
        else
        {
            return false;
        }
    }
    
    private async Task PostResultAsync(IDialogContext context)
    {
        Exercise CurrentExcercise = new Exercise(this.functionDirectory);
        await CurrentExcercise.GetExcerciseByIdAsync(this.CurrentExerciseAnswerValue.Excercise);
        string messageSecond = "";
        
        if(this.CurrentExerciseAnswerValue.Answer > 0 & this.CurrentExerciseAnswerValue.Answer <= CurrentExcercise.Answers.Count)
        {
            string messageFirst = CurrentExcercise.Answers[this.CurrentExerciseAnswerValue.Answer-1].AnswerText;

            if(await CurrentExcercise.AddAnswerAsync(this.CurrentExerciseAnswerValue.Answer, user) == false)
            {
                messageFirst += " <i>" + this.LocalizationExerciseDialog.GetString("AnswerResultAlreadyExists") + "</i>";
                messageSecond = this.LocalizationExerciseDialog.GetString("AnswerResultAlreadyExistsNextExerciseTomorrow");
            }
            else
            {
                messageSecond = this.LocalizationExerciseDialog.GetString("AnswerResult");
            }

            messageFirst = Formatter.Format(messageFirst, this.user.ChannelId);

            await context.PostAsync(messageFirst);
        }
        else
        {
            await context.PostAsync(this.LocalizationExerciseDialog.GetString("AnswerResultNoAnswerFound"));
        }

        messageSecond = Formatter.Format(messageSecond, this.user.ChannelId);

        await context.PostAsync(messageSecond);
        context.Done(String.Empty);
    }
}