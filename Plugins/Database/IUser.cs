using System.Collections.Generic;

namespace Database
{
    public interface IUser
    {
        string Username { get; }
        string Password { get; }
        List<string> Friends { get; }
        List<string> OpenFriendRequests { get; }
        List<string> UnansweredFriendRequests { get; }
    }
}