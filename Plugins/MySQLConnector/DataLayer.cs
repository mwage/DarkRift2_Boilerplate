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

        public IUser GetUser(string username)
        {
            var row = _database.ExecuteQuery(
                "SELECT id FROM users WHERE username = @username LIMIT 1 ",
                new QueryParameter("username", _database.EscapeString(username)))[0];
            return new User(Convert.ToString(row["username"]), Convert.ToString(row["password"]), null, null, null);
        }

        public bool UsernameAvailable(string username)
        {
            throw new NotImplementedException();
        }

        public void AddNewUser(string username, string password)
        {
            throw new NotImplementedException();
        }

        public void DeleteUser(string username)
        {
            throw new NotImplementedException();
        }

        public void AddRequest(string sender, string receiver)
        {
            throw new NotImplementedException();
        }

        public void RemoveRequest(string sender, string receiver)
        {
            throw new NotImplementedException();
        }

        public void AddFriend(string sender, string receiver)
        {
            throw new NotImplementedException();
        }

        public void RemoveFriend(string sender, string receiver)
        {
            throw new NotImplementedException();
        }
    }
}