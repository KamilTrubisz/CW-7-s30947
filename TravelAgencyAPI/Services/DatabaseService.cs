using Microsoft.Data.SqlClient;
using System.Data;

namespace TravelAgencyAPI.Services;

public interface IDatabaseService
{
    Task<SqlConnection> GetConnection();
    Task<int> ExecuteNonQueryAsync(string sql, params SqlParameter[] parameters);
    Task<SqlDataReader> ExecuteReaderAsync(string sql, params SqlParameter[] parameters);
    Task<T> ExecuteScalarAsync<T>(string sql, params SqlParameter[] parameters);
}

public class DatabaseService : IDatabaseService
{
    private readonly string _connectionString;

    public DatabaseService(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")!;
    }

    public async Task<SqlConnection> GetConnection()
    {
        var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();
        return connection;
    }

    public async Task<int> ExecuteNonQueryAsync(string sql, params SqlParameter[] parameters)
    {
        await using var connection = await GetConnection();
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddRange(parameters);
        return await command.ExecuteNonQueryAsync();
    }

    public async Task<SqlDataReader> ExecuteReaderAsync(string sql, params SqlParameter[] parameters)
    {
        var connection = await GetConnection();
        var command = new SqlCommand(sql, connection);
        command.Parameters.AddRange(parameters);
        return await command.ExecuteReaderAsync(CommandBehavior.CloseConnection);
    }

    public async Task<T> ExecuteScalarAsync<T>(string sql, params SqlParameter[] parameters)
    {
        await using var connection = await GetConnection();
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddRange(parameters);
        var result = await command.ExecuteScalarAsync();
        return (T)Convert.ChangeType(result, typeof(T));
    }
}