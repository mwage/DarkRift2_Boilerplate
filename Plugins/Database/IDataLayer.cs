using System.Threading.Tasks;

namespace Database
{
    public interface IDataLayer
    {
        string Name { get; }

        #region Login

        Task<IUser> GetUser(string username);
        Task<bool> UsernameAvailable(string username);
        Task AddNewUser(string username, string password);
        Task DeleteUser(string username);

        #endregion

        #region Friends

        Task AddRequest(string sender, string receiver);
        Task RemoveRequest(string sender, string receiver);
        Task AddFriend(string sender, string receiver);
        Task RemoveFriend(string sender, string receiver);

        #endregion
    }
}