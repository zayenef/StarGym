namespace StarGymApp.Core.Helpers;

public static class DateHelper
{
    public static bool IsExpired(DateTime joinDate, int durationDays)
    {
        var expiryDate = joinDate.AddDays(durationDays);
        return DateTime.Today > expiryDate;
    }
}
