using Database;
using System.Collections.Generic;

namespace MongoDbConnector
{
    public class UserDto : IUser
    {
        //DTO for returning an IUser object

        public string Username { get; }
        public string Password { get; }
        public List<string> Friends { get; }
        public List<string> OpenFriendRequests { get; }
        public List<string> UnansweredFriendRequests { get; }
        
        public UserDto(User user)
        {
            Username = user.Username;
            Password = user.Password;
            Friends = user.Friends;
            OpenFriendRequests = user.OpenFriendRequests;
            UnansweredFriendRequests = user.UnansweredFriendRequests;
        }
    }
}
