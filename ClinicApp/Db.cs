using System.Data;
using Microsoft.Data.SqlClient;

namespace ClinicApp;

public static class Db
{
    private static string ConnectionString =>
        Environment.GetEnvironmentVariable("CLINIC_DB_CONNECTION")
        ?? throw new InvalidOperationException(
            "Задайте строку подключения в переменной окружения CLINIC_DB_CONNECTION.");

    public static SqlConnection Open()
    {
        var connection = new SqlConnection(ConnectionString);
        connection.Open();
        return connection;
    }

    public static DataTable Query(string sql, params SqlParameter[] parameters)
    {
        using var connection = Open();
        using var command = new SqlCommand(sql, connection);
        command.Parameters.AddRange(parameters);
        using var adapter = new SqlDataAdapter(command);
        var table = new DataTable();
        adapter.Fill(table);
        return table;
    }

    public static int Execute(string sql, params SqlParameter[] parameters)
    {
        using var connection = Open();
        using var command = new SqlCommand(sql, connection);
        command.Parameters.AddRange(parameters);
        return command.ExecuteNonQuery();
    }
}
