namespace StarGymApp.Core.Services;

public sealed class BackupService
{
    public string CreateBackup(string databasePath, string backupDirectory)
    {
        if (!File.Exists(databasePath))
        {
            throw new FileNotFoundException("The StarGym database was not found.", databasePath);
        }

        Directory.CreateDirectory(backupDirectory);
        var backupPath = Path.Combine(backupDirectory, $"stargym-{DateTime.Now:yyyyMMdd-HHmmss}.db");
        File.Copy(databasePath, backupPath, overwrite: false);
        return backupPath;
    }
}
