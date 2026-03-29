using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Data.Sqlite;
using System.Text.Json;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        policy =>
        {
            policy.AllowAnyOrigin()
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        });
});

var app = builder.Build();
app.UseCors("AllowAll");

// Database setup
var dbPath = Path.Combine(Directory.GetCurrentDirectory(), "subscriptions.db");
var connectionString = $"Data Source={dbPath}";

// Initialize database
using (var conn = new SqliteConnection(connectionString))
{
    conn.Open();

    var createUsers = conn.CreateCommand();
    createUsers.CommandText = @"
        CREATE TABLE IF NOT EXISTS Users (
            Id TEXT PRIMARY KEY,
            Email TEXT UNIQUE NOT NULL,
            PasswordHash TEXT NOT NULL,
            CreatedAt TEXT NOT NULL
        )";
    createUsers.ExecuteNonQuery();

    var createSubs = conn.CreateCommand();
    createSubs.CommandText = @"
        CREATE TABLE IF NOT EXISTS Subscriptions (
            Id TEXT PRIMARY KEY,
            UserId TEXT NOT NULL,
            Name TEXT NOT NULL,
            Amount REAL NOT NULL,
            Frequency TEXT NOT NULL,
            NextChargeDate TEXT NOT NULL,
            CancellationLink TEXT,
            IsActive INTEGER NOT NULL,
            CreatedAt TEXT NOT NULL
        )";
    createSubs.ExecuteNonQuery();
}

string HashPassword(string password) => BCrypt.Net.BCrypt.HashPassword(password);
bool VerifyPassword(string password, string hash) => BCrypt.Net.BCrypt.Verify(password, hash);

// Health check
app.MapGet("/", () => Results.Ok(new { message = "API is running!" }));

