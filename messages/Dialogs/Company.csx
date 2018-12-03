#load "..\Models\Company.csx"

using System.Threading;
using System.Resources;
using System.Text.RegularExpressions;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;

// For more information about this template visit http://aka.ms/azurebots-csharp-basic
[Serializable]
public class CompanyDialog : IDialog<object>
{
    private string functionDirectory;
    private ResourceManager LocalizationCompanyDialog;
    private User user;
    private Company CurrentCompany;
    private string CompanyCode;
    private string CustomName;
    
    public CompanyDialog(string CompanyCode, string functionDirectory, User user)
    {
        this.user = user;
        this.functionDirectory = functionDirectory;
        this.CompanyCode = Regex.Replace(CompanyCode, @"\s+", "");
        this.LocalizationCompanyDialog = ResourceManager.CreateFileBasedResourceManager("CompanyDialog", Path.Combine(functionDirectory, "Resources\\Localization"), null);
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
        
        if(!String.IsNullOrEmpty(this.CompanyCode))
        {
            await ProceedCompanyAsync(context);
        }
        else
        {
            PromptDialog.Text(context, GetCompanyCodeAsync, this.LocalizationCompanyDialog.GetString("EnterCodeOrCancel"));
        }
    }

    private async Task GetCompanyCodeAsync(IDialogContext context, IAwaitable<string> prompt)
    {
        string reply  = Regex.Replace((await prompt), @"\s+", "");

        if(reply.ToLower() == this.LocalizationCompanyDialog.GetString("Cancel") || String.IsNullOrEmpty(reply))
        {
            await context.PostAsync(this.LocalizationCompanyDialog.GetString("EnteringCodeCanceled"));
            context.Done(String.Empty);
            return;
        }
        
        this.CompanyCode = reply;
        await ProceedCompanyAsync(context);
    }

    private async Task ProceedCompanyAsync(IDialogContext context)
    {
        CurrentCompany = new Company();
        await CurrentCompany.GetOrCreateCompanyByCodeAsync(this.CompanyCode);

        if(CurrentCompany.Id > 0)
        {
            await this.user.SetUserCompanyAsync(this.CurrentCompany);
            PromptDialog.Text(context, GetCustomNameAsync, this.LocalizationCompanyDialog.GetString("EnterCustomName"));
        }
        else
        {
            string message = Formatter.Format(this.LocalizationCompanyDialog.GetString("CompanyNotFound"), context.Activity.ChannelId, context.Activity.ChannelId == "telegram" || context.Activity.ChannelId == "webchat" ? false : true);

            PromptDialog.Confirm(
                context, 
                SupportRequestAsync, 
                prompt: message, 
                retry: message,
                options: new string[] { 
                    this.LocalizationCompanyDialog.GetString("Yes"), 
                    this.LocalizationCompanyDialog.GetString("No") 
                }, 
                patterns: new string[][] { 
                    new string[] { 
                        this.LocalizationCompanyDialog.GetString("Yes") 
                    }, 
                    new string[] { 
                        this.LocalizationCompanyDialog.GetString("No") 
                    } 
                }
            );
        }
    }

    private async Task GetCustomNameAsync(IDialogContext context, IAwaitable<string> prompt)
    {
        string reply  = await prompt;

        this.CustomName = reply;
        await this.user.SetUserCustomNameAsync(this.CustomName);
        //await context.PostAsync(string.Format(this.LocalizationCompanyDialog.GetString("CompanyApplied"), this.CurrentCompany.Title));
        await context.PostAsync(this.LocalizationCompanyDialog.GetString("ProfileSaved"));
        context.Done(String.Empty);
        return;
    }

    private async Task SupportRequestAsync(IDialogContext context, IAwaitable<bool> result)
    {
        if (await result == true)
        {
            await context.Forward(new FeedbackUserInDialog(this.LocalizationCompanyDialog.GetString("UserRequestedHelp"), this.functionDirectory, this.user), ResumeAfterFeedbackDialogAsync, context.Activity, CancellationToken.None);
        }
        else
        {
            await context.PostAsync(this.LocalizationCompanyDialog.GetString("UserRequestedHelpCanceled"));
        }
        context.Done(String.Empty);
        return;
    }

    private async Task ResumeAfterFeedbackDialogAsync(IDialogContext context, IAwaitable<object> result)
    {
        await result;
        context.Done(String.Empty);
        return;
    }
}