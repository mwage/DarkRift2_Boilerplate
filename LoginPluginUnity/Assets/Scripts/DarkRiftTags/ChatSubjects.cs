namespace DarkRiftTags
{
    public class ChatSubjects
    {
        public const ushort PrivateMessage = 0;
        public const ushort SuccessfulPrivateMessage = 1;
        public const ushort RoomMessage = 2;
        public const ushort GroupMessage = 3;
        public const ushort MessageFailed = 4;
        public const ushort JoinGroup = 5;
        public const ushort JoinGroupFailed = 6;
        public const ushort LeaveGroup = 7;
        public const ushort LeaveGroupFailed = 8;
        public const ushort GetActiveGroups = 9;
        public const ushort GetActiveGroupsFailed = 10;
    }
}