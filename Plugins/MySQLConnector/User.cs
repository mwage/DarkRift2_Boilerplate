using System.Collections.Generic;
using Database;

namespace MySQLConnector
{
    internal class User : IUser
    {
        public User(string username, string password, List<string> friends, List<string> openFriendRequests, List<string> unansweredFriendRequests)
        {
            Username = username;
            Password = password;
            Friends = friends;
            OpenFriendRequests = openFriendRequests;
            UnansweredFriendRequests = unansweredFriendRequests;
        }

        public string Username { get; }
        public string Password { get; }
        public List<string> Friends { get; }
        public List<string> OpenFriendRequests { get; }
        public List<string> UnansweredFriendRequests { get; }
    }
}