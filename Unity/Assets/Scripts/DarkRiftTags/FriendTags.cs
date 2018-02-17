namespace DarkRiftTags
{
    public class FriendTags
    {
        private const ushort Shift = Tags.Friends * Tags.TagsPerPlugin;

        public const ushort FriendRequest = 0 + Shift;
        public const ushort RequestFailed = 1 + Shift;
        public const ushort RequestSuccess = 2 + Shift;
        public const ushort AcceptRequest = 3 + Shift;
        public const ushort AcceptRequestSuccess = 4 + Shift;
        public const ushort AcceptRequestFailed = 5 + Shift;
        public const ushort DeclineRequest = 6 + Shift;
        public const ushort DeclineRequestSuccess = 7 + Shift;
        public const ushort DeclineRequestFailed = 8 + Shift;
        public const ushort RemoveFriend = 9 + Shift;
        public const ushort RemoveFriendSuccess = 10 + Shift;
        public const ushort RemoveFriendFailed = 11 + Shift;
        public const ushort GetAllFriends = 12 + Shift;
        public const ushort GetAllFriendsFailed = 13 + Shift;
        public const ushort FriendLoggedIn = 14 + Shift;
        public const ushort FriendLoggedOut = 15 + Shift;
    }
}