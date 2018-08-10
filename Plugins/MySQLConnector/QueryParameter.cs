using MySql.Data.MySqlClient;
namespace MySQLConnector
{
    public class QueryParameter
    {
        public string ParameterName { get; }
        public MySqlDbType FieldType { get; }
        public int Size { get; }
        public string Column { get; }
        public object Value { get; }

        public QueryParameter(string parameterName, MySqlDbType type, int size, string col, object obj)
        {
            ParameterName = parameterName;
            FieldType = type;
            Size = size;
            Column = col;
            Value = obj;
        }
    }
}