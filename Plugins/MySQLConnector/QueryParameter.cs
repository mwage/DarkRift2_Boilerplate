namespace MySQLConnector
{
    public class QueryParameter
    {
        public string ParameterName { get; }
        public object Value { get; }

        public QueryParameter(string parameterName, object obj)
        {
            ParameterName = parameterName;
            Value = obj;
        }
    }
}