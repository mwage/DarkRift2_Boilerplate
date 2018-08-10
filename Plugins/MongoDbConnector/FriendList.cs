using System.Collections.Generic;
using MongoDB.Bson.Serialization.Attributes;

namespace MongoDbConnector
{
    public class FriendList
    {
        // MongoDb has problems with Lists as properties, so you can't use the DTO as schema.
        // In most other cases you can just use the Schema as the DTO.

        [BsonId] public string Username { get; }
        public List<string> Friends;
        public List<string> OpenFriendRequests;
        public List<string> UnansweredFriendRequests;

        public FriendList(string username)
        {
            Username = username;
            Friends = new List<string>();
            OpenFriendRequests = new List<string>();
            UnansweredFriendRequests = new List<string>();
        }
    }
}