using System.Resources;
using System.Text.RegularExpressions;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;

// For more information about this template visit http://aka.ms/azurebots-csharp-basic
[Serializable]
public class SettingsDialog : IDialog<object>
{
    private ResourceManager LocalizationSettingsDialog;
    private User user;
    Settings settings;
    private bool TimezoneSelected;
    private bool SubscriptionSelected;
    private bool SubscriptionTimeSelected;

    public SettingsDialog(string SettingCode, string functionDirectory, User user)
    {
        this.user = user;
        this.settings = new Settings();
        this.LocalizationSettingsDialog = ResourceManager.CreateFileBasedResourceManager("SettingsDialog", Path.Combine(functionDirectory, "Resources\\Localization"), null);
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
        await PostSettingsDialogAsync(context);
    }

    private async Task PostSettingsDialogAsync(IDialogContext context)
    {
        if(this.user.Company == null || this.user.Company.Id == 0)
        {
            await context.PostAsync(Formatter.Format(this.LocalizationSettingsDialog.GetString("NoPremiumAccess"), context.Activity.ChannelId, context.Activity.ChannelId == "telegram" || context.Activity.ChannelId == "webchat" ? false : true));
            context.Done(String.Empty);
            return;
        }
        
        if(this.SubscriptionSelected != true)
        {
            PromptDialog.Confirm(
                context, 
                ProceedSubscriptionSettingsDialogAsync, 
                this.LocalizationSettingsDialog.GetString("Subscribe"), 
                options: new string[] { 
                    this.LocalizationSettingsDialog.GetString("Yes"), 
                    this.LocalizationSettingsDialog.GetString("No") 
                }, 
                patterns: new string[][] { 
                    new string[] { 
                        this.LocalizationSettingsDialog.GetString("Yes") 
                    }, 
                    new string[] { 
                        this.LocalizationSettingsDialog.GetString("No") 
                    } 
                }
            );
        }
        else if(this.TimezoneSelected != true)
        {
            PromptDialog.Text(
                context,
                ProceedTimezoneSettingsDialogAsync,
                this.LocalizationSettingsDialog.GetString("ChoseTimezone")
            );
        }
        else if(this.SubscriptionTimeSelected != true)
        {
            PromptDialog.Text(
                context,
                ProceedSubscriptionTimeSettingsDialogAsync,
                this.LocalizationSettingsDialog.GetString("SubscribeTime")
            );
        }
        else
        {
            await context.PostAsync(this.LocalizationSettingsDialog.GetString("SettingsSaved"));
            context.Done(String.Empty);
        }
    }

    private async Task ProceedSubscriptionSettingsDialogAsync(IDialogContext context, IAwaitable<bool> result)
    {
        if (await result == true)
        {
            await this.user.SubscribeAsync(context.Activity.Conversation.Id, context.Activity.ServiceUrl);
        }
        else
        {
            await this.user.UnsubscribeAsync(context.Activity.Conversation.Id, context.Activity.ServiceUrl);
            this.TimezoneSelected = true;
            this.SubscriptionTimeSelected = true;
        }

        this.SubscriptionSelected = true;
        await PostSettingsDialogAsync(context);
    }

    private async Task ProceedTimezoneSettingsDialogAsync(IDialogContext context, IAwaitable<string> prompt)
    {
        string result = (await prompt).Trim().ToLower();
        Match match = Regex.Match(result, @"(^([+-])([1-9][0-2]?$)|^([0]$))");

        if(match.Success)
        {
            this.settings.Timezone = match.Value;
            await this.user.SetUserSettingsAsync(settings);
        }

        this.TimezoneSelected = true;
        await PostSettingsDialogAsync(context);
    }

    private async Task ProceedSubscriptionTimeSettingsDialogAsync(IDialogContext context, IAwaitable<string> prompt)
    {
        string result = (await prompt).Trim().ToLower();
        Match match = Regex.Match(result, @"^[1-9]?$|^[1][0-9]?$|^[2][0-4]?$");

        if(match.Success)
        {
            await this.user.SetSubscriptionTimeAsync(context.Activity.Conversation.Id, context.Activity.ServiceUrl, match.Value);
            this.SubscriptionTimeSelected = true;
        }

        await PostSettingsDialogAsync(context);
    }
    
}