using System.Resources;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using PawnHunter.Numerals;

// For more information about this template visit http://aka.ms/azurebots-csharp-basic
[Serializable]
public class ProgressDialog : IDialog<object>
{
    private User user;
    private ResourceManager LocalizationProgressDialog;

    public ProgressDialog(string functionDirectory, User user)
    {
        this.user = user;
        this.LocalizationProgressDialog = ResourceManager.CreateFileBasedResourceManager("ProgressDialog", Path.Combine(functionDirectory, "Resources\\Localization"), null);
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
        
        Tuple<bool, int, int, int, int> CurrentProgress = await this.user.GetProgressAsync();

        if(CurrentProgress.Item1)
        {
            NumeralsFormatter formatter = new NumeralsFormatter();

            string DaysInEducation = String.Format(formatter, this.LocalizationProgressDialog.GetString("ProgressDays"), CurrentProgress.Item2);
            string AllAnswers = String.Format(formatter, this.LocalizationProgressDialog.GetString("ProgressAnswers"), CurrentProgress.Item3);
            string SuccessRateAnswers = String.Format(formatter, this.LocalizationProgressDialog.GetString("ProgressCorrectAnswersPercent"), CurrentProgress.Item5);
                    
            await context.PostAsync($"{DaysInEducation}\n\n{AllAnswers}\n\n{SuccessRateAnswers}");
        }

        context.Done(String.Empty);
    }
}