#load "..\Models\QnaPair.csx"

using System.Resources;
using Microsoft.Bot.Builder.Azure;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.CognitiveServices.QnAMaker;
using Microsoft.Bot.Connector;

[Serializable]
public class BasicQnAMakerDialog : QnAMakerDialog
{        
    private string functionDirectory;
    private ResourceManager LocalizationBasicQnAMakerDialog;
    private QnAMakerResults qnaMakerResults;
    private FeedbackRecord feedbackRecord;
    
    public BasicQnAMakerDialog(string functionDirectory, string ChannelId) : base(new QnAMakerService(new QnAMakerAttribute(Utils.GetAppSetting("QnAAuthKey"), Utils.GetAppSetting("QnAKnowledgebaseId"), Formatter.Format(ResourceManager.CreateFileBasedResourceManager("BasicQnAMakerDialog", Path.Combine(functionDirectory, "Resources\\Localization"), null).GetString("DefaultMessage"), ChannelId, ChannelId == "telegram" || ChannelId == "webchat" ? false : true), Double.Parse(Utils.GetAppSetting("QnAScoreMinimum")), Int32.Parse(Utils.GetAppSetting("QnAMaximumOptions")), Utils.GetAppSetting("QnAEndpointHostName"))))
    {
        this.functionDirectory = functionDirectory;
        this.LocalizationBasicQnAMakerDialog = ResourceManager.CreateFileBasedResourceManager("BasicQnAMakerDialog", Path.Combine(functionDirectory, "Resources\\Localization"), null);
    }

    protected override async Task QnAFeedbackStepAsync(IDialogContext context, QnAMakerResults qnaMakerResults)
    {
        if (qnaMakerResults.Answers.Count > 0 && qnaMakerResults.Answers.FirstOrDefault().Score > Double.Parse(Utils.GetAppSetting("QnAScoreMinToOneAnswer")))
        {
            await context.PostAsync(qnaMakerResults.Answers.FirstOrDefault().Answer);
        }
        else
        {
            this.QnAFeedbackStepTwoAsync(context, qnaMakerResults);
        }
    }

    private void QnAFeedbackStepTwoAsync(IDialogContext context, QnAMakerResults qnaMakerResults)
    {
        List<QnAMakerResult> qnaList = qnaMakerResults.Answers;
        string[] questions = qnaList.Select(x => x.Questions[0]).Concat(new[] { this.LocalizationBasicQnAMakerDialog.GetString("NoneOfTheAboveOption") }).ToArray();

        PromptOptions<string> promptOptions = new PromptOptions<string>(
            prompt: this.LocalizationBasicQnAMakerDialog.GetString("AnswerSelectionPrompt"),
            tooManyAttempts: this.LocalizationBasicQnAMakerDialog.GetString("TooManyAttempts"),
            options: questions,
            attempts: 0
        );

        this.qnaMakerResults = qnaMakerResults;
        this.feedbackRecord = new FeedbackRecord { UserId = context.Activity.AsMessageActivity().From.Id, UserQuestion = context.Activity.AsMessageActivity().Text };

        PromptDialog.Choice(context: context, resume: this.ResumeAndPostAnswer, promptOptions: promptOptions);
    }

    private async Task ResumeAndPostAnswer(IDialogContext context, IAwaitable<string> argument)
    {
        try
        {
            string selection = await argument;
            if (this.LocalizationBasicQnAMakerDialog.GetString("NoneOfTheAboveOption").Equals(selection, StringComparison.OrdinalIgnoreCase))
            {
                await context.PostAsync(this.LocalizationBasicQnAMakerDialog.GetString("NoneOfTheAboveOptionMessage"));
            }
            else if (this.qnaMakerResults != null)
            {
                foreach (QnAMakerResult qnaMakerResult in this.qnaMakerResults.Answers)
                {
                    if (qnaMakerResult.Questions[0].Equals(selection, StringComparison.OrdinalIgnoreCase))
                    {
                        await context.PostAsync(qnaMakerResult.Answer);

                        if (this.feedbackRecord != null)
                        {
                            this.feedbackRecord.KbQuestion = qnaMakerResult.Questions.First();
                            this.feedbackRecord.KbAnswer = qnaMakerResult.Answer;

                            Task[] tasks =
                                this.services.Select(
                                    s =>
                                    s.ActiveLearnAsync(
                                        this.feedbackRecord.UserId,
                                        this.feedbackRecord.UserQuestion,
                                        this.feedbackRecord.KbQuestion,
                                        this.feedbackRecord.KbAnswer,
                                        Utils.GetAppSetting("QnAKnowledgebaseId")
                                    )
                                ).ToArray();

                            await Task.WhenAll(tasks);
                            break;
                        }
                    }
                }
            }
        }
        catch (TooManyAttemptsException)
        { 
        }
        
        await this.DefaultWaitNextMessageAsync(context, context.Activity.AsMessageActivity(), qnaMakerResults);
    }
    
    protected override async Task DefaultWaitNextMessageAsync(IDialogContext context, IMessageActivity message, QnAMakerResults qnaMakerResults)
    {
        if(qnaMakerResults.Answers.Count == 0)
        {
            List<QnaPair> QnaPairList = new List<QnaPair>(){
                new QnaPair(){ 
                    Answer = this.LocalizationBasicQnAMakerDialog.GetString("DefaultAnswer"), 
                    Questions = new List<string>(new string[] { message.Text }),
                    Id = 0,
                    Source = "Custom Editorial",
                    Metadata = new List<string>()
                } 
            };

            QnaPairAdd QnaPairToAdd = new QnaPairAdd(QnaPairList);
            await QnaPairToAdd.AddQnaPairsAsync();
        }

        context.Done(true);
    }
}