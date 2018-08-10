using System.Collections.Generic;
using Database;

namespace MySQLConnector
{
    internal class User : IUser
    {
        public string Username { get; }
        public string Password { get; }

        public User(string username, string password)
        {
            Username = username;
            Password = password;
        }
    }
}