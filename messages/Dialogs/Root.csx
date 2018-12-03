#load "..\Dialogs\Exercise.csx"
#load "..\Dialogs\Test.csx"
#load "..\Dialogs\Company.csx"
#load "..\Dialogs\Settings.csx"
#load "..\Dialogs\FeedbackUserIn.csx"
#load "..\Dialogs\FeedbackAdminIn.csx"
#load "..\Dialogs\Progress.csx"
#load "..\Dialogs\BasicQnAMakerDialog.csx"
#load "..\Models\EmailMessage.csx"

using System.Threading;
using System.Resources;
using System.Text.RegularExpressions;
using Microsoft.Bot.Builder.Azure;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using Newtonsoft.Json;

// For more information about this template visit http://aka.ms/azurebots-csharp-basic
[Serializable]
public class RootDialog : IDialog<object>
{
    private string functionDirectory;
    private User user;
    private ResourceManager LocalizationRootDialog;

    public RootDialog(string functionDirectory, User user)
    {
        this.functionDirectory = functionDirectory;
        this.user = user;
        this.LocalizationRootDialog = ResourceManager.CreateFileBasedResourceManager("RootDialog", Path.Combine(functionDirectory, "Resources\\Localization"), null);
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

        ExerciseAnswerValue CurrentExerciseAnswerValue = null;
        TestQuestionAnswerValue CurrentTestQuestionAnswerValue = null;

        string AdminChannelId = Utils.GetAppSetting("AdminChannelId");
        string AdminChannelAccountId = Utils.GetAppSetting("AdminChannelAccountId");

        if(AdminChannelId == message.ChannelId && AdminChannelAccountId == message.From.Id)
        {
            Match MatchQuestion;

            switch (message.ChannelId)
            {
                case "email":
                    EmailMessage Email = message.GetChannelData<EmailMessage>();
                    MatchQuestion = Regex.Match(Email.Subject, @"(?<=" + this.LocalizationRootDialog.GetString("CommandQuestionAnswer") + @" #)\d+", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline);
                    break;

                default:
                    MatchQuestion = Regex.Match(message.Text, @"(?<=" + this.LocalizationRootDialog.GetString("CommandQuestionAnswer") + @" #)\d+", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline);
                    break;
            }

            if(MatchQuestion.Success && !String.IsNullOrEmpty(message.Text.Trim()))
            {
                int FeedbackQuestionId;

                if(!Int32.TryParse(MatchQuestion.Value, out FeedbackQuestionId))
                {
                    FeedbackQuestionId = 0;
                }

                await context.Forward(new FeedbackAdminInDialog(FeedbackQuestionId, message.Text.Trim()), ResumeAfterChildDialogAsync, message, CancellationToken.None);
            }
            else
            {
                await context.PostAsync(this.LocalizationRootDialog.GetString("CommandQuestionAnswerNotFound"));
                context.Done(String.Empty);
            }
        }
        else
        {
            if(!string.IsNullOrEmpty(message.Value?.ToString()))
            {
                try
                {
                    CurrentExerciseAnswerValue = JsonConvert.DeserializeObject<ExerciseAnswerValue>(message.Value.ToString());
                    CurrentTestQuestionAnswerValue = JsonConvert.DeserializeObject<TestQuestionAnswerValue>(message.Value.ToString());
                }
                catch
                {
                }
            }
            else
            {
                try
                {
                    CurrentExerciseAnswerValue = JsonConvert.DeserializeObject<ExerciseAnswerValue>(message.Text);
                    CurrentTestQuestionAnswerValue = JsonConvert.DeserializeObject<TestQuestionAnswerValue>(message.Text);
                }
                catch
                {
                }
            }

            if(CurrentExerciseAnswerValue != null && CurrentExerciseAnswerValue.Excercise > 0 && CurrentExerciseAnswerValue.Answer > 0)
            {
                await context.Forward(new ExerciseDialog(this.functionDirectory, this.user, CurrentExerciseAnswerValue), ResumeAfterChildDialogAsync, message, CancellationToken.None);
                return;
            }
            else if(CurrentTestQuestionAnswerValue != null && !string.IsNullOrEmpty(CurrentTestQuestionAnswerValue.TestTitle) && CurrentTestQuestionAnswerValue.Question > 0 && CurrentTestQuestionAnswerValue.Answer > 0)
            {
                await context.Forward(new TestDialog(CurrentTestQuestionAnswerValue.TestTitle, this.functionDirectory, this.user, CurrentTestQuestionAnswerValue), ResumeAfterChildDialogAsync, message, CancellationToken.None);
                return;
            }

            string preparedCommand = Regex.Replace(message.Text.Replace("\r", string.Empty).Replace("\n", string.Empty), "<.*?>", String.Empty);
            preparedCommand = Regex.Replace(preparedCommand, "[^0-9A-zА-я- Ёё]", String.Empty).Trim();
            
            Match MatchExercise = Regex.Match(preparedCommand, this.LocalizationRootDialog.GetString("CommandExercise"), RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline);
            Match MatchProgress = Regex.Match(preparedCommand, this.LocalizationRootDialog.GetString("CommandProgress"), RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline);
            Match MatchFeedbackInline = Regex.Match(preparedCommand, this.LocalizationRootDialog.GetString("CommandFeedbackInline"), RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline);
            Match MatchFeedbackPrompt = Regex.Match(preparedCommand, this.LocalizationRootDialog.GetString("CommandFeedbackPrompt"), RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline);
            Match MatchTestInline = Regex.Match(preparedCommand, this.LocalizationRootDialog.GetString("CommandTestInline"), RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline);
            Match MatchTestPrompt = Regex.Match(preparedCommand, this.LocalizationRootDialog.GetString("CommandTestPrompt"), RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline);
            Match MatchCompanyInline = Regex.Match(preparedCommand, this.LocalizationRootDialog.GetString("CommandCompanyInline"), RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline);
            Match MatchCompanyPrompt = Regex.Match(preparedCommand, this.LocalizationRootDialog.GetString("CommandCompanyPrompt"), RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline);
            Match MatchSettingsInline = Regex.Match(preparedCommand, this.LocalizationRootDialog.GetString("CommandSettingsInline"), RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline);
            Match MatchSettingsPrompt = Regex.Match(preparedCommand, this.LocalizationRootDialog.GetString("CommandSettingsPrompt"), RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline);
            
            if(MatchExercise.Success)
            {
                await context.Forward(new ExerciseDialog(this.functionDirectory, this.user), ResumeAfterChildDialogAsync, message, CancellationToken.None);
            }
            else if(MatchProgress.Success)
            {
                await context.Forward(new ProgressDialog(this.functionDirectory, this.user), ResumeAfterChildDialogAsync, message, CancellationToken.None);
            }
            else if(MatchFeedbackInline.Success && !String.IsNullOrEmpty(MatchFeedbackInline.Value.Trim()))
            {
                await context.Forward(new FeedbackUserInDialog(MatchFeedbackInline.Value.Trim(), this.functionDirectory, this.user), ResumeAfterChildDialogAsync, message, CancellationToken.None);
            }
            else if(MatchFeedbackPrompt.Success || (MatchFeedbackInline.Success && String.IsNullOrEmpty(MatchFeedbackInline.Value.Trim())))
            {
                await context.Forward(new FeedbackUserInDialog(String.Empty, this.functionDirectory, this.user), ResumeAfterChildDialogAsync, message, CancellationToken.None);
            }
            else if(MatchTestInline.Success && !String.IsNullOrEmpty(MatchTestInline.Value.Trim()))
            {
                await context.Forward(new TestDialog(MatchTestInline.Value.Trim(), this.functionDirectory, this.user), ResumeAfterChildDialogAsync, message, CancellationToken.None);
            }
            else if(MatchTestPrompt.Success || (MatchTestInline.Success && String.IsNullOrEmpty(MatchTestInline.Value.Trim())))
            {
                await context.Forward(new TestDialog(String.Empty, this.functionDirectory, this.user), ResumeAfterChildDialogAsync, message, CancellationToken.None);
            }
            else if(MatchCompanyInline.Success && !String.IsNullOrEmpty(MatchCompanyInline.Value.Trim()))
            {
                await context.Forward(new CompanyDialog(MatchCompanyInline.Value.Trim(), this.functionDirectory, this.user), ResumeAfterChildDialogAsync, message, CancellationToken.None);
            }
            else if(MatchCompanyPrompt.Success || (MatchCompanyInline.Success && String.IsNullOrEmpty(MatchCompanyInline.Value.Trim())))
            {
                await context.Forward(new CompanyDialog(String.Empty, this.functionDirectory, this.user), ResumeAfterChildDialogAsync, message, CancellationToken.None);
            }
            else if(MatchSettingsInline.Success && !String.IsNullOrEmpty(MatchSettingsInline.Value.Trim()))
            {
                await context.Forward(new SettingsDialog(MatchSettingsInline.Value.Trim(), this.functionDirectory, this.user), ResumeAfterChildDialogAsync, message, CancellationToken.None);
            }
            else if(MatchSettingsPrompt.Success || (MatchSettingsInline.Success && String.IsNullOrEmpty(MatchSettingsInline.Value.Trim())))
            {
                await context.Forward(new SettingsDialog(String.Empty, this.functionDirectory, this.user), ResumeAfterChildDialogAsync, message, CancellationToken.None);
            }
            else
            {
                await context.Forward(new BasicQnAMakerDialog(this.functionDirectory, message.ChannelId), ResumeAfterChildDialogAsync, message, CancellationToken.None);
            }
        }
    }

    private async Task ResumeAfterChildDialogAsync(IDialogContext context, IAwaitable<object> result)
    {
        context.Done(String.Empty);
        await Task.FromResult(this);
    }
}