using System.Collections.Generic;
using MongoDB.Bson.Serialization.Attributes;

namespace MongoDbConnector
{
    public class User
    {
        // MongoDb has problems with Lists as properties, so you can't use the DTO as schema.
        // In most other cases you can just use the Schema as the DTO.

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