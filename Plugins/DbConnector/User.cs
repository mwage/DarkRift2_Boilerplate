using System.Collections.Generic;
using MongoDB.Bson.Serialization.Attributes;

namespace DbConnectorPlugin
{
    public class User
    {
        [BsonId]
        public string Username { get; }
        public string Password { get; }
        public List<string> Friends;
        public List<string> OpenFriendRequests;
        public List<string> UnansweredFriendRequests;

        public User(string username, string password)
        {
            Username = username;
            Password = password;
            Friends = new List<string>();
            OpenFriendRequests = new List<string>();
            UnansweredFriendRequests = new List<string>();
        }
    }
}
