using StarGymApp.Core.Services;
using Xunit;

namespace StarGymApp.Core.Tests;

public class BackupServiceTests
{
    [Fact]
    public void CreateBackup_CopiesDatabaseToBackupDirectory()
    {
        var root = Path.Combine(Path.GetTempPath(), $"stargym-backup-{Guid.NewGuid():N}");
        var databasePath = Path.Combine(root, "stargym.db");
        var backupDirectory = Path.Combine(root, "backups");
        Directory.CreateDirectory(root);
        File.WriteAllText(databasePath, "database-content");

        try
        {
            var backupPath = new BackupService().CreateBackup(databasePath, backupDirectory);

            Assert.True(File.Exists(backupPath));
            Assert.Equal("database-content", File.ReadAllText(backupPath));
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
    }
}
