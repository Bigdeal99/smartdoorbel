using Npgsql;
using System;
using Infrastructure;

public class DatabaseManager
{
    private readonly string _connectionString;

    public DatabaseManager()
    {
        _connectionString = Utilities.ProperlyFormattedConnectionString;
        InitializeDatabase();
    }

    private void InitializeDatabase()
    {
        using (var connection = new NpgsqlConnection(_connectionString))
        {
            connection.Open();
            var command = connection.CreateCommand();
            command.CommandText = @"
                CREATE TABLE IF NOT EXISTS StreamMessages (
                    Id SERIAL PRIMARY KEY,
                    MessageType TEXT,
                    Timestamp TIMESTAMPTZ
                )";
            command.ExecuteNonQuery();
        }
    }

    public void SaveMessage(string messageType)
    {
        using (var connection = new NpgsqlConnection(_connectionString))
        {
            connection.Open();
            var command = connection.CreateCommand();
            command.CommandText = @"
                INSERT INTO StreamMessages (MessageType, Timestamp)
                VALUES (@messageType, @timestamp)";
            command.Parameters.AddWithValue("@messageType", messageType);
            command.Parameters.AddWithValue("@timestamp", DateTime.UtcNow);
            command.ExecuteNonQuery();
        }
    }
}