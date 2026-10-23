using Microsoft.Data.Sqlite;
using StarGymApp.Core.Models;

namespace StarGymApp.Core.Repositories;

public sealed class SqlitePaymentRepository : IRepository<Payment>
{
    private readonly string connectionString;

    public SqlitePaymentRepository(string databasePath)
    {
        connectionString = new SqliteConnectionStringBuilder { DataSource = databasePath, Pooling = false }.ToString();
    }

    public async Task InitializeAsync(IEnumerable<Payment>? seedPayments = null)
    {
        await using var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE IF NOT EXISTS Payments (
                Id INTEGER PRIMARY KEY,
                MemberId INTEGER NOT NULL,
                Amount NUMERIC NOT NULL,
                PaymentDate TEXT NOT NULL,
                PaymentMethod TEXT NOT NULL,
                InvoiceNumber TEXT NOT NULL,
                Status TEXT NOT NULL,
                Notes TEXT NOT NULL
            );
            """;
        await command.ExecuteNonQueryAsync();

        if (seedPayments is null) return;
        using var seedTransaction = connection.BeginTransaction();
        await using var seedCommand = connection.CreateCommand();
        seedCommand.Transaction = seedTransaction;
        seedCommand.CommandText = "INSERT OR IGNORE INTO Payments (Id, MemberId, Amount, PaymentDate, PaymentMethod, InvoiceNumber, Status, Notes) VALUES ($id, $memberId, $amount, $date, $method, $invoice, $status, $notes)";
        var hasParameters = false;
        foreach (var payment in seedPayments)
        {
            if (hasParameters)
            {
                SetParameters(seedCommand, payment);
            }
            else
            {
                AddParameters(seedCommand, payment);
                seedCommand.Prepare();
                hasParameters = true;
            }

            await seedCommand.ExecuteNonQueryAsync();
        }
        seedTransaction.Commit();
    }

    public async Task<IEnumerable<Payment>> GetAllAsync()
    {
        await using var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT Id, MemberId, Amount, PaymentDate, PaymentMethod, InvoiceNumber, Status, Notes FROM Payments ORDER BY PaymentDate DESC";
        await using var reader = await command.ExecuteReaderAsync();
        var results = new List<Payment>();
        while (await reader.ReadAsync()) results.Add(ReadPayment(reader));
        return results;
    }

    public async Task RemoveGeneratedTestPaymentsAfterAsync(int maxMemberId)
    {
        await using var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM Payments WHERE MemberId > $maxId AND MemberId <= 2000 AND Id = 100000 + MemberId AND InvoiceNumber LIKE 'GEN-%'";
        command.Parameters.AddWithValue("$maxId", maxMemberId);
        await command.ExecuteNonQueryAsync();
    }

    public async Task<Payment?> GetByIdAsync(int id)
    {
        var results = await QueryAsync("SELECT Id, MemberId, Amount, PaymentDate, PaymentMethod, InvoiceNumber, Status, Notes FROM Payments WHERE Id = $id", command => command.Parameters.AddWithValue("$id", id));
        return results.SingleOrDefault();
    }

    public async Task AddAsync(Payment payment)
    {
        await using var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "INSERT INTO Payments (MemberId, Amount, PaymentDate, PaymentMethod, InvoiceNumber, Status, Notes) VALUES ($memberId, $amount, $date, $method, $invoice, $status, $notes); SELECT last_insert_rowid();";
        AddParameters(command, payment, false);
        payment.Id = Convert.ToInt32(await command.ExecuteScalarAsync());
    }

    public async Task UpdateAsync(Payment payment)
    {
        await using var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "UPDATE Payments SET MemberId = $memberId, Amount = $amount, PaymentDate = $date, PaymentMethod = $method, InvoiceNumber = $invoice, Status = $status, Notes = $notes WHERE Id = $id";
        AddParameters(command, payment);
        if (await command.ExecuteNonQueryAsync() == 0) throw new KeyNotFoundException($"Payment {payment.Id} was not found.");
    }

    public async Task DeleteAsync(int id)
    {
        await using var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM Payments WHERE Id = $id";
        command.Parameters.AddWithValue("$id", id);
        await command.ExecuteNonQueryAsync();
    }

    private async Task<List<Payment>> QueryAsync(string sql, Action<SqliteCommand> configure)
    {
        await using var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        configure(command);
        await using var reader = await command.ExecuteReaderAsync();
        var results = new List<Payment>();
        while (await reader.ReadAsync()) results.Add(ReadPayment(reader));
        return results;
    }

    private static void AddParameters(SqliteCommand command, Payment payment, bool includeId = true)
    {
        if (includeId) command.Parameters.AddWithValue("$id", payment.Id);
        command.Parameters.AddWithValue("$memberId", payment.MemberId);
        command.Parameters.AddWithValue("$amount", payment.Amount);
        command.Parameters.AddWithValue("$date", payment.PaymentDate.ToString("O"));
        command.Parameters.AddWithValue("$method", payment.PaymentMethod);
        command.Parameters.AddWithValue("$invoice", payment.InvoiceNumber);
        command.Parameters.AddWithValue("$status", payment.Status);
        command.Parameters.AddWithValue("$notes", payment.Notes);
    }

    private static void SetParameters(SqliteCommand command, Payment payment)
    {
        command.Parameters["$id"].Value = payment.Id;
        command.Parameters["$memberId"].Value = payment.MemberId;
        command.Parameters["$amount"].Value = payment.Amount;
        command.Parameters["$date"].Value = payment.PaymentDate.ToString("O");
        command.Parameters["$method"].Value = payment.PaymentMethod;
        command.Parameters["$invoice"].Value = payment.InvoiceNumber;
        command.Parameters["$status"].Value = payment.Status;
        command.Parameters["$notes"].Value = payment.Notes;
    }

    private static Payment ReadPayment(SqliteDataReader reader) => new()
    {
        Id = reader.GetInt32(0), MemberId = reader.GetInt32(1), Amount = reader.GetDecimal(2),
        PaymentDate = DateTime.Parse(reader.GetString(3)), PaymentMethod = reader.GetString(4),
        InvoiceNumber = reader.GetString(5), Status = reader.GetString(6), Notes = reader.GetString(7)
    };
}
