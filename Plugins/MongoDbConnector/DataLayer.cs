using System.Linq;
using Database;
using MongoDB.Driver;

namespace MongoDbConnector
{
    internal class DataLayer : IDataLayer
    {
        private readonly MongoDbConnector _database;

        public DataLayer(string name, MongoDbConnector database)
        {
            Name = name;
            _database = database;
        }

        public string Name { get; }

        #region Login

        public IUser GetUser(string username)
        {
            //Gets user data from the database and builds a User object to send back
            var user = _database.Users.AsQueryable().FirstOrDefault(u => u.Username == username);
            return user != null ? new UserDto(user) : null;
        }

        public bool UsernameAvailable(string username)
        {
            //Checks if a username is already taken
            return !_database.Users.AsQueryable().Any(u => u.Username == username);
        }

        public void AddNewUser(string username, string password)
        {
            //Add a new user to the database
            _database.Users.InsertOne(new User(username, password));
        }

        public void DeleteUser(string username)
        {
            //Remove user completely from the database
            _database.Users.DeleteOne(u => u.Username == username);
        }

        #endregion

        #region Friends

        public void AddRequest(string sender, string receiver)
        {
            //Add OpenFriendRequest of sender to receiver
            var updateReceiving = Builders<User>.Update.AddToSet(u => u.OpenFriendRequests, sender);
            _database.Users.UpdateOne(u => u.Username == receiver, updateReceiving);

            //Add OpenFriendRequest of receiver to sender
            var updateSender = Builders<User>.Update.AddToSet(u => u.UnansweredFriendRequests, receiver);
            _database.Users.UpdateOne(u => u.Username == sender, updateSender);
        }

        public void RemoveRequest(string sender, string receiver)
        {
            //Remove OpenFriendRequest of receiver from sender
            var updateSender = Builders<User>.Update.Pull(u => u.OpenFriendRequests, receiver);
            _database.Users.UpdateOne(u => u.Username == sender, updateSender);

            //Remove OpenFriendRequest of sender from receiver
            var updateReceiving = Builders<User>.Update.Pull(u => u.UnansweredFriendRequests, sender);
            _database.Users.UpdateOne(u => u.Username == receiver, updateReceiving);
        }

        public void AddFriend(string sender, string receiver)
        {
            //Add sender to receivers friend list
            var updateReceiving = Builders<User>.Update.AddToSet(u => u.Friends, sender);
            _database.Users.UpdateOne(u => u.Username == receiver, updateReceiving);

            //Add receiver to senders friend list
            var updateSending = Builders<User>.Update.AddToSet(u => u.Friends, receiver);
            _database.Users.UpdateOne(u => u.Username == sender, updateSending);
        }

        public void RemoveFriend(string sender, string receiver)
        {
            var senderUser = GetUser(sender);
            var receiverUser = GetUser(receiver);

            //Update sender
            if (senderUser.Friends.Contains(receiver))
            {
                //remove receiver from senders friend list
                var updateSender = Builders<User>.Update.Pull(u => u.Friends, receiver);
                _database.Users.UpdateOne(u => u.Username == sender, updateSender);
            }
            if (senderUser.OpenFriendRequests.Contains(receiver))
            {
                //remove receiver from senders open friend requests
                var updateSender = Builders<User>.Update.Pull(u => u.OpenFriendRequests, receiver);
                _database.Users.UpdateOne(u => u.Username == sender, updateSender);
            }
            if (senderUser.UnansweredFriendRequests.Contains(receiver))
            {
                // remove receiver from senders unanswered friend requests
                var updateSender = Builders<User>.Update.Pull(u => u.UnansweredFriendRequests, receiver);
                _database.Users.UpdateOne(u => u.Username == sender, updateSender);
            }

            //Update receiver
            if (receiverUser.Friends.Contains(sender))
            {
                //remove sender from receivers friend list
                var updateReceiver = Builders<User>.Update.Pull(u => u.Friends, sender);
                _database.Users.UpdateOne(u => u.Username == receiver, updateReceiver);
            }
            if (receiverUser.OpenFriendRequests.Contains(sender))
            {
                //remove sender from receivers open friend requests
                var updateReceiver = Builders<User>.Update.Pull(u => u.OpenFriendRequests, sender);
                _database.Users.UpdateOne(u => u.Username == receiver, updateReceiver);
            }
            if (receiverUser.UnansweredFriendRequests.Contains(sender))
            {
                //remove sender from receivers unanswered friend requests
                var updateReceiver = Builders<User>.Update.Pull(u => u.UnansweredFriendRequests, sender);
                _database.Users.UpdateOne(u => u.Username == receiver, updateReceiver);
            }

            #endregion
        }
    }
}