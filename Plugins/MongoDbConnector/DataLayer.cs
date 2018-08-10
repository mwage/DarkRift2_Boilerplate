using System;
using System.Collections.Generic;
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

        public async void GetUser(string username, Action<IUser> callback)
        {
            //Gets user data from the database and builds a User object to send back
            var user = await _database.Users.Find(u => u.Username == username).FirstOrDefaultAsync();
            callback(user);
        }

        public async void UsernameAvailable(string username, Action<bool> callback)
        {
            //Checks if a username is already taken
            callback(await _database.Users.Find(u => u.Username == username).FirstOrDefaultAsync() == null);
        }

        public async void AddNewUser(string username, string password, Action callback)
        {
            //Add a new user to the database
            await _database.Users.InsertOneAsync(new User(username, password));
            await _database.FriendLists.InsertOneAsync(new FriendList(username));
            callback();
        }

        public async void DeleteUser(string username, Action callback)
        {
            //Remove user completely from the database
            await _database.Users.DeleteOneAsync(u => u.Username == username);
            callback();
        }

        #endregion

        #region Friends

        public async void AddRequest(string sender, string receiver, Action callback)
        {
            //Add OpenFriendRequest of sender to receiver
            var updateReceiving = Builders<FriendList>.Update.AddToSet(u => u.OpenFriendRequests, sender);
            var task1 = _database.FriendLists.UpdateOneAsync(u => u.Username == receiver, updateReceiving);

            //Add OpenFriendRequest of receiver to sender
            var updateSender = Builders<FriendList>.Update.AddToSet(u => u.UnansweredFriendRequests, receiver);
            var task2 = _database.FriendLists.UpdateOneAsync(u => u.Username == sender, updateSender);

            await Task.WhenAll(task1, task2);
            callback();
        }

        public async void RemoveRequest(string sender, string receiver, Action callback)
        {
            //Remove OpenFriendRequest of receiver from sender
            var updateSender = Builders<FriendList>.Update.Pull(u => u.OpenFriendRequests, receiver);
            var task1 = _database.FriendLists.UpdateOneAsync(u => u.Username == sender, updateSender);

            //Remove OpenFriendRequest of sender from receiver
            var updateReceiving = Builders<FriendList>.Update.Pull(u => u.UnansweredFriendRequests, sender);
            var task2 = _database.FriendLists.UpdateOneAsync(u => u.Username == receiver, updateReceiving);

            await Task.WhenAll(task1, task2);
            callback();
        }

        public async void AddFriend(string sender, string receiver, Action callback)
        {
            var tasks = new List<Task>();
            //Add sender to receivers friend list
            var updateReceiving = Builders<FriendList>.Update.AddToSet(u => u.Friends, sender);
            tasks.Add(_database.FriendLists.UpdateOneAsync(u => u.Username == receiver, updateReceiving));

            //Add receiver to senders friend list
            var updateSending = Builders<FriendList>.Update.AddToSet(u => u.Friends, receiver);
            tasks.Add(_database.FriendLists.UpdateOneAsync(u => u.Username == sender, updateSending));

            //Remove OpenFriendRequest of receiver from sender
            var updateSender = Builders<FriendList>.Update.Pull(u => u.OpenFriendRequests, receiver);
            tasks.Add(_database.FriendLists.UpdateOneAsync(u => u.Username == sender, updateSender));

            //Remove OpenFriendRequest of sender from receiver
            var updateReceiver = Builders<FriendList>.Update.Pull(u => u.UnansweredFriendRequests, sender);
            tasks.Add(_database.FriendLists.UpdateOneAsync(u => u.Username == receiver, updateReceiver));

            await Task.WhenAll(tasks);
            callback();
        }

        public void RemoveFriend(string sender, string receiver, Action callback)
        {
            GetFriendLists(new[] {sender, receiver}, async friendLists =>
            {
                var senderUser = friendLists.Single(u => u.Username == sender);
                var receiverUser = friendLists.Single(u => u.Username == receiver);

                var tasks = new List<Task>();

                //Update sender
                if (senderUser.Friends.Contains(receiver))
                {
                    //remove receiver from senders friend list
                    var updateSender = Builders<FriendList>.Update.Pull(u => u.Friends, receiver);
                    tasks.Add(_database.FriendLists.UpdateOneAsync(u => u.Username == sender, updateSender));
                }
                if (senderUser.OpenFriendRequests.Contains(receiver))
                {
                    //remove receiver from senders open friend requests
                    var updateSender = Builders<FriendList>.Update.Pull(u => u.OpenFriendRequests, receiver);
                    tasks.Add(_database.FriendLists.UpdateOneAsync(u => u.Username == sender, updateSender));
                }
                if (senderUser.UnansweredFriendRequests.Contains(receiver))
                {
                    // remove receiver from senders unanswered friend requests
                    var updateSender = Builders<FriendList>.Update.Pull(u => u.UnansweredFriendRequests, receiver);
                    tasks.Add(_database.FriendLists.UpdateOneAsync(u => u.Username == sender, updateSender));
                }

                //Update receiver
                if (receiverUser.Friends.Contains(sender))
                {
                    //remove sender from receivers friend list
                    var updateReceiver = Builders<FriendList>.Update.Pull(u => u.Friends, sender);
                    tasks.Add(_database.FriendLists.UpdateOneAsync(u => u.Username == receiver, updateReceiver));
                }
                if (receiverUser.OpenFriendRequests.Contains(sender))
                {
                    //remove sender from receivers open friend requests
                    var updateReceiver = Builders<FriendList>.Update.Pull(u => u.OpenFriendRequests, sender);
                    tasks.Add(_database.FriendLists.UpdateOneAsync(u => u.Username == receiver, updateReceiver));
                }
                if (receiverUser.UnansweredFriendRequests.Contains(sender))
                {
                    //remove sender from receivers unanswered friend requests
                    var updateReceiver = Builders<FriendList>.Update.Pull(u => u.UnansweredFriendRequests, sender);
                    tasks.Add(_database.FriendLists.UpdateOneAsync(u => u.Username == receiver, updateReceiver));
                }

                await Task.WhenAll(tasks);
                callback();
            });
        }

        public async void GetFriends(string username, Action<IFriendList> callback)
        {
            //Gets friendlist data from the database and builds a User object to send back
            var friendList = await _database.FriendLists.Find(u => u.Username == username).FirstOrDefaultAsync();
            if (friendList == null)
            {
                await _database.FriendLists.InsertOneAsync(new FriendList(username));
                friendList = new FriendList(username);
            }
            callback(new FriendListDto(friendList));
        }

        #endregion

        #region Helper

        private async void GetFriendLists(IEnumerable<string> usernames, Action<FriendList[]> callback)
        {
            var friendLists = new List<FriendList>();
            var tasks = usernames.Select(username => _database.FriendLists.Find(u => u.Username == username).FirstOrDefaultAsync()).ToList();

            await Task.WhenAll(tasks);

            foreach (var task in tasks)
            {
                var friendList = await task;
                friendLists.Add(friendList);
            }

            callback(friendLists.ToArray());
        }

        #endregion
    }
}