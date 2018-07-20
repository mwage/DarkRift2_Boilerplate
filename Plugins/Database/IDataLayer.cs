using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Database
{
    public interface IDataLayer
    {
        string Name { get; }

        #region Login

        IUser GetUser(string username);
        bool UsernameAvailable(string username);
        void AddNewUser(string username, string password);
        void DeleteUser(string username);

        #endregion

        #region Friends

        void AddRequest(string sender, string receiver);
        void RemoveRequest(string sender, string receiver);
        void AddFriend(string sender, string receiver);
        void RemoveFriend(string sender, string receiver);

        #endregion
    }
}
