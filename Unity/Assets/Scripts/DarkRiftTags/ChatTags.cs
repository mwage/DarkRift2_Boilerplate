namespace DarkRiftTags
{
    public class ChatTags
    {
        private const ushort Shift = Tags.Chat * Tags.TagsPerPlugin;

        public const ushort PrivateMessage = 0 + Shift;
        public const ushort SuccessfulPrivateMessage = 1 + Shift;
        public const ushort RoomMessage = 2 + Shift;
        public const ushort GroupMessage = 3 + Shift;
        public const ushort MessageFailed = 4 + Shift;
        public const ushort JoinGroup = 5 + Shift;
        public const ushort JoinGroupFailed = 6 + Shift;
        public const ushort LeaveGroup = 7 + Shift;
        public const ushort LeaveGroupFailed = 8 + Shift;
        public const ushort GetActiveGroups = 9 + Shift;
        public const ushort GetActiveGroupsFailed = 10 + Shift;
    }
}