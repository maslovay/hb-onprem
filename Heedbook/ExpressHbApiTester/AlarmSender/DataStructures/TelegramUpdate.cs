using System.Collections.Generic;

namespace AlarmSender.DataStructures
{
    public class ChannelPost
    {
        public long message_id { get; set; }
        public long date { get; set; }
        public string text { get; set; }
    }

    public class TelegramUpdate
    {
        public long update_id { get; set; }
        public ChannelPost channel_post { get; set; }
    }

    public class TelegramUpdateResponse
    {
        public string ok { get; set; }
        public List<TelegramUpdate> result { get; set; }
    }
}