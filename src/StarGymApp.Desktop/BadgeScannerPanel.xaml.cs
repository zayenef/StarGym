using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.IO;
using StarGymApp.Core.Helpers;
using StarGymApp.Core.Models;
using StarGymApp.Core.Repositories;

namespace StarGymApp.Desktop;

public partial class BadgeScannerPanel : UserControl
{
    private IReadOnlyList<Member> members = Array.Empty<Member>();
    private IReadOnlyList<MembershipPlan> plans = Array.Empty<MembershipPlan>();
    private IReadOnlyList<Payment> payments = Array.Empty<Payment>();
    private IReadOnlyList<Attendance> attendance = Array.Empty<Attendance>();
    private SqliteAttendanceRepository? attendanceRepository;
    private Member? selectedMember;
    private bool checkInInProgress;

    public event Action<Attendance>? CheckInRecorded;

    public BadgeScannerPanel() => InitializeComponent();

    public void SetData(IEnumerable<Member> memberRecords, IEnumerable<MembershipPlan> planRecords, IEnumerable<Payment> paymentRecords, IEnumerable<Attendance> attendanceRecords, SqliteAttendanceRepository repository)
    {
        members = memberRecords.ToList(); plans = planRecords.ToList(); payments = paymentRecords.ToList(); attendance = attendanceRecords.ToList(); attendanceRepository = repository;
        BadgeInput.Focus();
    }

    private async void BadgeInput_OnKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter) return;
        e.Handled = true;
        FindMember();
        if (CheckInButton.IsEnabled)
        {
            await RecordCheckInAsync();
        }
    }
    private void FindMemberButton_OnClick(object sender, RoutedEventArgs e) => FindMember();

    private void FindMember()
    {
        selectedMember = members.FirstOrDefault(member => string.Equals(member.BadgeId, BadgeInput.Text.Trim(), StringComparison.OrdinalIgnoreCase));
        if (selectedMember is null) { ScanStatusText.Text = "Badge not recognized. Try 10000001."; MemberNameText.Text = "No member scanned"; return; }
        var plan = plans.FirstOrDefault(item => item.Id == selectedMember.MembershipPlanId);
        var memberPayments = payments.Where(payment => payment.MemberId == selectedMember.Id).OrderByDescending(payment => payment.PaymentDate).ToList();
        var memberAttendance = attendance.Count(record => record.MemberId == selectedMember.Id);
        DateTime? expiry = selectedMember.GetMembershipExpiry(plan);
        MemberNameText.Text = selectedMember.FullName; MemberPhoto.Source = LoadPhoto(selectedMember); PhoneText.Text = selectedMember.Phone; AgeText.Text = selectedMember.Age.ToString(); BadgeText.Text = selectedMember.BadgeId; StartDateText.Text = selectedMember.JoinDate.ToString("yyyy-MM-dd"); MembershipStatusText.Text = selectedMember.Status; ExpiryText.Text = expiry?.ToString("yyyy-MM-dd") ?? "Unknown"; TotalPaidText.Text = memberPayments.Where(payment => payment.Status == "Paid").Sum(payment => payment.Amount).ToString("N0"); VisitsText.Text = $"Visits: {memberAttendance}"; LastPaymentText.Text = memberPayments.Count == 0 ? "Last payment: none" : $"Last payment: {memberPayments[0].PaymentDate:yyyy-MM-dd} - {memberPayments[0].Amount:N0}"; ScanStatusText.Text = "Member found. Ready for entry."; CheckInButton.IsEnabled = selectedMember.Status == "Active" && (expiry is null || DateTime.Today <= expiry.Value.Date);
    }

    private async void CheckInButton_OnClick(object sender, RoutedEventArgs e) => await RecordCheckInAsync();

    private async Task RecordCheckInAsync()
    {
        if (checkInInProgress || selectedMember is null || attendanceRepository is null || !CheckInButton.IsEnabled) return;

        checkInInProgress = true;
        CheckInButton.IsEnabled = false;
        try
        {
            var record = new Attendance
            {
                MemberId = selectedMember.Id,
                CheckInDate = DateTime.Today,
                CheckInTime = DateTime.Now.TimeOfDay,
                Status = "Present"
            };
            await attendanceRepository.AddAsync(record);
            attendance = attendance.Append(record).ToList();
            CheckInRecorded?.Invoke(record);
            ScanStatusText.Text = "Check-in recorded successfully and added to member history.";
            var current = int.Parse(VisitsText.Text.Replace("Visits: ", ""));
            VisitsText.Text = $"Visits: {current + 1}";
            BadgeInput.SelectAll();
        }
        catch (Exception exception) when (exception is Microsoft.Data.Sqlite.SqliteException or IOException or UnauthorizedAccessException)
        {
            ScanStatusText.Text = $"Check-in could not be recorded: {exception.Message}";
        }
        finally
        {
            checkInInProgress = false;
            CheckInButton.IsEnabled = selectedMember is not null
                && selectedMember.Status == "Active"
                && (selectedMember.GetMembershipExpiry(plans.FirstOrDefault(plan => plan.Id == selectedMember.MembershipPlanId)) is not { } expiry
                    || DateTime.Today <= expiry.Date);
        }
    }

    private static ImageSource LoadPhoto(Member member)
        => MemberPhotoStorage.LoadImage(member);
}
