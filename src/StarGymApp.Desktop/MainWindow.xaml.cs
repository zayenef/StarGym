using System.Windows;
using System.IO;
using System.ComponentModel;
using System.Windows.Controls;
using System.Windows.Data;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.Data.Sqlite;
using StarGymApp.Core.Data;
using StarGymApp.Core.Models;
using StarGymApp.Core.Repositories;
using StarGymApp.Core.Services;

namespace StarGymApp.Desktop;

public partial class MainWindow : Window
{
    private RevenueReportsPanel? revenueReportsPanel;
    private readonly ObservableCollection<Member> members = new();
    private readonly ObservableCollection<Payment> payments = new();
    private readonly ObservableCollection<Attendance> attendance = new();
    private readonly ObservableCollection<MemberHistoryEntry> memberHistory = new();
    private readonly MemberExcelBackupService memberExcelBackupService = new();
    private readonly DispatcherTimer dashboardDateTimer = new() { Interval = TimeSpan.FromMinutes(1) };
    private DateTime lastDashboardDate = DateTime.Today;
    private IReadOnlyList<MembershipPlan> plans = Array.Empty<MembershipPlan>();
    private SqliteMemberRepository? memberRepository;
    private SqliteAttendanceRepository? attendanceRepository;
    private string? databasePath;

    public MainWindow()
    {
        InitializeComponent();
        BadgeScannerPanelControl.CheckInRecorded += OnBadgeCheckInRecorded;
        MemberHistoryList.ItemsSource = memberHistory;
        dashboardDateTimer.Tick += OnDashboardDateTimerTick;
        dashboardDateTimer.Start();
        Closed += (_, _) => dashboardDateTimer.Stop();
        AttendanceTrendCanvas.SizeChanged += (_, _) => RefreshAttendanceTrend();
        ReportsPage.Children.Clear();
        revenueReportsPanel = new RevenueReportsPanel();
        ReportsPage.Children.Add(revenueReportsPanel);
        EnableVirtualization(MembersList);
        Loaded += LoadDashboardAsync;
    }

    private static void EnableVirtualization(ListView list)
    {
        VirtualizingPanel.SetIsVirtualizing(list, true);
        VirtualizingPanel.SetVirtualizationMode(list, VirtualizationMode.Recycling);
        ScrollViewer.SetCanContentScroll(list, true);
    }

    private void DashboardNavButton_OnClick(object sender, RoutedEventArgs e) => ShowPage(DashboardPage, DashboardNavButton, "Dashboard", "A clear view of today's gym operations.");

    private void MembersNavButton_OnClick(object sender, RoutedEventArgs e) => ShowPage(MembersPage, MembersNavButton, "Members", "Search, manage, and grow your athlete community.");

