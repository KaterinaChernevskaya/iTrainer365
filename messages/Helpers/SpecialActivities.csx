using Microsoft.Bot.Connector;
using System.Resources;

public class SpecialActivities
{
    public static async Task SendTypingActivityAsync(Activity activity)
    {
        ConnectorClient connector = new ConnectorClient(new Uri(activity.ServiceUrl));
        Activity isTypingReply = activity.CreateReply();
        isTypingReply.Type = ActivityTypes.Typing;
        await connector.Conversations.ReplyToActivityAsync(isTypingReply);
    }

    public static async Task SendConversationUpdateAsync(Activity activity, string functionDirectory)
    {
        ResourceManager LocalizationRun;
        ConnectorClient client = new ConnectorClient(new Uri(activity.ServiceUrl));
        IConversationUpdateActivity update = activity;
        if (update.MembersAdded.Any())
        {
            LocalizationRun = ResourceManager.CreateFileBasedResourceManager("Run", Path.Combine(functionDirectory, "Resources\\Localization"), null);
            Activity reply = activity.CreateReply();
            IEnumerable<ChannelAccount> newMembers = update.MembersAdded?.Where(t => t.Id != activity.Recipient.Id);
            foreach (ChannelAccount newMember in newMembers)
            {
                reply.Text = Formatter.Format(LocalizationRun.GetString("FirstWelcome"), activity.ChannelId);
                await client.Conversations.ReplyToActivityAsync(reply);
            }
        }
    }
}