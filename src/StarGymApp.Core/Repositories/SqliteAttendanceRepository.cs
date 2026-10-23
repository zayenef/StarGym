using Microsoft.Data.Sqlite;
using StarGymApp.Core.Models;

namespace StarGymApp.Core.Repositories;

public sealed class SqliteAttendanceRepository : IRepository<Attendance>
{
    private readonly string connectionString;

    public SqliteAttendanceRepository(string databasePath)
    {
        connectionString = new SqliteConnectionStringBuilder { DataSource = databasePath, Pooling = false }.ToString();
    }

    public async Task InitializeAsync(IEnumerable<Attendance>? seedAttendance = null)
    {
        await using var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE IF NOT EXISTS Attendance (
                Id INTEGER PRIMARY KEY,
                MemberId INTEGER NOT NULL,
                CheckInDate TEXT NOT NULL,
                CheckInTime TEXT NOT NULL,
                CheckOutTime TEXT NULL,
                Status TEXT NOT NULL
            );
            """;
        await command.ExecuteNonQueryAsync();

        if (seedAttendance is null) return;
        using var seedTransaction = connection.BeginTransaction();
        await using var seedCommand = connection.CreateCommand();
        seedCommand.Transaction = seedTransaction;
        seedCommand.CommandText = "INSERT OR IGNORE INTO Attendance (Id, MemberId, CheckInDate, CheckInTime, CheckOutTime, Status) VALUES ($id, $memberId, $date, $checkIn, $checkOut, $status)";
        var hasParameters = false;
        foreach (var attendance in seedAttendance)
        {
            if (hasParameters)
            {
                SetParameters(seedCommand, attendance);
            }
            else
            {
                AddParameters(seedCommand, attendance);
                seedCommand.Prepare();
                hasParameters = true;
            }

            await seedCommand.ExecuteNonQueryAsync();
        }
        seedTransaction.Commit();
    }

    public async Task<IEnumerable<Attendance>> GetAllAsync()
    {
        await using var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT Id, MemberId, CheckInDate, CheckInTime, CheckOutTime, Status FROM Attendance ORDER BY CheckInDate DESC, CheckInTime DESC";
        await using var reader = await command.ExecuteReaderAsync();
        var results = new List<Attendance>();
        while (await reader.ReadAsync()) results.Add(ReadAttendance(reader));
        return results;
    }

    public async Task RemoveGeneratedTestAttendanceAfterAsync(int maxMemberId)
    {
        await using var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM Attendance WHERE MemberId > $maxId AND MemberId <= 2000 AND Id = 100000 + MemberId";
        command.Parameters.AddWithValue("$maxId", maxMemberId);
        await command.ExecuteNonQueryAsync();
    }

    public async Task<Attendance?> GetByIdAsync(int id)
    {
        var results = await QueryAsync("SELECT Id, MemberId, CheckInDate, CheckInTime, CheckOutTime, Status FROM Attendance WHERE Id = $id", command => command.Parameters.AddWithValue("$id", id));
        return results.SingleOrDefault();
    }

    public async Task AddAsync(Attendance attendance)
    {
        await using var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "INSERT INTO Attendance (MemberId, CheckInDate, CheckInTime, CheckOutTime, Status) VALUES ($memberId, $date, $checkIn, $checkOut, $status); SELECT last_insert_rowid();";
        AddParameters(command, attendance, false);
        attendance.Id = Convert.ToInt32(await command.ExecuteScalarAsync());
    }

    public async Task UpdateAsync(Attendance attendance)
    {
        await using var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "UPDATE Attendance SET MemberId = $memberId, CheckInDate = $date, CheckInTime = $checkIn, CheckOutTime = $checkOut, Status = $status WHERE Id = $id";
        AddParameters(command, attendance);
        if (await command.ExecuteNonQueryAsync() == 0) throw new KeyNotFoundException($"Attendance {attendance.Id} was not found.");
    }

    public async Task DeleteAsync(int id)
    {
        await using var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM Attendance WHERE Id = $id";
        command.Parameters.AddWithValue("$id", id);
        await command.ExecuteNonQueryAsync();
    }

    private async Task<List<Attendance>> QueryAsync(string sql, Action<SqliteCommand> configure)
    {
        await using var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        configure(command);
        await using var reader = await command.ExecuteReaderAsync();
        var results = new List<Attendance>();
        while (await reader.ReadAsync()) results.Add(ReadAttendance(reader));
        return results;
    }

    private static void AddParameters(SqliteCommand command, Attendance attendance, bool includeId = true)
    {
        if (includeId) command.Parameters.AddWithValue("$id", attendance.Id);
        command.Parameters.AddWithValue("$memberId", attendance.MemberId);
        command.Parameters.AddWithValue("$date", attendance.CheckInDate.ToString("O"));
        command.Parameters.AddWithValue("$checkIn", attendance.CheckInTime.ToString());
        command.Parameters.AddWithValue("$checkOut", attendance.CheckOutTime?.ToString() ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("$status", attendance.Status);
    }

    private static void SetParameters(SqliteCommand command, Attendance attendance)
    {
        command.Parameters["$id"].Value = attendance.Id;
        command.Parameters["$memberId"].Value = attendance.MemberId;
        command.Parameters["$date"].Value = attendance.CheckInDate.ToString("O");
        command.Parameters["$checkIn"].Value = attendance.CheckInTime.ToString();
        command.Parameters["$checkOut"].Value = attendance.CheckOutTime?.ToString() ?? (object)DBNull.Value;
        command.Parameters["$status"].Value = attendance.Status;
    }

    private static Attendance ReadAttendance(SqliteDataReader reader) => new()
    {
        Id = reader.GetInt32(0), MemberId = reader.GetInt32(1), CheckInDate = DateTime.Parse(reader.GetString(2)),
        CheckInTime = TimeSpan.Parse(reader.GetString(3)), CheckOutTime = reader.IsDBNull(4) ? null : TimeSpan.Parse(reader.GetString(4)),
        Status = reader.GetString(5)
    };
}
