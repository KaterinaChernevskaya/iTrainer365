#load "Dialogs\Root.csx"
#load "Models\User.csx"
#load "Helpers\Proactive.csx"
#load "Helpers\SpecialActivities.csx"
//#load "Tests\RootDialogTests.csx"
//#load "Tests\IChatHelper.csx"

using System.Globalization;
using System.Net;
using System.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Microsoft.Bot.Builder.Azure;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Internals;
using Microsoft.Bot.Connector;
using Autofac;

public static async Task<object> Run(HttpRequestMessage req, TraceWriter log, Microsoft.Azure.WebJobs.ExecutionContext context)
{
    log.Info("Webhook was triggered!");
    CultureInfo.DefaultThreadCurrentCulture = new CultureInfo(Utils.GetAppSetting("Locale"));
    CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo(Utils.GetAppSetting("Locale"));

    // Initialize the azure bot
    using (BotService.Initialize())
    {
        string functionDirectory = context.FunctionDirectory;
        User user;

        // Deserialize the incoming activity
        string jsonContent = await req.Content.ReadAsStringAsync();
        Activity activity = JsonConvert.DeserializeObject<Activity>(jsonContent);
        activity.Locale = Utils.GetAppSetting("Locale");
        
        // authenticate incoming request and add activity.ServiceUrl to MicrosoftAppCredentials.TrustedHostNames
        // if request is authenticated
        if (!await BotService.Authenticator.TryAuthenticateAsync(req, new [] {activity}, CancellationToken.None))
        {
            return BotAuthenticator.GenerateUnauthorizedResponse(req);
        }
        
        if (activity != null)
        {
            TableBotDataStore store = new TableBotDataStore(Utils.GetAppSetting("AzureWebJobsStorage"));
            Conversation.UpdateContainer(
                builder =>
                {
                    builder.Register(c => store)
                        .Keyed<IBotDataStore<BotData>>(AzureModule.Key_DataStore)
                        .AsSelf()
                        .SingleInstance();

                    builder.Register(c => new CachingBotDataStore(store,
                        CachingBotDataStoreConsistencyPolicy
                        //.ETagBasedConsistency))
                        .LastWriteWins))
                        .As<IBotDataStore<BotData>>()
                        .AsSelf()
                        .InstancePerLifetimeScope();
                }
            );

            user = new User();
            await user.GetOrCreateUserByActivityAsync(activity);
              
            // one of these will have an interface and process it
            switch (activity.GetActivityType())
            {
                case ActivityTypes.Message:
                    await SpecialActivities.SendTypingActivityAsync(activity);
                    log.Info($"Bot called by: {activity.From.Name} <{activity.From.Id}@{activity.ChannelId}>");
                    await Conversation.SendAsync(activity, () => new RootDialog(functionDirectory, user).DefaultIfException());
                    break;
                case ActivityTypes.Event:
                    log.Info("Trigger start");
                    IEventActivity triggerEvent = activity;
                    Message Message = JsonConvert.DeserializeObject<Message>(((JObject) triggerEvent.Value).GetValue("Message").ToString());
                    
                    if(Message.Type == MessageType.Subscription)
                    {
                        log.Info("Trigger Subscription");
                        await Proactive.SendSubscriptionMessageAsync(Message.To, functionDirectory);
                    }
                    else if(Message.Type == MessageType.Feedback)
                    {
                        if(Message.Direction == Direction.ToAdmin)
                        {
                            log.Info("Trigger FeedbackAdminOut");
                            await Proactive.SendFeedbackAdminOutMessageAsync(Message, functionDirectory);
                        }
                        else if(Message.Direction == Direction.ToUser)
                        {
                            log.Info("Trigger FeedbackUserOut");
                            await Proactive.SendFeedbackUserOutMessageAsync(Message, functionDirectory);
                        }
                    }

                    log.Info("Trigger end");
                    break;
                case ActivityTypes.ConversationUpdate:
                    await SpecialActivities.SendConversationUpdateAsync(activity, functionDirectory);
                    break;
                case ActivityTypes.ContactRelationUpdate:
                case ActivityTypes.Typing:
                case ActivityTypes.DeleteUserData:
                case ActivityTypes.Ping:
                default:
                    log.Error($"Unknown activity type ignored: {activity.GetActivityType()}");
                    break;
            }
        }
        return req.CreateResponse(HttpStatusCode.Accepted);
    }    
}
