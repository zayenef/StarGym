using Microsoft.Data.Sqlite;
using StarGymApp.Core.Models;

namespace StarGymApp.Core.Repositories;

public sealed class SqliteMemberRepository : IMemberRepository
{
    private readonly string connectionString;

    public SqliteMemberRepository(string databasePath)
    {
        connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = databasePath,
            Pooling = false
        }.ToString();
    }

    public async Task InitializeAsync(IEnumerable<Member>? seedMembers = null)
    {
        await using var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync();
        await using (var command = connection.CreateCommand())
        {
            command.CommandText = """
                CREATE TABLE IF NOT EXISTS Members (
                    Id INTEGER PRIMARY KEY,
                    FullName TEXT NOT NULL,
                    Phone TEXT NOT NULL,
                    Email TEXT NOT NULL,
                    Gender TEXT NOT NULL,
                    DateOfBirth TEXT NOT NULL,
                    JoinDate TEXT NOT NULL,
                    MembershipPlanId INTEGER NOT NULL,
                    Status TEXT NOT NULL,
                    TrainerId INTEGER NULL,
                    Notes TEXT NOT NULL,
                    BadgeId TEXT NOT NULL DEFAULT '',
                    PhotoPath TEXT NOT NULL DEFAULT '',
                    MembershipDurationMonths INTEGER NOT NULL DEFAULT 0,
                    MembershipPrice REAL NOT NULL DEFAULT 0
                );
                CREATE TABLE IF NOT EXISTS DeletedMembers (
                    MemberId INTEGER PRIMARY KEY
                );
                """;
            await command.ExecuteNonQueryAsync();
            try
            {
                await using var migration = connection.CreateCommand();
                migration.CommandText = "ALTER TABLE Members ADD COLUMN BadgeId TEXT NOT NULL DEFAULT ''";
                await migration.ExecuteNonQueryAsync();
            }
            catch (SqliteException)
            {
                // The column already exists on databases created by the current schema.
            }
            try
            {
                await using var photoMigration = connection.CreateCommand();
                photoMigration.CommandText = "ALTER TABLE Members ADD COLUMN PhotoPath TEXT NOT NULL DEFAULT ''";
                await photoMigration.ExecuteNonQueryAsync();
            }
            catch (SqliteException)
            {
            }
            try
            {
                await using var durationMigration = connection.CreateCommand();
                durationMigration.CommandText = "ALTER TABLE Members ADD COLUMN MembershipDurationMonths INTEGER NOT NULL DEFAULT 0";
                await durationMigration.ExecuteNonQueryAsync();
            }
            catch (SqliteException)
            {
            }
            try
            {
                await using var priceMigration = connection.CreateCommand();
                priceMigration.CommandText = "ALTER TABLE Members ADD COLUMN MembershipPrice REAL NOT NULL DEFAULT 0";
                await priceMigration.ExecuteNonQueryAsync();
            }
            catch (SqliteException)
            {
            }
            await using (var badgeUpdate = connection.CreateCommand())
            {
                badgeUpdate.CommandText = "UPDATE Members SET BadgeId = printf('%08d', 10000000 + Id), PhotoPath = 'Images/Members/' || printf('%08d', 10000000 + Id) || '.jpg' WHERE BadgeId = '' OR BadgeId LIKE 'STAR-%' OR PhotoPath = '' OR PhotoPath LIKE '%STAR-%'";
                await badgeUpdate.ExecuteNonQueryAsync();
            }
        }

        if (seedMembers is null)
        {
            return;
        }

        using var seedTransaction = connection.BeginTransaction();
        await using var seedCommand = connection.CreateCommand();
        seedCommand.Transaction = seedTransaction;
        seedCommand.CommandText = """
            INSERT OR IGNORE INTO Members
                (Id, FullName, Phone, Email, Gender, DateOfBirth, JoinDate, MembershipPlanId, Status, TrainerId, Notes, BadgeId, PhotoPath, MembershipDurationMonths, MembershipPrice)
            SELECT $id, $fullName, $phone, $email, $gender, $dateOfBirth, $joinDate, $planId, $status, $trainerId, $notes, $badgeId, $photoPath, $durationMonths, $membershipPrice
            WHERE NOT EXISTS (SELECT 1 FROM DeletedMembers WHERE MemberId = $id);
            """;
        var hasParameters = false;
        foreach (var member in seedMembers)
        {
            if (hasParameters)
            {
                SetParameters(seedCommand, member);
            }
            else
            {
                AddParameters(seedCommand, member);
                seedCommand.Prepare();
                hasParameters = true;
            }

            await seedCommand.ExecuteNonQueryAsync();
        }
        seedTransaction.Commit();
    }

    public async Task<IEnumerable<Member>> GetAllAsync()
    {
        await using var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT Id, FullName, Phone, Email, Gender, DateOfBirth, JoinDate, MembershipPlanId, Status, TrainerId, Notes, BadgeId, PhotoPath, MembershipDurationMonths, MembershipPrice FROM Members ORDER BY Id DESC";
        await using var reader = await command.ExecuteReaderAsync();
        var results = new List<Member>();
        while (await reader.ReadAsync())
        {
            results.Add(ReadMember(reader));
        }

        return results;
    }

    public async Task RemoveGeneratedTestMembersAfterAsync(int maxMemberId)
    {
        await using var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM Members WHERE Id > $maxId AND Email LIKE 'member%@stargym.test'";
        command.Parameters.AddWithValue("$maxId", maxMemberId);
        await command.ExecuteNonQueryAsync();
    }

    public async Task<Member?> GetByIdAsync(int id)
    {
        var members = await QueryAsync("SELECT Id, FullName, Phone, Email, Gender, DateOfBirth, JoinDate, MembershipPlanId, Status, TrainerId, Notes, BadgeId, PhotoPath, MembershipDurationMonths, MembershipPrice FROM Members WHERE Id = $id", (command) => command.Parameters.AddWithValue("$id", id));
        return members.SingleOrDefault();
    }

    public async Task AddAsync(Member member)
    {
        await using var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO Members
                (FullName, Phone, Email, Gender, DateOfBirth, JoinDate, MembershipPlanId, Status, TrainerId, Notes, BadgeId, PhotoPath, MembershipDurationMonths, MembershipPrice)
            VALUES ($fullName, $phone, $email, $gender, $dateOfBirth, $joinDate, $planId, $status, $trainerId, $notes, $badgeId, $photoPath, $durationMonths, $membershipPrice);
            SELECT last_insert_rowid();
            """;
        AddParameters(command, member, includeId: false);
        member.Id = Convert.ToInt32(await command.ExecuteScalarAsync());
    }

    public async Task UpdateAsync(Member member)
    {
        await using var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            UPDATE Members SET FullName = $fullName, Phone = $phone, Email = $email, Gender = $gender,
                DateOfBirth = $dateOfBirth, JoinDate = $joinDate, MembershipPlanId = $planId,
                Status = $status, TrainerId = $trainerId, Notes = $notes, BadgeId = $badgeId, PhotoPath = $photoPath,
                MembershipDurationMonths = $durationMonths, MembershipPrice = $membershipPrice
            WHERE Id = $id;
            """;
        AddParameters(command, member);
        if (await command.ExecuteNonQueryAsync() == 0)
        {
            throw new KeyNotFoundException($"Member {member.Id} was not found.");
        }
    }

    public async Task SaveMembershipAsync(Member member, Payment? payment)
    {
        await using var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync();
        using var transaction = connection.BeginTransaction();

        await using (var memberCommand = connection.CreateCommand())
        {
            memberCommand.Transaction = transaction;
            if (member.Id == 0)
            {
                memberCommand.CommandText = """
                    INSERT INTO Members
                        (FullName, Phone, Email, Gender, DateOfBirth, JoinDate, MembershipPlanId, Status, TrainerId, Notes, BadgeId, PhotoPath, MembershipDurationMonths, MembershipPrice)
                    VALUES ($fullName, $phone, $email, $gender, $dateOfBirth, $joinDate, $planId, $status, $trainerId, $notes, $badgeId, $photoPath, $durationMonths, $membershipPrice);
                    SELECT last_insert_rowid();
                    """;
                AddParameters(memberCommand, member, includeId: false);
                member.Id = Convert.ToInt32(await memberCommand.ExecuteScalarAsync());

                await using (var clearDeletionCommand = connection.CreateCommand())
                {
                    clearDeletionCommand.Transaction = transaction;
                    clearDeletionCommand.CommandText = "DELETE FROM DeletedMembers WHERE MemberId = $id";
                    clearDeletionCommand.Parameters.AddWithValue("$id", member.Id);
                    await clearDeletionCommand.ExecuteNonQueryAsync();
                }

                if (string.IsNullOrWhiteSpace(member.BadgeId) || member.BadgeId.StartsWith("STAR-", StringComparison.OrdinalIgnoreCase))
                {
                    member.BadgeId = member.Id > 0 ? $"{10000000 + member.Id:00000000}" : string.Empty;
                    if (string.IsNullOrWhiteSpace(member.PhotoPath) || member.PhotoPath.Contains("STAR-", StringComparison.OrdinalIgnoreCase))
                    {
                        member.PhotoPath = member.BadgeId.Length > 0 ? $"Images/Members/{member.BadgeId}.jpg" : string.Empty;
                    }

                    await using var profileCommand = connection.CreateCommand();
                    profileCommand.Transaction = transaction;
                    profileCommand.CommandText = "UPDATE Members SET BadgeId = $badgeId, PhotoPath = $photoPath WHERE Id = $id";
                    profileCommand.Parameters.AddWithValue("$id", member.Id);
                    profileCommand.Parameters.AddWithValue("$badgeId", member.BadgeId);
                    profileCommand.Parameters.AddWithValue("$photoPath", member.PhotoPath);
                    await profileCommand.ExecuteNonQueryAsync();
                }
            }
            else
            {
                memberCommand.CommandText = """
                    UPDATE Members SET FullName = $fullName, Phone = $phone, Email = $email, Gender = $gender,
                        DateOfBirth = $dateOfBirth, JoinDate = $joinDate, MembershipPlanId = $planId,
                        Status = $status, TrainerId = $trainerId, Notes = $notes, BadgeId = $badgeId, PhotoPath = $photoPath,
                        MembershipDurationMonths = $durationMonths, MembershipPrice = $membershipPrice
                    WHERE Id = $id;
                    """;
                AddParameters(memberCommand, member);
                if (await memberCommand.ExecuteNonQueryAsync() == 0)
                {
                    throw new KeyNotFoundException($"Member {member.Id} was not found.");
                }
            }
        }

        if (payment is not null)
        {
            payment.MemberId = member.Id;
            await using var paymentCommand = connection.CreateCommand();
            paymentCommand.Transaction = transaction;
            paymentCommand.CommandText = """
                INSERT INTO Payments (MemberId, Amount, PaymentDate, PaymentMethod, InvoiceNumber, Status, Notes)
                VALUES ($memberId, $amount, $date, $method, $invoice, $status, $notes);
                SELECT last_insert_rowid();
                """;
            paymentCommand.Parameters.AddWithValue("$memberId", payment.MemberId);
            paymentCommand.Parameters.AddWithValue("$amount", payment.Amount);
            paymentCommand.Parameters.AddWithValue("$date", payment.PaymentDate.ToString("O"));
            paymentCommand.Parameters.AddWithValue("$method", payment.PaymentMethod);
            paymentCommand.Parameters.AddWithValue("$invoice", payment.InvoiceNumber);
            paymentCommand.Parameters.AddWithValue("$status", payment.Status);
            paymentCommand.Parameters.AddWithValue("$notes", payment.Notes);
            payment.Id = Convert.ToInt32(await paymentCommand.ExecuteScalarAsync());
        }

        transaction.Commit();
    }

    public async Task DeleteWithHistoryAsync(int id)
    {
        await using var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync();
        using var transaction = connection.BeginTransaction();

        await using (var tombstoneCommand = connection.CreateCommand())
        {
            tombstoneCommand.Transaction = transaction;
            tombstoneCommand.CommandText = "INSERT OR IGNORE INTO DeletedMembers (MemberId) SELECT Id FROM Members WHERE Id = $memberId";
            tombstoneCommand.Parameters.AddWithValue("$memberId", id);
            await tombstoneCommand.ExecuteNonQueryAsync();
        }

        foreach (var table in new[] { "Payments", "Attendance" })
        {
            await using var historyCommand = connection.CreateCommand();
            historyCommand.Transaction = transaction;
            historyCommand.CommandText = $"DELETE FROM {table} WHERE MemberId = $memberId";
            historyCommand.Parameters.AddWithValue("$memberId", id);
            await historyCommand.ExecuteNonQueryAsync();
        }

        await using var memberCommand = connection.CreateCommand();
        memberCommand.Transaction = transaction;
        memberCommand.CommandText = "DELETE FROM Members WHERE Id = $id";
        memberCommand.Parameters.AddWithValue("$id", id);
        if (await memberCommand.ExecuteNonQueryAsync() == 0)
        {
            throw new KeyNotFoundException($"Member {id} was not found.");
        }

        transaction.Commit();
    }

    public async Task DeleteAsync(int id)
    {
        await using var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM Members WHERE Id = $id";
        command.Parameters.AddWithValue("$id", id);
        await command.ExecuteNonQueryAsync();
    }

    private async Task<List<Member>> QueryAsync(string sql, Action<SqliteCommand> configure)
    {
        await using var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        configure(command);
        await using var reader = await command.ExecuteReaderAsync();
        var results = new List<Member>();
        while (await reader.ReadAsync())
        {
            results.Add(ReadMember(reader));
        }

        return results;
    }

    private static void AddParameters(SqliteCommand command, Member member, bool includeId = true)
    {
        if (includeId) command.Parameters.AddWithValue("$id", member.Id);
        command.Parameters.AddWithValue("$fullName", member.FullName);
        command.Parameters.AddWithValue("$phone", member.Phone);
        command.Parameters.AddWithValue("$email", member.Email);
        command.Parameters.AddWithValue("$gender", member.Gender);
        command.Parameters.AddWithValue("$dateOfBirth", member.DateOfBirth.ToString("O"));
        command.Parameters.AddWithValue("$joinDate", member.JoinDate.ToString("O"));
        command.Parameters.AddWithValue("$planId", member.MembershipPlanId);
        command.Parameters.AddWithValue("$status", member.Status);
        command.Parameters.AddWithValue("$trainerId", member.TrainerId.HasValue ? member.TrainerId.Value : DBNull.Value);
        command.Parameters.AddWithValue("$notes", member.Notes);
        command.Parameters.AddWithValue("$badgeId", NormalizeBadgeId(member));
        command.Parameters.AddWithValue("$photoPath", GetStoredPhotoPath(member));
        command.Parameters.AddWithValue("$durationMonths", member.MembershipDurationMonths);
        command.Parameters.AddWithValue("$membershipPrice", member.MembershipPrice);
    }

    private static void SetParameters(SqliteCommand command, Member member)
    {
        command.Parameters["$id"].Value = member.Id;
        command.Parameters["$fullName"].Value = member.FullName;
        command.Parameters["$phone"].Value = member.Phone;
        command.Parameters["$email"].Value = member.Email;
        command.Parameters["$gender"].Value = member.Gender;
        command.Parameters["$dateOfBirth"].Value = member.DateOfBirth.ToString("O");
        command.Parameters["$joinDate"].Value = member.JoinDate.ToString("O");
        command.Parameters["$planId"].Value = member.MembershipPlanId;
        command.Parameters["$status"].Value = member.Status;
        command.Parameters["$trainerId"].Value = member.TrainerId.HasValue ? member.TrainerId.Value : DBNull.Value;
        command.Parameters["$notes"].Value = member.Notes;
        command.Parameters["$badgeId"].Value = NormalizeBadgeId(member);
        command.Parameters["$photoPath"].Value = GetStoredPhotoPath(member);
        command.Parameters["$durationMonths"].Value = member.MembershipDurationMonths;
        command.Parameters["$membershipPrice"].Value = member.MembershipPrice;
    }

    private static string NormalizeBadgeId(Member member)
    {
        var badgeId = member.BadgeId?.Trim();
        if (!string.IsNullOrWhiteSpace(badgeId) && !badgeId.StartsWith("STAR-", StringComparison.OrdinalIgnoreCase))
        {
            return badgeId;
        }

        return member.Id > 0 ? $"{10000000 + member.Id:00000000}" : string.Empty;
    }

    private static string GetStoredPhotoPath(Member member)
    {
        if (!string.IsNullOrWhiteSpace(member.PhotoPath) && !member.PhotoPath.Contains("STAR-", StringComparison.OrdinalIgnoreCase))
        {
            return member.PhotoPath;
        }

        var badgeId = NormalizeBadgeId(member);
        return string.IsNullOrWhiteSpace(badgeId) ? string.Empty : $"Images/Members/{badgeId}.jpg";
    }

    private static Member ReadMember(SqliteDataReader reader) => new()
    {
        Id = reader.GetInt32(0),
        FullName = reader.GetString(1),
        Phone = reader.GetString(2),
        Email = reader.GetString(3),
        Gender = reader.GetString(4),
        DateOfBirth = DateTime.Parse(reader.GetString(5)),
        JoinDate = DateTime.Parse(reader.GetString(6)),
        MembershipPlanId = reader.GetInt32(7),
        Status = reader.GetString(8),
        TrainerId = reader.IsDBNull(9) ? null : reader.GetInt32(9),
        Notes = reader.GetString(10),
        BadgeId = reader.GetString(11),
        PhotoPath = reader.GetString(12),
        MembershipDurationMonths = reader.GetInt32(13),
        MembershipPrice = reader.GetDecimal(14)
    };
}
