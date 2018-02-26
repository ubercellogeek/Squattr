using System.Collections.Generic;

namespace Squattr.RESTAPI.Services.Models.Slack
{
    public class Attachment
    {
        public string fallback;
        public string color;
        public string pretext;
        public string author_name;
        public string author_link;
        public string author_icon;
        public string title;
        public string title_link;
        public string text;
        public List<Field> fields;

        public string image_url;
        public string thumb_url;
        public string[] mrkdwn_in;
    }
}
