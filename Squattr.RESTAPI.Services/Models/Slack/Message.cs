using System.Collections.Generic;

namespace Squattr.RESTAPI.Services.Models.Slack
{
    public class Message
    {
        public string text { get; set; }
        public List<Attachment> attachments;
        public bool mrkdwn { get; set; }
    }
}
