using System.Windows;
using System.IO;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using StarGymApp.Core.Helpers;
using StarGymApp.Core.Models;
using StarGymApp.Core.Repositories;

namespace StarGymApp.Desktop;

public partial class BadgeScannerWindow : Window
{
    private readonly IReadOnlyList<Member> members;
    private readonly IReadOnlyList<MembershipPlan> plans;
    private readonly IReadOnlyList<Payment> payments;
    private readonly IReadOnlyList<Attendance> attendance;
    private readonly SqliteAttendanceRepository attendanceRepository;
    private Member? selectedMember;

    public BadgeScannerWindow(IEnumerable<Member> members, IEnumerable<MembershipPlan> plans, IEnumerable<Payment> payments, IEnumerable<Attendance> attendance, SqliteAttendanceRepository attendanceRepository)
    {
        InitializeComponent();
        this.members = members.ToList();
        this.plans = plans.ToList();
        this.payments = payments.ToList();
        this.attendance = attendance.ToList();
        this.attendanceRepository = attendanceRepository;
        Loaded += (_, _) => BadgeInput.Focus();
    }

    private void BadgeInput_OnKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter) FindMember();
    }

    private void FindMemberButton_OnClick(object sender, RoutedEventArgs e) => FindMember();

    private void FindMember()
    {
        var code = BadgeInput.Text.Trim();
        selectedMember = members.FirstOrDefault(member => string.Equals(member.BadgeId, code, StringComparison.OrdinalIgnoreCase));
        if (selectedMember is null)
        {
            ScanStatusText.Text = "Badge not recognized. Try 10000001.";
            MemberNameText.Text = "No member scanned";
            return;
        }

        var plan = plans.FirstOrDefault(item => item.Id == selectedMember.MembershipPlanId);
        var memberPayments = payments.Where(payment => payment.MemberId == selectedMember.Id).OrderByDescending(payment => payment.PaymentDate).ToList();
        var memberAttendance = attendance.Count(record => record.MemberId == selectedMember.Id);
        DateTime? expiry = selectedMember.GetMembershipExpiry(plan);
        MemberNameText.Text = selectedMember.FullName;
        MemberPhoto.Source = LoadPhoto(selectedMember);
        PhoneText.Text = selectedMember.Phone;
        AgeText.Text = selectedMember.Age.ToString();
        BadgeText.Text = selectedMember.BadgeId;
        StartDateText.Text = selectedMember.JoinDate.ToString("yyyy-MM-dd");
        MembershipStatusText.Text = selectedMember.Status;
        ExpiryText.Text = expiry?.ToString("yyyy-MM-dd") ?? "Unknown";
        TotalPaidText.Text = memberPayments.Where(payment => payment.Status == "Paid").Sum(payment => payment.Amount).ToString("N0");
        VisitsText.Text = memberAttendance.ToString();
        LastPaymentText.Text = memberPayments.Count == 0 ? "Last payment: none" : $"Last payment: {memberPayments[0].PaymentDate:yyyy-MM-dd} - {memberPayments[0].Amount:N0}";
        ScanStatusText.Text = "Member found. Ready for entry.";
        CheckInButton.IsEnabled = selectedMember.Status == "Active" && (expiry is null || DateTime.Today <= expiry.Value.Date);
    }

    private static ImageSource LoadPhoto(Member member)
    {
        var photoPath = Path.IsPathRooted(member.PhotoPath) ? member.PhotoPath : Path.Combine(AppContext.BaseDirectory, member.PhotoPath);
        if (File.Exists(photoPath))
        {
            return new BitmapImage(new Uri(photoPath, UriKind.Absolute));
        }

        var initials = string.Concat(member.FullName.Split(' ', StringSplitOptions.RemoveEmptyEntries).Take(2).Select(part => part[0])).ToUpperInvariant();
        var visual = new DrawingVisual();
        using (var drawing = visual.RenderOpen())
        {
            drawing.DrawRectangle(new SolidColorBrush(Color.FromRgb(23, 105, 224)), null, new Rect(0, 0, 120, 120));
            var text = new FormattedText(initials, System.Globalization.CultureInfo.InvariantCulture, FlowDirection.LeftToRight, new Typeface("Segoe UI"), 38, Brushes.White, 1.0);
            drawing.DrawText(text, new Point((120 - text.Width) / 2, (120 - text.Height) / 2));
        }

        var bitmap = new RenderTargetBitmap(120, 120, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(visual);
        return bitmap;
    }

    private async void CheckInButton_OnClick(object sender, RoutedEventArgs e
    )
    {
        if (selectedMember is null) return;
        var record = new Attendance { MemberId = selectedMember.Id, CheckInDate = DateTime.Today, CheckInTime = DateTime.Now.TimeOfDay, Status = "Present" };
        await attendanceRepository.AddAsync(record);
        ScanStatusText.Text = "Check-in recorded successfully.";
        VisitsText.Text = (int.Parse(VisitsText.Text) + 1).ToString();
        BadgeInput.SelectAll();
    }
}
