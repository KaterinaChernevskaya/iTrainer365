using System.Text.RegularExpressions;

public class Formatter
{
    public static string Format(string Message, string ChannelId, bool ForceHTML = false)
    {
        ReverseMarkdown.Converter converter2Markdown = new ReverseMarkdown.Converter();
        
        if(ForceHTML == true)
        {
            Message = Regex.Replace(CommonMark.CommonMarkConverter.Convert(Message), @"<\/?br>", "\n\n");
            if(ChannelId == "skype")
            {
                Message = Regex.Replace(Message, @"<\/?p>", String.Empty);
            }
        }
        else
        {
            switch (ChannelId)
            {
                case "skype":
                    Message = Regex.Replace(converter2Markdown.Convert(Message), "<.*?>", String.Empty);
                    break;
                case "msteams":
                    Message = Regex.Replace(CommonMark.CommonMarkConverter.Convert(Message), @"<\/?br>", "\n\n");
                    break;
                case "telegram":
                    Message = Regex.Replace(converter2Markdown.Convert(Message), "<.*?>", String.Empty);
                    Message = Regex.Replace(Message, @"((http|ftp)(s?))(\:\/\/|\&#58;\/\/)([-.\w]*[0-9a-zA-Z])*(:(0-9)*)*(\/?)([a-zA-Z0-9\-\?\=\'\/\\\+&amp;%\$#_]*)?", "$1://$5$8$9");
                    break;
                case "webchat":
                    Message = Regex.Replace(converter2Markdown.Convert(Message), "<.*?>", String.Empty);
                    break;
                default:
                    Message = Regex.Replace(converter2Markdown.Convert(Message), "<.*?>", String.Empty);
                    Message = Regex.Replace(Message, @"((http|ftp)(s?))(\:\/\/|\&#58;\/\/)([-.\w]*[0-9a-zA-Z])*(:(0-9)*)*(\/?)([a-zA-Z0-9\-\?\=\'\/\\\+&amp;%\$#_]*)?", "$1://$5$8$9");
                    break;
            }
        }

        return Message;
    }
}