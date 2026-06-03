using System;
using System.Data;
using System.Data.SqlClient;

public sealed class ExpenseChatbotRepository
{
    private readonly string _connectionString;
    private readonly int _commandTimeoutSeconds;

    public ExpenseChatbotRepository()
        : this(ExpenseChatbotConfig.GetConnectionString(), ExpenseChatbotConfig.GetCommandTimeoutSeconds())
    {
    }

    public ExpenseChatbotRepository(string connectionString, int commandTimeoutSeconds)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new ArgumentException("A SQL connection string is required.", "connectionString");
        }

        _connectionString = connectionString;
        _commandTimeoutSeconds = commandTimeoutSeconds > 0 ? commandTimeoutSeconds : 30;
    }

    public DataTable Execute(ExpenseChatbotQueryDefinition definition, string input)
    {
        if (definition == null)
        {
            throw new ArgumentNullException("definition");
        }

        DataTable results = new DataTable();

        using (SqlConnection connection = new SqlConnection(_connectionString))
        using (SqlCommand command = new SqlCommand(definition.StoredProcedureName, connection))
        using (SqlDataAdapter adapter = new SqlDataAdapter(command))
        {
            command.CommandType = CommandType.StoredProcedure;
            command.CommandTimeout = _commandTimeoutSeconds;
            command.Parameters.Add(definition.ParameterName, SqlDbType.NVarChar, 200).Value = input;

            connection.Open();
            adapter.Fill(results);
        }

        return results;
    }
}
