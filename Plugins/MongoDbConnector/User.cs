using Database;
using MongoDB.Bson.Serialization.Attributes;

namespace MongoDbConnector
{
    public class User : IUser
    {
        [BsonId]
        public string Username { get; }
        public string Password { get; }

        public User(string username, string password)
        {
            Username = username;
            Password = password;
        }
    }
}