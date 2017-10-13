namespace Chat
{
    public class Filter
    {
        public MessageType MessageType { get; }
        public string ChannelName { get; }

        public Filter(MessageType messageType, string channelName)
        {
            MessageType = messageType;
            ChannelName = channelName;
        }
    }
}
