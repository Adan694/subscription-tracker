using Microsoft.Data.Sqlite;
using Backend.Models;

namespace Backend;

public class DatabaseHelper
{
    private readonly string _connectionString;

    public DatabaseHelper()
    {
        var dbPath = Path.Combine(Directory.GetCurrentDirectory(), "subscriptions.db");
        _connectionString = $"Data Source={dbPath}";
        InitializeDatabase();
    }

    private void InitializeDatabase()
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        var createUsersTable = @"
            CREATE TABLE IF NOT EXISTS Users (
                Id TEXT PRIMARY KEY,
                Email TEXT UNIQUE NOT NULL,
                PasswordHash TEXT NOT NULL,
                CreatedAt TEXT NOT NULL
            )";

        using var cmd1 = new SqliteCommand(createUsersTable, connection);
        cmd1.ExecuteNonQuery();

        var createSubscriptionsTable = @"
            CREATE TABLE IF NOT EXISTS Subscriptions (
                Id TEXT PRIMARY KEY,
                UserId TEXT NOT NULL,
                Name TEXT NOT NULL,
                Amount REAL NOT NULL,
                Frequency TEXT NOT NULL,
                NextChargeDate TEXT NOT NULL,
                CancellationLink TEXT,
                IsActive INTEGER NOT NULL,
                CreatedAt TEXT NOT NULL,
                FOREIGN KEY (UserId) REFERENCES Users(Id)
            )";

        using var cmd2 = new SqliteCommand(createSubscriptionsTable, connection);
        cmd2.ExecuteNonQuery();
    }

    public void CreateUser(User user)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        var sql = @"INSERT INTO Users (Id, Email, PasswordHash, CreatedAt) 
                    VALUES (@Id, @Email, @PasswordHash, @CreatedAt)";

        using var cmd = new SqliteCommand(sql, connection);
        cmd.Parameters.AddWithValue("@Id", user.Id);
        cmd.Parameters.AddWithValue("@Email", user.Email);
        cmd.Parameters.AddWithValue("@PasswordHash", user.PasswordHash);
        cmd.Parameters.AddWithValue("@CreatedAt", user.CreatedAt.ToString("o"));

        cmd.ExecuteNonQuery();
    }

    public User? GetUserByEmail(string email)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        var sql = "SELECT Id, Email, PasswordHash, CreatedAt FROM Users WHERE Email = @Email";

        using var cmd = new SqliteCommand(sql, connection);
        cmd.Parameters.AddWithValue("@Email", email);

        using var reader = cmd.ExecuteReader();
        if (reader.Read())
        {
            return new User
            {
                Id = reader.GetString(0),
                Email = reader.GetString(1),
                PasswordHash = reader.GetString(2),
                CreatedAt = DateTime.Parse(reader.GetString(3))
            };
        }
        return null;
    }

    public void CreateSubscription(Subscription subscription)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        var sql = @"INSERT INTO Subscriptions (Id, UserId, Name, Amount, Frequency, NextChargeDate, CancellationLink, IsActive, CreatedAt) 
                    VALUES (@Id, @UserId, @Name, @Amount, @Frequency, @NextChargeDate, @CancellationLink, @IsActive, @CreatedAt)";

        using var cmd = new SqliteCommand(sql, connection);
        cmd.Parameters.AddWithValue("@Id", subscription.Id);
        cmd.Parameters.AddWithValue("@UserId", subscription.UserId);
        cmd.Parameters.AddWithValue("@Name", subscription.Name);
        cmd.Parameters.AddWithValue("@Amount", subscription.Amount);
        cmd.Parameters.AddWithValue("@Frequency", subscription.Frequency);
        cmd.Parameters.AddWithValue("@NextChargeDate", subscription.NextChargeDate.ToString("o"));
        cmd.Parameters.AddWithValue("@CancellationLink", subscription.CancellationLink ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@IsActive", subscription.IsActive ? 1 : 0);
        cmd.Parameters.AddWithValue("@CreatedAt", subscription.CreatedAt.ToString("o"));

        cmd.ExecuteNonQuery();
    }

    public List<Subscription> GetUserSubscriptions(string userId)
    {
        var subscriptions = new List<Subscription>();

        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        var sql = @"SELECT Id, UserId, Name, Amount, Frequency, NextChargeDate, CancellationLink, IsActive, CreatedAt 
                    FROM Subscriptions 
                    WHERE UserId = @UserId AND IsActive = 1 
                    ORDER BY NextChargeDate";

        using var cmd = new SqliteCommand(sql, connection);
        cmd.Parameters.AddWithValue("@UserId", userId);

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            subscriptions.Add(new Subscription
            {
                Id = reader.GetString(0),
                UserId = reader.GetString(1),
                Name = reader.GetString(2),
                Amount = reader.GetDecimal(3),
                Frequency = reader.GetString(4),
                NextChargeDate = DateTime.Parse(reader.GetString(5)),
                CancellationLink = reader.IsDBNull(6) ? null : reader.GetString(6),
                IsActive = reader.GetInt32(7) == 1,
                CreatedAt = DateTime.Parse(reader.GetString(8))
            });
        }

        return subscriptions;
    }

    public void DeleteSubscription(string subscriptionId)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        var sql = "UPDATE Subscriptions SET IsActive = 0 WHERE Id = @Id";

        using var cmd = new SqliteCommand(sql, connection);
        cmd.Parameters.AddWithValue("@Id", subscriptionId);

        cmd.ExecuteNonQuery();
    }

    public Subscription? GetSubscription(string subscriptionId)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        var sql = @"SELECT Id, UserId, Name, Amount, Frequency, NextChargeDate, CancellationLink, IsActive, CreatedAt 
                    FROM Subscriptions WHERE Id = @Id";

        using var cmd = new SqliteCommand(sql, connection);
        cmd.Parameters.AddWithValue("@Id", subscriptionId);

        using var reader = cmd.ExecuteReader();
        if (reader.Read())
        {
            return new Subscription
            {
                Id = reader.GetString(0),
                UserId = reader.GetString(1),
                Name = reader.GetString(2),
                Amount = reader.GetDecimal(3),
                Frequency = reader.GetString(4),
                NextChargeDate = DateTime.Parse(reader.GetString(5)),
                CancellationLink = reader.IsDBNull(6) ? null : reader.GetString(6),
                IsActive = reader.GetInt32(7) == 1,
                CreatedAt = DateTime.Parse(reader.GetString(8))
            };
        }
        return null;
    }
}