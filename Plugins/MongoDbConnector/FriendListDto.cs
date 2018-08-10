using System.Collections.Generic;
using Database;

namespace MongoDbConnector
{
    //DTO for returning an IFriendList object
    public class FriendListDto : IFriendList
    {
        public List<string> Friends { get; }
        public List<string> OpenFriendRequests { get; }
        public List<string> UnansweredFriendRequests { get; }

        public FriendListDto(FriendList friends)
        {
            Friends = friends.Friends;
            OpenFriendRequests = friends.OpenFriendRequests;
            UnansweredFriendRequests = friends.UnansweredFriendRequests;
        }
    }
}