// REGISTER
app.MapPost("/api/register", async (HttpRequest request) =>
{
    try
    {
        using var reader = new StreamReader(request.Body);
        var body = await reader.ReadToEndAsync();
        var json = JsonSerializer.Deserialize<Dictionary<string, string>>(body);

        if (json == null || !json.ContainsKey("email") || !json.ContainsKey("password"))
        {
            return Results.BadRequest(new { error = "Email and password required" });
        }

        var email = json["email"].ToLower();
        var password = json["password"];

        using var conn = new SqliteConnection(connectionString);
        conn.Open();

        var checkCmd = conn.CreateCommand();
        checkCmd.CommandText = "SELECT COUNT(*) FROM Users WHERE Email = @email";
        checkCmd.Parameters.AddWithValue("@email", email);
        var exists = Convert.ToInt32(checkCmd.ExecuteScalar()) > 0;

        if (exists)
        {
            return Results.BadRequest(new { error = "Email already exists" });
        }

        var userId = Guid.NewGuid().ToString();
        var insertCmd = conn.CreateCommand();
        insertCmd.CommandText = "INSERT INTO Users (Id, Email, PasswordHash, CreatedAt) VALUES (@id, @email, @hash, @created)";
        insertCmd.Parameters.AddWithValue("@id", userId);
        insertCmd.Parameters.AddWithValue("@email", email);
        insertCmd.Parameters.AddWithValue("@hash", HashPassword(password));
        insertCmd.Parameters.AddWithValue("@created", DateTime.UtcNow.ToString("o"));
        insertCmd.ExecuteNonQuery();

        return Results.Ok(new { success = true, userId = userId });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

// LOGIN
app.MapPost("/api/login", async (HttpRequest request) =>
{
    try
    {
        using var reader = new StreamReader(request.Body);
        var body = await reader.ReadToEndAsync();
        var json = JsonSerializer.Deserialize<Dictionary<string, string>>(body);

        if (json == null || !json.ContainsKey("email") || !json.ContainsKey("password"))
        {
            return Results.BadRequest(new { error = "Email and password required" });
        }

        var email = json["email"].ToLower();
        var password = json["password"];

        using var conn = new SqliteConnection(connectionString);
        conn.Open();

        var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT Id, PasswordHash FROM Users WHERE Email = @email";
        cmd.Parameters.AddWithValue("@email", email);

        using var reader2 = cmd.ExecuteReader();
        if (reader2.Read())
        {
            var userId = reader2.GetString(0);
            var hash = reader2.GetString(1);

            if (VerifyPassword(password, hash))
            {
                return Results.Ok(new { success = true, userId = userId, email = email });
            }
        }

        return Results.Json(new { error = "Invalid credentials" }, statusCode: 401);
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

// GET SUBSCRIPTIONS
app.MapGet("/api/subscriptions/{userId}", (string userId) =>
{
    try
    {
        var subscriptions = new List<object>();

        using var conn = new SqliteConnection(connectionString);
        conn.Open();

        var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT Id, Name, Amount, Frequency, NextChargeDate, CancellationLink FROM Subscriptions WHERE UserId = @userId AND IsActive = 1 ORDER BY NextChargeDate";
        cmd.Parameters.AddWithValue("@userId", userId);

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            subscriptions.Add(new
            {
                id = reader.GetString(0),
                name = reader.GetString(1),
                amount = reader.GetDecimal(2),
                frequency = reader.GetString(3),
                nextChargeDate = DateTime.Parse(reader.GetString(4)),
                cancellationLink = reader.IsDBNull(5) ? null : reader.GetString(5)
            });
        }

        decimal monthlyTotal = 0;
        foreach (var sub in subscriptions)
        {
            var freq = sub.GetType().GetProperty("frequency")?.GetValue(sub)?.ToString();
            if (freq == "monthly")
            {
                var amt = (decimal)(sub.GetType().GetProperty("amount")?.GetValue(sub) ?? 0m);
                monthlyTotal += amt;
            }
        }

        return Results.Ok(new { subscriptions = subscriptions, monthlyTotal = monthlyTotal });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

// ADD SUBSCRIPTION - FIXED VERSION
app.MapPost("/api/subscriptions", async (HttpRequest request) =>
{
    try
    {
        using var reader = new StreamReader(request.Body);
        var body = await reader.ReadToEndAsync();

        // Parse JSON manually to avoid type conversion issues
        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;

        // Extract values safely
        if (!root.TryGetProperty("userId", out var userIdElement) ||
            !root.TryGetProperty("name", out var nameElement) ||
            !root.TryGetProperty("amount", out var amountElement))
        {
            return Results.BadRequest(new { error = "userId, name, and amount are required" });
        }

        var userId = userIdElement.GetString();
        var name = nameElement.GetString();
        decimal amount = amountElement.GetDecimal();

        var frequency = "monthly";
        if (root.TryGetProperty("frequency", out var freqElement))
        {
            frequency = freqElement.GetString() ?? "monthly";
        }

        DateTime nextChargeDate = DateTime.UtcNow.AddDays(30);
        if (root.TryGetProperty("nextChargeDate", out var dateElement))
        {
            var dateStr = dateElement.GetString();
            if (!string.IsNullOrEmpty(dateStr) && DateTime.TryParse(dateStr, out var parsedDate))
            {
                nextChargeDate = parsedDate;
            }
        }

        string? cancellationLink = null;
        if (root.TryGetProperty("cancellationLink", out var linkElement))
        {
            cancellationLink = linkElement.GetString();
        }

        var id = Guid.NewGuid().ToString();

        using var conn = new SqliteConnection(connectionString);
        conn.Open();

        var cmd = conn.CreateCommand();
        cmd.CommandText = @"INSERT INTO Subscriptions (Id, UserId, Name, Amount, Frequency, NextChargeDate, CancellationLink, IsActive, CreatedAt) 
                            VALUES (@id, @userId, @name, @amount, @frequency, @nextDate, @cancelLink, 1, @created)";
        cmd.Parameters.AddWithValue("@id", id);
        cmd.Parameters.AddWithValue("@userId", userId);
        cmd.Parameters.AddWithValue("@name", name);
        cmd.Parameters.AddWithValue("@amount", amount);
        cmd.Parameters.AddWithValue("@frequency", frequency);
        cmd.Parameters.AddWithValue("@nextDate", nextChargeDate.ToString("o"));
        cmd.Parameters.AddWithValue("@cancelLink", cancellationLink ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@created", DateTime.UtcNow.ToString("o"));
        cmd.ExecuteNonQuery();

        return Results.Ok(new { success = true, subscriptionId = id });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

// DELETE SUBSCRIPTION
app.MapDelete("/api/subscriptions/{subscriptionId}", (string subscriptionId) =>
{
    try
    {
        using var conn = new SqliteConnection(connectionString);
        conn.Open();

        var cmd = conn.CreateCommand();
        cmd.CommandText = "UPDATE Subscriptions SET IsActive = 0 WHERE Id = @id";
        cmd.Parameters.AddWithValue("@id", subscriptionId);
        cmd.ExecuteNonQuery();

        return Results.Ok(new { success = true });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

app.Run();