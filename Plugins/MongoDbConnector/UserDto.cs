using System.Collections.Generic;
using Database;

namespace MongoDbConnector
{
    //DTO for returning an IUser object
    public class UserDto : IUser
    {
        public UserDto(User user)
        {
            Username = user.Username;
            Password = user.Password;
            Friends = user.Friends;
            OpenFriendRequests = user.OpenFriendRequests;
            UnansweredFriendRequests = user.UnansweredFriendRequests;
        }

        public string Username { get; }
        public string Password { get; }
        public List<string> Friends { get; }
        public List<string> OpenFriendRequests { get; }
        public List<string> UnansweredFriendRequests { get; }

    }
}