    private void ReportsNavButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (payments.Count == 0)
        {
            MessageBox.Show("Reports will be available after payment data is loaded.", "Reports", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

            ShowPage(ReportsPage, ReportsNavButton, "Reports", "Turn gym activity into your next smart decision.");
    }

    private void BadgeScannerNavButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (attendanceRepository is null || BadgeScannerPanelControl is null)
        {
            MessageBox.Show("The entry desk is still loading.", "Badge check-in", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        BadgeScannerPanelControl.SetData(members, plans, payments, attendance, attendanceRepository);
        ShowPage(BadgeScannerPage, BadgeScannerNavButton, "Badge check-in", "Identify a member and confirm their profile before entry.");
    }

    private async void MemberHistoryNavButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (attendanceRepository is null)
        {
            MessageBox.Show("Member history is still loading.", "Member history", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        try
        {
            var latestAttendance = (await attendanceRepository.GetAllAsync()).ToList();
            attendance.Clear();
            foreach (var record in latestAttendance)
            {
                attendance.Add(record);
            }
            PopulateMemberHistory();
        }
        catch (Exception exception) when (exception is SqliteException or IOException or UnauthorizedAccessException)
        {
            MessageBox.Show($"Member history could not be refreshed: {exception.Message}", "Member history", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        ShowPage(MemberHistoryPage, MemberHistoryNavButton, "Member history", "Review badge entries with the latest visit at the top.");
        if (memberHistory.Count > 0)
        {
            MemberHistoryList.ScrollIntoView(memberHistory[0]);
        }
    }

    private void ShowPage(UIElement page, Button activeButton, string title, string subtitle)
    {
        DashboardPage.Visibility = Visibility.Collapsed;
        MembersPage.Visibility = Visibility.Collapsed;
        ReportsPage.Visibility = Visibility.Collapsed;
        BadgeScannerPage.Visibility = Visibility.Collapsed;
        MemberHistoryPage.Visibility = Visibility.Collapsed;
        page.Visibility = Visibility.Visible;

        foreach (var button in new[] { DashboardNavButton, MembersNavButton, ReportsNavButton, BadgeScannerNavButton, MemberHistoryNavButton })
        {
            button.Background = Brushes.Transparent;
            button.Foreground = (Brush)FindResource("NavTextBrush");
        }

        activeButton.Background = new SolidColorBrush(Color.FromRgb(23, 105, 224));
        activeButton.Foreground = Brushes.White;
        PageTitleText.Text = title;
        PageSubtitleText.Text = subtitle;
    }

    private async void LoadDashboardAsync(object sender, RoutedEventArgs e)
    {
        var dataPath = Path.Combine(AppContext.BaseDirectory, "Data", "expanded-starter-data.json");
        var databaseDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "StarGym");
        Directory.CreateDirectory(databaseDirectory);
        databasePath = Path.Combine(databaseDirectory, "stargym.db");
        var preparedData = await Task.Run(async () =>
        {
            var data = await GymDataLoader.LoadAsync(dataPath);
            var memberSqliteRepository = new SqliteMemberRepository(databasePath);
            var paymentSqliteRepository = new SqlitePaymentRepository(databasePath);
            var attendanceSqliteRepository = new SqliteAttendanceRepository(databasePath);
            await memberSqliteRepository.InitializeAsync();
            await paymentSqliteRepository.InitializeAsync();
            await attendanceSqliteRepository.InitializeAsync();
            var resetResult = await new SqliteProductionResetService().ClearExistingDataOnceAsync(databasePath);
            return (
                memberRepository: memberSqliteRepository,
                attendanceRepository: attendanceSqliteRepository,
                resetResult,
                members: (await memberSqliteRepository.GetAllAsync()).ToList(),
                plans: GymDataMapper.ToPlans(data).ToList(),
                payments: (await paymentSqliteRepository.GetAllAsync()).ToList(),
                attendance: (await attendanceSqliteRepository.GetAllAsync()).ToList());
        });

        if (preparedData.resetResult.WasApplied)
        {
            try
            {
                MemberPhotoStorage.ClearAllStoredPhotos();
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                MessageBox.Show(
                    $"The member records were cleared, but stored member photos could not all be removed: {exception.Message}",
                    "Photo cleanup incomplete",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }

            MessageBox.Show(
                $"Demo data has been cleared. Removed {preparedData.resetResult.MembersRemoved} members, " +
                $"{preparedData.resetResult.PaymentsRemoved} payments, and " +
                $"{preparedData.resetResult.AttendanceRemoved} attendance records. StarGym is ready for real clients.",
                "Production setup complete",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        memberRepository = preparedData.memberRepository;
        attendanceRepository = preparedData.attendanceRepository;
        plans = preparedData.plans;
        foreach (var member in preparedData.members)
        {
            UpdateMembershipExpiry(member);
            members.Add(member);
        }
        foreach (var payment in preparedData.payments) payments.Add(payment);
        foreach (var record in preparedData.attendance) attendance.Add(record);
        PopulateMemberHistory();
        payments.CollectionChanged += OnPaymentsCollectionChanged;
        revenueReportsPanel?.SetPayments(payments);
        RefreshDashboardSummary();
        MembersList.ItemsSource = members;
        var memberView = CollectionViewSource.GetDefaultView(members);
        memberView.Filter = FilterMember;
        memberView.SortDescriptions.Clear();
        memberView.SortDescriptions.Add(new SortDescription(nameof(Member.Id), ListSortDirection.Descending));
        MemberSearchBox.Clear();
        MemberStatusFilter.SelectedIndex = 0;
        RefreshMemberFilter();
        RefreshReports();
        UpdateMemberExcelBackup();
    }

    private void MemberSearchBox_OnTextChanged(object sender, TextChangedEventArgs e)
    {
        RefreshMemberFilter();
    }

    private void MemberStatusFilter_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        RefreshMemberFilter();
    }

    private void RefreshMemberFilter()
    {
        if (MembersList is not null && MembersList.ItemsSource is not null)
        {
            var view = CollectionViewSource.GetDefaultView(MembersList.ItemsSource);
            view.Refresh();
            MembersCountText.Text = $"Showing {view.Cast<object>().Count()} of {members.Count} members";
        }
    }

    private void UpdateMembershipExpiry(Member member)
    {
        var plan = plans.FirstOrDefault(candidate => candidate.Id == member.MembershipPlanId);
        member.MembershipExpiresOn = member.GetMembershipExpiry(plan);
    }

    private async void AddMemberButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (memberRepository is null) return;
        if (!plans.Any(plan => plan.IsActive))
        {
            MessageBox.Show("There are no active membership plans available.", "New client", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var editor = new MemberEditorWindow(plans: plans) { Owner = this };
        if (editor.ShowDialog() == true)
        {
            var payment = CreateMembershipPayment(editor);
            string? savedPhotoPath = null;
            try
            {
                if (editor.CapturedPhoto is { Length: > 0 } photo)
                {
                    savedPhotoPath = MemberPhotoStorage.Save(photo);
                    editor.Member.PhotoPath = savedPhotoPath;
                }

                await memberRepository.SaveMembershipAsync(editor.Member, payment);
            }
            catch (Exception exception) when (exception is SqliteException or KeyNotFoundException or IOException or UnauthorizedAccessException)
            {
                if (savedPhotoPath is not null) MemberPhotoStorage.Delete(savedPhotoPath);
                MessageBox.Show($"The client could not be added: {exception.Message}", "New client", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            UpdateMembershipExpiry(editor.Member);
            members.Add(editor.Member);
            if (payment is not null) payments.Insert(0, payment);
            UpdateMemberExcelBackup();
            RefreshMemberFilter();
            RefreshDashboardSummary();
        }
    }

    private async void RenewMembershipButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (memberRepository is null || MembersList.SelectedItem is not Member selectedMember)
        {
            MessageBox.Show("Select a client to renew their membership.", "Renew membership", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (!plans.Any(plan => plan.IsActive))
        {
            MessageBox.Show("There are no active membership plans available.", "Renew membership", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var editor = new MemberEditorWindow(selectedMember, plans, isRenewal: true) { Owner = this };
        if (editor.ShowDialog() != true) return;

        var payment = CreateMembershipPayment(editor);
        try
        {
            await memberRepository.SaveMembershipAsync(editor.Member, payment);
        }
        catch (Exception exception) when (exception is SqliteException or KeyNotFoundException)
        {
            MessageBox.Show($"The membership could not be renewed: {exception.Message}", "Renew membership", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        UpdateMembershipExpiry(editor.Member);
        var index = members.IndexOf(selectedMember);
        if (index >= 0) members[index] = editor.Member;
        if (payment is not null) payments.Insert(0, payment);
        UpdateMemberExcelBackup();
        RefreshMemberFilter();
        RefreshDashboardSummary();
    }

    private static Payment? CreateMembershipPayment(MemberEditorWindow editor)
    {
        if (!editor.RecordPayment) return null;
        return new Payment
        {
            MemberId = editor.Member.Id,
            Amount = editor.PaymentAmount,
            PaymentDate = DateTime.Today,
            PaymentMethod = editor.PaymentMethod,
            InvoiceNumber = $"INV-{DateTime.Now:yyyyMMddHHmmssfff}",
            Status = "Paid",
            Notes = $"Membership: {editor.SelectedPlan?.Name}"
        };
    }

    private async void EditMemberButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (memberRepository is null || MembersList.SelectedItem is not Member selectedMember)
        {
            MessageBox.Show("Select a member to edit.", "Members", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var editor = new MemberEditorWindow(selectedMember, plans) { Owner = this };
        if (editor.ShowDialog() == true)
        {
            string? savedPhotoPath = null;
            try
            {
                if (editor.CapturedPhoto is { Length: > 0 } photo)
                {
                    savedPhotoPath = MemberPhotoStorage.Save(photo);
                    editor.Member.PhotoPath = savedPhotoPath;
                }

                await memberRepository.UpdateAsync(editor.Member);
            }
            catch (Exception exception) when (exception is SqliteException or KeyNotFoundException or IOException or UnauthorizedAccessException)
            {
                if (savedPhotoPath is not null) MemberPhotoStorage.Delete(savedPhotoPath);
                MessageBox.Show($"The client could not be updated: {exception.Message}", "Edit member", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            UpdateMembershipExpiry(editor.Member);
            var index = members.IndexOf(selectedMember);
            members[index] = editor.Member;
            UpdateMemberExcelBackup();
            RefreshMemberFilter();
            RefreshDashboardSummary();
        }
    }

    private async void DeleteMemberButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (memberRepository is null || MembersList.SelectedItem is not Member selectedMember)
        {
            MessageBox.Show("Select a member to delete.", "Members", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var result = MessageBox.Show(
            $"Permanently remove {selectedMember.FullName} and their payment and attendance history?",
            "Confirm client removal",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);
        if (result == MessageBoxResult.Yes)
        {
            try
            {
                await memberRepository.DeleteWithHistoryAsync(selectedMember.Id);
            }
            catch (Exception exception) when (exception is SqliteException or KeyNotFoundException)
            {
                MessageBox.Show($"The client could not be removed: {exception.Message}", "Remove client", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            members.Remove(selectedMember);
            foreach (var payment in payments.Where(payment => payment.MemberId == selectedMember.Id).ToList())
            {
                payments.Remove(payment);
            }
            foreach (var record in attendance.Where(record => record.MemberId == selectedMember.Id).ToList())
            {
                attendance.Remove(record);
            }
            UpdateMemberExcelBackup();
            RefreshMemberFilter();
            RefreshDashboardSummary();
        }
    }

    private void RefreshDashboardSummary()
    {
        var summary = new DashboardService().GetSummary(members, plans, payments);
        TotalMembersText.Text = summary.TotalMembers.ToString();
        ActiveMembersText.Text = summary.ActiveMembers.ToString();
        ExpiredMembersText.Text = summary.ExpiredMembers.ToString();
        MonthlyRevenueText.Text = summary.MonthlyRevenue.ToString("N0");
        var attendanceKpis = new ReportingService().GetAttendanceKpis(attendance, DateTime.Now);
        MembersInGymNowText.Text = attendanceKpis.MembersInGymNow.ToString();
        TodaysCheckInsText.Text = attendanceKpis.TodaysCheckIns.ToString();
        AttendanceKpiBar.Value = Math.Clamp((double)attendanceKpis.MonthlyAttendance / 20 * 100, 0, 100);
        AttendanceKpiValue.Text = attendanceKpis.MonthlyAttendance.ToString();
        var activeMemberCount = members.Count(member => member.SubscriptionStatus == "Active");
        var floorActivity = activeMemberCount == 0
            ? 0
            : Math.Clamp((double)attendanceKpis.MembersInGymNow / activeMemberCount, 0, 1);
        ActivityRingText.Text = $"{Math.Round(floorActivity * 100):0}%";
        FloorActivityBar.Value = floorActivity * 100;
        FloorActivitySummaryText.Text = $"{attendanceKpis.MembersInGymNow} of {activeMemberCount} active members on the floor";
        RefreshAttendanceTrend();
    }

    private void RefreshAttendanceTrend()
    {
        if (AttendanceTrendCanvas is null || AttendanceTrendCanvas.ActualWidth <= 1) return;
        AttendanceTrendCanvas.Children.Clear();
        var firstDate = DateTime.Today.AddDays(-6);
        var counts = Enumerable.Range(0, 7)
            .Select(offset => attendance.Count(record => record.CheckInDate.Date == firstDate.AddDays(offset)))
            .ToArray();
        var chartWidth = AttendanceTrendCanvas.ActualWidth;
        var chartHeight = AttendanceTrendCanvas.ActualHeight;
        var maximum = Math.Max(1, counts.Max());
        const double baseline = 65;
        const double maximumBarHeight = 38;
        var slotWidth = chartWidth / counts.Length;
        var barWidth = Math.Min(22, slotWidth * 0.46);
        for (var index = 0; index < counts.Length; index++)
        {
            var barHeight = counts[index] == 0 ? 0 : Math.Max(4, counts[index] / (double)maximum * maximumBarHeight);
            var left = index * slotWidth + (slotWidth - barWidth) / 2;
            var track = new Border
            {
                Width = barWidth,
                Height = maximumBarHeight,
                CornerRadius = new CornerRadius(barWidth / 2),
                Background = new SolidColorBrush(Color.FromRgb(229, 237, 247))
            };
            Canvas.SetLeft(track, left);
            Canvas.SetTop(track, baseline - maximumBarHeight);
            AttendanceTrendCanvas.Children.Add(track);

            if (barHeight > 0)
            {
                var bar = new Border
                {
                    Width = barWidth,
                    Height = barHeight,
                    CornerRadius = new CornerRadius(barWidth / 2),
                    Background = new SolidColorBrush(index == counts.Length - 1
                        ? Color.FromRgb(23, 105, 224)
                        : Color.FromRgb(119, 169, 235))
                };
                Canvas.SetLeft(bar, left);
                Canvas.SetTop(bar, baseline - barHeight);
                AttendanceTrendCanvas.Children.Add(bar);
            }

            var countLabel = new TextBlock
            {
                Width = slotWidth,
                Text = counts[index].ToString(),
                FontSize = 9,
                FontWeight = index == counts.Length - 1 ? FontWeights.Bold : FontWeights.Normal,
                Foreground = (Brush)FindResource("MutedBrush"),
                TextAlignment = TextAlignment.Center
            };
            Canvas.SetLeft(countLabel, index * slotWidth);
            Canvas.SetTop(countLabel, Math.Max(0, baseline - (barHeight > 0 ? barHeight : maximumBarHeight) - 15));
            AttendanceTrendCanvas.Children.Add(countLabel);

            var label = new TextBlock
            {
                Text = firstDate.AddDays(index).ToString("ddd"),
                FontSize = 9,
                Width = slotWidth,
                Foreground = (Brush)FindResource("MutedBrush"),
                TextAlignment = TextAlignment.Center
            };
            Canvas.SetLeft(label, index * slotWidth);
            Canvas.SetTop(label, chartHeight - 15);
            AttendanceTrendCanvas.Children.Add(label);
        }
    }

    private void OnDashboardDateTimerTick(object? sender, EventArgs e)
    {
        foreach (var historyEntry in memberHistory)
        {
            historyEntry.UpdateRecencyLabel();
        }

        RefreshDashboardSummary();
        var today = DateTime.Today;
        if (today == lastDashboardDate) return;

        lastDashboardDate = today;
        RefreshMemberFilter();
        RefreshReports();
    }

    private void OnBadgeCheckInRecorded(Attendance record)
    {
        attendance.Insert(0, record);
        var member = members.FirstOrDefault(candidate => candidate.Id == record.MemberId);
        if (member is not null)
        {
            var entry = new MemberHistoryEntry(member, record);
            memberHistory.Insert(0, entry);
            RefreshMemberHistorySummary();
            if (MemberHistoryPage.Visibility == Visibility.Visible)
            {
                MemberHistoryList.ScrollIntoView(entry);
            }
        }
        RefreshDashboardSummary();
        RefreshReports();
    }

    private void PopulateMemberHistory()
    {
        memberHistory.Clear();
        var membersById = members.ToDictionary(member => member.Id);
        foreach (var record in attendance.OrderByDescending(item => item.Id))
        {
            if (membersById.TryGetValue(record.MemberId, out var member))
            {
                memberHistory.Add(new MemberHistoryEntry(member, record));
            }
        }
        RefreshMemberHistorySummary();
    }

    private void RefreshMemberHistorySummary()
    {
        LatestCheckInText.Text = memberHistory.Count == 0
            ? "No entries yet"
            : $"{memberHistory[0].Member.FullName} · {memberHistory[0].CheckedInAt:MMM d, yyyy h:mm tt}";
    }

    private void OnPaymentsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) => RefreshRevenueViews();

    private void RefreshRevenueViews()
    {
        revenueReportsPanel?.SetPayments(payments);
        RefreshDashboardSummary();
        RefreshReports();
    }

    private void RefreshReports()
    {
        var summary = new ReportingService().GetSummary(members, plans, payments, attendance);
        PaidRevenueReportText.Text = summary.PaidRevenue.ToString("N0");
        PendingPaymentsReportText.Text = summary.PendingPayments.ToString();
        ExpiringMembersReportText.Text = summary.ExpiringMembers.ToString();
        MonthlyAttendanceReportText.Text = summary.AttendanceThisMonth.ToString();

    }

    private void UpdateMemberExcelBackup()
    {
        var backupDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "StarGym Backups");
        var backupPath = Path.Combine(backupDirectory, "StarGym-Member-Backup.xlsx");
        try
        {
            memberExcelBackupService.Export(backupPath, members, plans);
        }
        catch (Exception exception) when (exception is IOException
            or UnauthorizedAccessException
            or InvalidOperationException
            or ArgumentException
            or System.Xml.XmlException)
        {
            MessageBox.Show(
                $"StarGym saved the member data, but could not update the Excel backup at:\n{backupPath}\n\n{exception.Message}",
                "Excel backup unavailable",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
    }

    private void BackupButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (databasePath is null) return;
        var backupDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "StarGym Backups");
        var backupPath = new BackupService().CreateBackup(databasePath, backupDirectory);
        MessageBox.Show($"Backup created:\n{backupPath}", "Backup", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private bool FilterMember(object item)
    {
        if (item is not Member member)
        {
            return false;
        }

        var search = MemberSearchBox.Text.Trim();
        var status = (MemberStatusFilter.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? string.Empty;
        var matchesSearch = string.IsNullOrWhiteSpace(search)
            || member.FullName.Contains(search, StringComparison.OrdinalIgnoreCase)
            || member.Phone.Contains(search, StringComparison.OrdinalIgnoreCase)
            || member.BadgeId.Contains(search, StringComparison.OrdinalIgnoreCase);

        return matchesSearch && (string.IsNullOrEmpty(status) || member.SubscriptionStatus == status);
    }
}
