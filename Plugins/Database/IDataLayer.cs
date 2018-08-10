using System;
using System.Collections.Generic;

namespace Database
{
    public interface IDataLayer
    {
        string Name { get; }

        #region Login

        void GetUser(string username, Action<IUser> callback);
        void UsernameAvailable(string username, Action<bool> callback);
        void AddNewUser(string username, string password, Action callback);
        void DeleteUser(string username, Action callback);

        #endregion

        #region Friends

        void AddRequest(string sender, string receiver, Action callback);
        void RemoveRequest(string sender, string receiver, Action callback);
        void AddFriend(string sender, string receiver, Action callback);
        void RemoveFriend(string sender, string receiver, Action callback);
        void GetFriends(string username, Action<IFriendList> callback);

        #endregion
    }
}