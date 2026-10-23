using Microsoft.Data.Sqlite;

namespace StarGymApp.Core.Services;

public sealed record ProductionResetResult(bool WasApplied, int MembersRemoved, int PaymentsRemoved, int AttendanceRemoved);

public sealed class SqliteProductionResetService
{
    private const string ResetKey = "production-data-reset-v1";

    public async Task<ProductionResetResult> ClearExistingDataOnceAsync(string databasePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databasePath);
        var connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = databasePath,
            Pooling = false
        }.ToString();

        await using var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync();

        await using (var metadataCommand = connection.CreateCommand())
        {
            metadataCommand.Transaction = (SqliteTransaction)transaction;
            metadataCommand.CommandText = """
                CREATE TABLE IF NOT EXISTS AppMetadata (
                    Key TEXT PRIMARY KEY,
                    Value TEXT NOT NULL
                );
                SELECT Value FROM AppMetadata WHERE Key = $key;
                """;
            metadataCommand.Parameters.AddWithValue("$key", ResetKey);
            var existingReset = await metadataCommand.ExecuteScalarAsync();
            if (existingReset is not null)
            {
                await transaction.CommitAsync();
                return new ProductionResetResult(false, 0, 0, 0);
            }
        }

        var attendanceRemoved = await DeleteRowsAsync(connection, (SqliteTransaction)transaction, "Attendance");
        var paymentsRemoved = await DeleteRowsAsync(connection, (SqliteTransaction)transaction, "Payments");
        var membersRemoved = await DeleteRowsAsync(connection, (SqliteTransaction)transaction, "Members");
        await DeleteRowsAsync(connection, (SqliteTransaction)transaction, "DeletedMembers");

        await using (var markCommand = connection.CreateCommand())
        {
            markCommand.Transaction = (SqliteTransaction)transaction;
            markCommand.CommandText = "INSERT INTO AppMetadata (Key, Value) VALUES ($key, $value)";
            markCommand.Parameters.AddWithValue("$key", ResetKey);
            markCommand.Parameters.AddWithValue("$value", DateTimeOffset.UtcNow.ToString("O"));
            await markCommand.ExecuteNonQueryAsync();
        }

        await transaction.CommitAsync();
        return new ProductionResetResult(true, membersRemoved, paymentsRemoved, attendanceRemoved);
    }

    private static async Task<int> DeleteRowsAsync(SqliteConnection connection, SqliteTransaction transaction, string tableName)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = $"DELETE FROM {tableName}";
        return await command.ExecuteNonQueryAsync();
    }
}
