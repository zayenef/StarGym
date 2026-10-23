using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using StarGymApp.Core.Models;

namespace StarGymApp.Desktop;

public partial class MemberEditorWindow : Window
{
    public Member Member { get; }
    public MembershipPlan? SelectedPlan => MembershipPlanBox.SelectedItem as MembershipPlan;
    public bool RecordPayment => RecordPaymentCheckBox.IsChecked == true;
    public decimal PaymentAmount { get; private set; }
    public string PaymentMethod => (PaymentMethodBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Cash";
    public byte[]? CapturedPhoto { get; private set; }
    private readonly bool isNewMember;
    private readonly bool isRenewal;
    private readonly IReadOnlyList<MembershipPlan> availablePlans;

    public MemberEditorWindow(Member? member = null, IReadOnlyList<MembershipPlan>? plans = null, bool isRenewal = false)
    {
        InitializeComponent();
        this.isRenewal = isRenewal;
        isNewMember = member is null;
        var isEditing = member is not null && !isRenewal;
        availablePlans = (plans ?? Array.Empty<MembershipPlan>()).Where(plan => plan.IsActive).ToList();
        FormTitleText.Text = isRenewal ? "Renew membership" : isEditing ? "Edit member" : "Add a member";
        FormSubtitleText.Text = isRenewal
            ? $"Renew the membership for {member!.FullName}."
            : isEditing ? "Update this member's contact details and status." : "Enter the new member's details and membership plan.";
        SaveButton.Content = isRenewal ? "Complete renewal" : isEditing ? "Save changes" : "Create member";
        Member = member is null ? new Member { JoinDate = DateTime.Today, Status = "Active" } : Copy(member);
        var photoPath = MemberPhotoStorage.ResolvePath(Member.PhotoPath);
        if (photoPath is not null)
        {
            MemberPhotoPreview.Source = LoadPhoto(photoPath);
            PhotoInstructionText.Text = "Current photo will appear in badge check-in.";
        }
        CapturePhotoButton.Content = photoPath is null ? "Open camera" : "Change photo";
        FullNameBox.Text = Member.FullName;
        BadgeIdBox.Text = Member.BadgeId;
        AgeBox.Text = Member.DateOfBirth > DateTime.MinValue ? Member.Age.ToString() : string.Empty;
        PhoneBox.Text = Member.Phone;
        EmailBox.Text = Member.Email;
        NotesBox.Text = Member.Notes;
        StatusBox.SelectedIndex = Math.Max(0, StatusBox.Items.IndexOf(StatusBox.Items.Cast<ComboBoxItem>().FirstOrDefault(item => item.Content?.ToString() == Member.Status)));
        ProfilePanel.Visibility = isRenewal ? Visibility.Collapsed : Visibility.Visible;
        SubscriptionPanel.Visibility = isEditing ? Visibility.Collapsed : Visibility.Visible;
        SubscriptionHeadingText.Text = isRenewal ? "RENEWAL DETAILS" : "MEMBERSHIP PLAN";
        SubscriptionDescriptionText.Text = isRenewal
            ? "Choose a plan, set a 1–12 month period, and enter the total price manually. The new term starts after the current term ends."
            : "Choose a plan and a 1–12 month training period, then enter the total price manually.";
        MembershipPlanBox.ItemsSource = availablePlans;
        if (isRenewal && member is not null)
        {
            var currentPlan = availablePlans.FirstOrDefault(plan => plan.Id == member.MembershipPlanId);
            var currentEndDate = member.GetMembershipExpiry(currentPlan) ?? DateTime.Today.AddDays(-1);
            Member.JoinDate = currentEndDate < DateTime.Today ? DateTime.Today : currentEndDate.AddDays(1);
            Member.Status = "Active";
            MembershipPlanBox.SelectedItem = currentPlan ?? availablePlans.FirstOrDefault();
        }
        else
        {
            MembershipPlanBox.SelectedItem = availablePlans.FirstOrDefault(plan => plan.Id == Member.MembershipPlanId)
                ?? availablePlans.FirstOrDefault();
        }
        var selectedPlan = SelectedPlan;
        var initialDurationMonths = Member.MembershipDurationMonths is >= 1 and <= 12
            ? Member.MembershipDurationMonths
            : selectedPlan is null ? 1 : Math.Clamp((selectedPlan.DurationDays + 29) / 30, 1, 12);
        MembershipDurationBox.SelectedItem = MembershipDurationBox.Items
            .Cast<ComboBoxItem>()
            .First(item => item.Tag?.ToString() == initialDurationMonths.ToString());
        PaymentAmountBox.Text = Member.MembershipPrice > 0 ? Member.MembershipPrice.ToString("0.##") : string.Empty;
        UpdatePaymentFields();
    }

    private void SaveButton_OnClick(object sender, RoutedEventArgs e)
    {
        var badgeId = BadgeIdBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(FullNameBox.Text))
        {
            MessageBox.Show("Full name is required.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (string.IsNullOrWhiteSpace(badgeId))
        {
            MessageBox.Show("Badge ID is required for every member.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!isRenewal && (!int.TryParse(AgeBox.Text, out var age) || age is < 1 or > 120))
        {
            MessageBox.Show("Enter a valid age from 1 to 120.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (isNewMember && CapturedPhoto is null)
        {
            MessageBox.Show("Capture a member photo before creating the client.", "Photo required", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (SubscriptionPanel.Visibility == Visibility.Visible && SelectedPlan is null)
        {
            MessageBox.Show("Choose an active membership plan.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (SubscriptionPanel.Visibility == Visibility.Visible && !TryGetSelectedDurationMonths(out _))
        {
            MessageBox.Show("Choose a training period from 1 to 12 months.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (SubscriptionPanel.Visibility == Visibility.Visible
            && (!decimal.TryParse(PaymentAmountBox.Text, out var amount) || amount <= 0))
        {
            MessageBox.Show("Enter the total membership price manually.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!isRenewal && !isNewMember && string.IsNullOrWhiteSpace(FullNameBox.Text))
        {
            MessageBox.Show("Full name is required.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!isRenewal)
        {
            Member.FullName = FullNameBox.Text.Trim();
            Member.BadgeId = badgeId;
            Member.DateOfBirth = DateTime.Today.AddYears(-int.Parse(AgeBox.Text));
            Member.Phone = PhoneBox.Text.Trim();
            Member.Email = EmailBox.Text.Trim();
            Member.Status = (StatusBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Active";
            Member.Notes = NotesBox.Text.Trim();
        }

        if (SubscriptionPanel.Visibility == Visibility.Visible && SelectedPlan is not null)
        {
            Member.MembershipPlanId = SelectedPlan.Id;
            TryGetSelectedDurationMonths(out var durationMonths);
            Member.MembershipDurationMonths = durationMonths;
            Member.MembershipPrice = decimal.Parse(PaymentAmountBox.Text);
            Member.Status = "Active";
            if (RecordPayment) PaymentAmount = Member.MembershipPrice;
        }

        DialogResult = true;
    }

    private void MembershipPlanBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        UpdateMembershipDates();
    }

    private void MembershipDurationBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e) => UpdateMembershipDates();

    private void UpdateMembershipDates()
    {
        if (MembershipPlanBox.SelectedItem is not MembershipPlan plan || !TryGetSelectedDurationMonths(out var months))
        {
            MembershipDatesText.Text = string.Empty;
            return;
        }

        var endDate = Member.JoinDate.Date.AddMonths(months);
        MembershipDatesText.Text = isRenewal
            ? $"New {months}-month term: {Member.JoinDate:yyyy-MM-dd} – {endDate:yyyy-MM-dd}"
            : $"{plan.Name} · {months}-month term: {Member.JoinDate:yyyy-MM-dd} – {endDate:yyyy-MM-dd}";
    }

    private bool TryGetSelectedDurationMonths(out int months)
    {
        months = 0;
        return MembershipDurationBox.SelectedItem is ComboBoxItem item
            && int.TryParse(item.Tag?.ToString(), out months)
            && months is >= 1 and <= 12;
    }

    private void RecordPaymentCheckBox_OnChanged(object sender, RoutedEventArgs e) => UpdatePaymentFields();

    private void UpdatePaymentFields()
    {
        if (PaymentAmountBox is null || PaymentMethodBox is null || RecordPaymentCheckBox is null) return;
        var enabled = RecordPaymentCheckBox.IsChecked == true;
        PaymentMethodBox.IsEnabled = enabled;
    }

    private void CancelButton_OnClick(object sender, RoutedEventArgs e) => DialogResult = false;

    private void CapturePhotoButton_OnClick(object sender, RoutedEventArgs e)
    {
        var captureWindow = new PhotoCaptureWindow { Owner = this };
        if (captureWindow.ShowDialog() != true || captureWindow.CapturedPhoto is not { Length: > 0 } photo) return;

        CapturedPhoto = photo;
        MemberPhotoPreview.Source = LoadPhoto(photo);
        PhotoInstructionText.Text = "Photo captured. It will be saved with this member.";
        CapturePhotoButton.Content = "Retake photo";
    }

    private static BitmapImage LoadPhoto(string path)
    {
        using var stream = File.OpenRead(path);
        var image = new BitmapImage();
        image.BeginInit();
        image.CacheOption = BitmapCacheOption.OnLoad;
        image.StreamSource = stream;
        image.EndInit();
        image.Freeze();
        return image;
    }

    private static BitmapImage LoadPhoto(byte[] photo)
    {
        using var stream = new MemoryStream(photo);
        var image = new BitmapImage();
        image.BeginInit();
        image.CacheOption = BitmapCacheOption.OnLoad;
        image.StreamSource = stream;
        image.EndInit();
        image.Freeze();
        return image;
    }

    private static Member Copy(Member member) => new()
    {
        Id = member.Id,
        BadgeId = member.BadgeId,
        PhotoPath = member.PhotoPath,
        FullName = member.FullName,
        Phone = member.Phone,
        Email = member.Email,
        Gender = member.Gender,
        DateOfBirth = member.DateOfBirth,
        JoinDate = member.JoinDate,
        MembershipPlanId = member.MembershipPlanId,
        MembershipDurationMonths = member.MembershipDurationMonths,
        MembershipPrice = member.MembershipPrice,
        Status = member.Status,
        TrainerId = member.TrainerId,
        Notes = member.Notes
    };
}
