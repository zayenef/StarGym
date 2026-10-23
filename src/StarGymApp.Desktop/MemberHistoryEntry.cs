using System.ComponentModel;
using System.Runtime.CompilerServices;
using StarGymApp.Core.Models;

namespace StarGymApp.Desktop;

public sealed class MemberHistoryEntry : INotifyPropertyChanged
{
    private string recencyLabel = string.Empty;

    public MemberHistoryEntry(Member member, Attendance attendance)
    {
        Member = member;
        Attendance = attendance;
        UpdateRecencyLabel();
    }

    public Member Member { get; }
    public Attendance Attendance { get; }
    public DateTime CheckedInAt => Attendance.CheckInDate.Date + Attendance.CheckInTime;

    public string RecencyLabel
    {
        get => recencyLabel;
        private set
        {
            if (recencyLabel == value) return;
            recencyLabel = value;
            OnPropertyChanged();
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public void UpdateRecencyLabel()
    {
        var elapsed = DateTime.Now - CheckedInAt;
        RecencyLabel = elapsed < TimeSpan.Zero || elapsed < TimeSpan.FromMinutes(1)
            ? "Just now"
            : elapsed < TimeSpan.FromHours(1)
                ? $"{Math.Max(1, (int)elapsed.TotalMinutes)} min ago"
                : elapsed < TimeSpan.FromDays(1)
                    ? $"{(int)elapsed.TotalHours} hr ago"
                    : elapsed < TimeSpan.FromDays(2)
                        ? "Yesterday"
                        : CheckedInAt.ToString("MMM d, yyyy");
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
