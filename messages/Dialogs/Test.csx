#load "..\Models\Test.csx"
#load "..\Helpers\Formatter.csx"

using System.Resources;
using System.Text.RegularExpressions;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;

// For more information about this template visit http://aka.ms/azurebots-csharp-basic
[Serializable]
public class TestDialog : IDialog<object>
{
    private string functionDirectory;
    private ResourceManager LocalizationTestDialog;
    private Test CurrentTest;
    private int QuestionNumber;
    private int QuestionCorrectCounter;
    private string TestTitle;
    private User user;
    TestQuestionAnswerValue CurrentTestQuestionAnswerValue;
    
    public TestDialog(string TestTitle, string functionDirectory, User user, TestQuestionAnswerValue CurrentTestQuestionAnswerValue = null)
    {
        this.functionDirectory = functionDirectory;
        this.user = user;
        if(CurrentTestQuestionAnswerValue != null)
        {
            this.CurrentTestQuestionAnswerValue = CurrentTestQuestionAnswerValue;
            this.TestTitle = this.CurrentTestQuestionAnswerValue.TestTitle;
            this.QuestionNumber = this.CurrentTestQuestionAnswerValue.QuestionNumber;
            this.QuestionCorrectCounter = this.CurrentTestQuestionAnswerValue.QuestionCorrectCounter;
        }
        else
        {
            this.TestTitle = TestTitle.Trim();
            this.QuestionNumber = 0;
            this.QuestionCorrectCounter = 0;
        }
        this.LocalizationTestDialog = ResourceManager.CreateFileBasedResourceManager("TestDialog", Path.Combine(functionDirectory, "Resources\\Localization"), null);
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

        if(!String.IsNullOrEmpty(this.TestTitle))
        {
            await this.ProceedTestAsync(context);
        }
        else
        {
            PromptDialog.Text(context, GetTestTitleAsync, this.LocalizationTestDialog.GetString("EnterTestTitleOrCancel"));
        }
    }

    private async Task GetTestTitleAsync(IDialogContext context, IAwaitable<string> prompt)
    {
        string reply  = (await prompt).Trim();

        if(reply.ToLower() == this.LocalizationTestDialog.GetString("Cancel") || String.IsNullOrEmpty(reply))
        {
            await context.PostAsync(this.LocalizationTestDialog.GetString("RunningTestCanceled"));
            context.Done(String.Empty);
            return;
        }
        
        this.TestTitle = reply;
        await this.ProceedTestAsync(context);
    }

    private async Task ProceedTestAsync(IDialogContext context)
    {
        if(this.CurrentTestQuestionAnswerValue != null && this.CurrentTestQuestionAnswerValue.Question > 0 && this.CurrentTestQuestionAnswerValue.Answer > 0)
        {
            this.QuestionNumber++;
            await this.PostTestQuestionResultAsync(context);
        }
        
        this.CurrentTest = new Test(this.functionDirectory);
        await this.CurrentTest.GetTestByTitleAsync(this.TestTitle, this.QuestionNumber, this.QuestionCorrectCounter);

        if(this.CurrentTest.Questions.Count > 0)
        {
            this.TestTitle = this.CurrentTest.Title;

            if(this.CurrentTestQuestionAnswerValue == null)
            {
                await context.PostAsync(string.Format(this.LocalizationTestDialog.GetString("RunningTest"), this.TestTitle));
            }

            if(this.CurrentTest.Questions.Count > this.QuestionNumber)
            {
                await this.PostTestQuestionAsync(context, this.CurrentTest.Questions.Values[this.QuestionNumber]);
            }
            else
            {
                await this.PostTestResultAsync(context);
            } 
        }
        else
        {
            await context.PostAsync(this.LocalizationTestDialog.GetString("TestNotFound"));
        }
        context.Done(String.Empty);
    }

    private async Task PostTestQuestionAsync(IDialogContext context, TestQuestion CurrentTestQuestion)
    {
        IMessageActivity TestQuestionMessage = context.MakeMessage();

        List<CardAction> Buttons = new List<CardAction>();

        foreach (TestQuestionAnswer CurrentTestQuestionAnswer in CurrentTestQuestion.Answers)
        {
            Buttons.Add(CurrentTestQuestionAnswer.Button);
        }

        Attachment CardAttachment = new HeroCard
        {
            Title = CurrentTestQuestion.Title,
            Subtitle = CurrentTestQuestion.Subtitle,
            Text = Formatter.Format(CurrentTestQuestion.TaskText, this.user.ChannelId, this.user.ChannelId == "telegram" || this.user.ChannelId == "webchat" ? false : true),
            Buttons = Buttons
        }.ToAttachment();

        TestQuestionMessage.Attachments.Add(CardAttachment);
        await context.PostAsync(TestQuestionMessage);
    }

    private async Task PostTestQuestionResultAsync(IDialogContext context)
    {
        this.CurrentTest = new Test(this.functionDirectory);
        await this.CurrentTest.GetTestByTitleAsync(this.TestTitle);

        if(this.CurrentTestQuestionAnswerValue != null && this.CurrentTestQuestionAnswerValue.Question > 0 && this.CurrentTestQuestionAnswerValue.Answer > 0 && this.CurrentTestQuestionAnswerValue.Answer <= this.CurrentTest.Questions[this.CurrentTestQuestionAnswerValue.Question].Answers.Count)
        {
            string message = Formatter.Format(this.CurrentTest.Questions[this.CurrentTestQuestionAnswerValue.Question].Answers[this.CurrentTestQuestionAnswerValue.Answer-1].AnswerText, this.user.ChannelId);

            if(this.CurrentTest.Questions[this.CurrentTestQuestionAnswerValue.Question].Answers[this.CurrentTestQuestionAnswerValue.Answer-1].Correct)
            {
                this.QuestionCorrectCounter++;
            }

            await this.CurrentTest.AddTestAnswerAsync(this.CurrentTestQuestionAnswerValue.Question, this.CurrentTestQuestionAnswerValue.Answer, this.user);
            await context.PostAsync(message);
        }
        else
        {
            await context.PostAsync(this.LocalizationTestDialog.GetString("AnswerNotFound"));
        }
    }

    private async Task PostTestResultAsync(IDialogContext context)
    {
        this.CurrentTest = new Test(this.functionDirectory);
        await this.CurrentTest.GetTestByTitleAsync(this.TestTitle);

        decimal SuccessRateAnswers = (decimal)this.QuestionCorrectCounter / this.CurrentTest.Questions.Count;
        decimal BestTestResult = await this.CurrentTest.GetBestTestResultAsync(this.user);
        
        await this.CurrentTest.AddTestResultAsync(SuccessRateAnswers, this.user);
        
        string message = string.Format(this.LocalizationTestDialog.GetString("TestFinished"), this.TestTitle, SuccessRateAnswers);

        if(BestTestResult > SuccessRateAnswers)
        {
            message += " " + string.Format(this.LocalizationTestDialog.GetString("BestResultPercent"), BestTestResult);
        }
        else if(SuccessRateAnswers > BestTestResult)
        {
            message += " " + this.LocalizationTestDialog.GetString("BestResult");
        }

        await context.PostAsync(message);
    }
}