using System.Collections.Generic;
using System.Linq;

namespace MySQLConnector
{
    public class DatabaseRow
    {
        private readonly Dictionary<string, object> _rowItems = new Dictionary<string, object>();

        public void Add(string key, object value)
        {
            _rowItems.Add(key, value);
        }

        public object this[string id] => _rowItems[id];

        public Dictionary<string, object> GetRow() => _rowItems;
        public object[] GetAllValues => _rowItems.Values.ToArray();
    }
}