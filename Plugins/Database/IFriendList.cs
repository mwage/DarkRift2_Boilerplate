using System.Collections.Generic;

namespace Database
{
    public interface IFriendList
    {
        List<string> Friends { get; }
        List<string> OpenFriendRequests { get; }
        List<string> UnansweredFriendRequests { get; }
    }
}