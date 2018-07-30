using System.Linq;
using System.Threading.Tasks;
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

        //Gets user data from the database and builds a User object to send back
        public async Task<IUser> GetUser(string username)
        {
            var user = await _database.Users.Find(u => u.Username == username).FirstOrDefaultAsync();
            return user != null ? new UserDto(user) : null;
        }

        //Checks if a username is already taken
        public async Task<bool> UsernameAvailable(string username)
        {
            return await _database.Users.Find(u => u.Username == username).FirstOrDefaultAsync() == null;
        }

        //Add a new user to the database
        public async Task AddNewUser(string username, string password)
        {
            await _database.Users.InsertOneAsync(new User(username, password));
        }

        //Remove user completely from the database
        public async Task DeleteUser(string username)
        {
            await _database.Users.DeleteOneAsync(u => u.Username == username);
        }

        #endregion

        #region Friends

        public async Task AddRequest(string sender, string receiver)
        {
            //Add OpenFriendRequest of sender to receiver
            var updateReceiving = Builders<User>.Update.AddToSet(u => u.OpenFriendRequests, sender);
            await _database.Users.UpdateOneAsync(u => u.Username == receiver, updateReceiving);

            //Add OpenFriendRequest of receiver to sender
            var updateSender = Builders<User>.Update.AddToSet(u => u.UnansweredFriendRequests, receiver);
            await _database.Users.UpdateOneAsync(u => u.Username == sender, updateSender);
        }

        public async Task RemoveRequest(string sender, string receiver)
        {
            //Remove OpenFriendRequest of receiver from sender
            var updateSender = Builders<User>.Update.Pull(u => u.OpenFriendRequests, receiver);
            await _database.Users.UpdateOneAsync(u => u.Username == sender, updateSender);

            //Remove OpenFriendRequest of sender from receiver
            var updateReceiving = Builders<User>.Update.Pull(u => u.UnansweredFriendRequests, sender);
            await _database.Users.UpdateOneAsync(u => u.Username == receiver, updateReceiving);
        }

        public async Task AddFriend(string sender, string receiver)
        {
            //Add sender to receivers friend list
            var updateReceiving = Builders<User>.Update.AddToSet(u => u.Friends, sender);
            await _database.Users.UpdateOneAsync(u => u.Username == receiver, updateReceiving);

            //Add receiver to senders friend list
            var updateSending = Builders<User>.Update.AddToSet(u => u.Friends, receiver);
            await _database.Users.UpdateOneAsync(u => u.Username == sender, updateSending);
        }

        public async Task RemoveFriend(string sender, string receiver)
        {
            var senderUser = await _database.Users.Find(u => u.Username == sender).FirstOrDefaultAsync();
            var receiverUser = await _database.Users.Find(u => u.Username == receiver).FirstOrDefaultAsync();

            //Update sender
            if (senderUser.Friends.Contains(receiver))
            {
                //remove receiver from senders friend list
                var updateSender = Builders<User>.Update.Pull(u => u.Friends, receiver);
                await _database.Users.UpdateOneAsync(u => u.Username == sender, updateSender);
            }
            if (senderUser.OpenFriendRequests.Contains(receiver))
            {
                //remove receiver from senders open friend requests
                var updateSender = Builders<User>.Update.Pull(u => u.OpenFriendRequests, receiver);
                await _database.Users.UpdateOneAsync(u => u.Username == sender, updateSender);
            }
            if (senderUser.UnansweredFriendRequests.Contains(receiver))
            {
                // remove receiver from senders unanswered friend requests
                var updateSender = Builders<User>.Update.Pull(u => u.UnansweredFriendRequests, receiver);
                await _database.Users.UpdateOneAsync(u => u.Username == sender, updateSender);
            }

            //Update receiver
            if (receiverUser.Friends.Contains(sender))
            {
                //remove sender from receivers friend list
                var updateReceiver = Builders<User>.Update.Pull(u => u.Friends, sender);
                await _database.Users.UpdateOneAsync(u => u.Username == receiver, updateReceiver);
            }
            if (receiverUser.OpenFriendRequests.Contains(sender))
            {
                //remove sender from receivers open friend requests
                var updateReceiver = Builders<User>.Update.Pull(u => u.OpenFriendRequests, sender);
                await _database.Users.UpdateOneAsync(u => u.Username == receiver, updateReceiver);
            }
            if (receiverUser.UnansweredFriendRequests.Contains(sender))
            {
                //remove sender from receivers unanswered friend requests
                var updateReceiver = Builders<User>.Update.Pull(u => u.UnansweredFriendRequests, sender);
                await _database.Users.UpdateOneAsync(u => u.Username == receiver, updateReceiver);
            }
        }

        #endregion
    }
}