using System;
using Database;

namespace MySQLConnector
{
    internal class DataLayer : IDataLayer
    {
        private readonly MySqlConnector _database;

        public DataLayer(string name, MySqlConnector database)
        {
            Name = name;
            _database = database;
        }

        public string Name { get; }
        public void GetUser(string username, Action<IUser> callback)
        {
            var row = _database.ExecuteQuery(
                "SELECT id FROM users WHERE username = @username LIMIT 1 ",
                new QueryParameter("username", _database.EscapeString(username)))[0];
            callback(new User(Convert.ToString(row["username"]), Convert.ToString(row["password"]), null, null, null));
        }

        public void UsernameAvailable(string username, Action<bool> callback)
        {
            throw new NotImplementedException();
        }

        public void AddNewUser(string username, string password, Action callback)
        {
            throw new NotImplementedException();
        }

        public void DeleteUser(string username, Action callback)
        {
            throw new NotImplementedException();
        }

        public void AddRequest(string sender, string receiver, Action callback)
        {
            throw new NotImplementedException();
        }

        public void RemoveRequest(string sender, string receiver, Action callback)
        {
            throw new NotImplementedException();
        }

        public void AddFriend(string sender, string receiver, Action callback)
        {
            throw new NotImplementedException();
        }

        public void RemoveFriend(string sender, string receiver, Action callback)
        {
            throw new NotImplementedException();
        }
    }
}