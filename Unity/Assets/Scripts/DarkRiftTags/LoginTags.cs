namespace DarkRiftTags
{
    public class LoginTags
    {
        private const ushort Shift = Tags.Login * Tags.TagsPerPlugin;

        public const ushort LoginUser = 0 + Shift;
        public const ushort LogoutUser = 1 + Shift;
        public const ushort AddUser = 2 + Shift;
        public const ushort LoginSuccess = 3 + Shift;
        public const ushort LoginFailed = 4 + Shift;
        public const ushort LogoutSuccess = 5 + Shift;
        public const ushort AddUserSuccess = 6 + Shift;
        public const ushort AddUserFailed = 7 + Shift;
    }
}