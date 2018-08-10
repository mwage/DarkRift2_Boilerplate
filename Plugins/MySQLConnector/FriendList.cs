using System.Collections.Generic;
using Database;

namespace MySQLConnector
{
    public class FriendList : IFriendList
    {
        public List<string> Friends { get; } = new List<string>();
        public List<string> OpenFriendRequests { get; } = new List<string>();
        public List<string> UnansweredFriendRequests { get; } = new List<string>();

        public FriendList(IEnumerable<string> friends, IEnumerable<string> outRequests, IEnumerable<string> inRequests)
        {
            Friends.AddRange(friends);
            OpenFriendRequests.AddRange(outRequests);
            UnansweredFriendRequests.AddRange(inRequests);
        }
    }
}