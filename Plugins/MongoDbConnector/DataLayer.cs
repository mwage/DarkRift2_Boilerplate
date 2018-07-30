using System.Linq;
using System.Threading.Tasks;
using Database;
using MongoDB.Driver;

namespace MongoDbConnector
{
    internal class DataLayer : IDataLayer
    {
        //TODO: Find out how asynchronous calls work in MongoDB driver

        private readonly MongoDbConnector _database;

        public DataLayer(string name, MongoDbConnector database)
        {
            Name = name;
            _database = database;
        }

        public string Name { get; }

        #region Login

        //Gets user data from the database and builds a User object to send back
        public async Task<IUser> GetUser(string username)
        {
            User user = null;
            await Task.Run(() => { user = _database.Users.AsQueryable().FirstOrDefault(u => u.Username == username); });
            return user != null ? new UserDto(user) : null;
        }

        //Checks if a username is already taken
        public async Task<bool> UsernameAvailable(string username)
        {
            var isAvailable = false;
            await Task.Run(() => { isAvailable = _database.Users.AsQueryable().Any(u => u.Username == username); });
            return isAvailable;
        }

        //Add a new user to the database
        public async Task AddNewUser(string username, string password)
        {
            await Task.Run(() => { _database.Users.InsertOne(new User(username, password)); });
        }

        //Remove user completely from the database
        public async Task DeleteUser(string username)
        {
            _database.Users.DeleteOne(u => u.Username == username);
            await Task.Run(() => { _database.Users.DeleteOne(u => u.Username == username); });
        }

        #endregion

        #region Friends

        public async Task AddRequest(string sender, string receiver)
        {
            await Task.Run(() =>
            {
                //Add OpenFriendRequest of sender to receiver
                var updateReceiving = Builders<User>.Update.AddToSet(u => u.OpenFriendRequests, sender);
                _database.Users.UpdateOne(u => u.Username == receiver, updateReceiving);

                //Add OpenFriendRequest of receiver to sender
                var updateSender = Builders<User>.Update.AddToSet(u => u.UnansweredFriendRequests, receiver);
                _database.Users.UpdateOne(u => u.Username == sender, updateSender);
            });
        }

        public async Task RemoveRequest(string sender, string receiver)
        {
            await Task.Run(() =>
            {
                //Remove OpenFriendRequest of receiver from sender
                var updateSender = Builders<User>.Update.Pull(u => u.OpenFriendRequests, receiver);
                _database.Users.UpdateOne(u => u.Username == sender, updateSender);

                //Remove OpenFriendRequest of sender from receiver
                var updateReceiving = Builders<User>.Update.Pull(u => u.UnansweredFriendRequests, sender);
                _database.Users.UpdateOne(u => u.Username == receiver, updateReceiving);
            });
        }

        public async Task AddFriend(string sender, string receiver)
        {
            await Task.Run(() =>
            {
                //Add sender to receivers friend list
                var updateReceiving = Builders<User>.Update.AddToSet(u => u.Friends, sender);
                _database.Users.UpdateOne(u => u.Username == receiver, updateReceiving);

                //Add receiver to senders friend list
                var updateSending = Builders<User>.Update.AddToSet(u => u.Friends, receiver);
                _database.Users.UpdateOne(u => u.Username == sender, updateSending);
            });
        }

        public async Task RemoveFriend(string sender, string receiver)
        {
            var senderUser = GetUser(sender).Result;
            var receiverUser = GetUser(receiver).Result;

            await Task.Run(() =>
            {
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
            });
        }

        #endregion
    }
}