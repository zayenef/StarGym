using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using StarGymApp.Core.Models;
using StarGymApp.Core.Services;

namespace StarGymApp.Desktop;

public partial class RevenueReportsWindow : Window
{
    private IReadOnlyList<Payment> payments = Array.Empty<Payment>();
    private IEnumerable<Payment>? paymentSource;
    private INotifyCollectionChanged? paymentNotifications;
    private readonly ObservableCollection<RevenuePeriodPoint> monthlyRevenue = new();
    private readonly ObservableCollection<RevenuePeriodPoint> yearlyRevenue = new();
    private readonly ReportingService reportingService = new();

    public RevenueReportsWindow(IEnumerable<Payment> payments)
    {
        InitializeComponent();
        MonthlyChart.ItemsSource = monthlyRevenue;
        YearlyChart.ItemsSource = yearlyRevenue;
        SetPayments(payments);
        Closed += (_, _) =>
        {
            if (paymentNotifications is not null) paymentNotifications.CollectionChanged -= PaymentSource_OnCollectionChanged;
        };
    }

    public void SetPayments(IEnumerable<Payment> paymentRecords)
    {
        if (paymentNotifications is not null) paymentNotifications.CollectionChanged -= PaymentSource_OnCollectionChanged;
        paymentSource = paymentRecords;
        paymentNotifications = paymentRecords as INotifyCollectionChanged;
        if (paymentNotifications is not null) paymentNotifications.CollectionChanged += PaymentSource_OnCollectionChanged;
        payments = paymentRecords.ToList();

        var years = payments.Where(payment => payment.Status == "Paid").Select(payment => payment.PaymentDate.Year).Distinct().OrderByDescending(year => year).ToList();
        var selectedYear = YearFilter.SelectedItem is int currentYear && years.Contains(currentYear)
            ? currentYear
            : years.FirstOrDefault();
        YearFilter.ItemsSource = years;
        if (years.Count == 0)
        {
            monthlyRevenue.Clear();
            yearlyRevenue.Clear();
            return;
        }

        YearFilter.SelectedItem = selectedYear;
        RefreshRevenueCharts(selectedYear);
    }

    private void PaymentSource_OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (paymentSource is not null) SetPayments(paymentSource);
    }

    private void YearFilter_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (YearFilter.SelectedItem is not int year) return;
        RefreshRevenueCharts(year);
    }

    private void RefreshRevenueCharts(int year)
    {
        var monthly = reportingService.GetRevenueByMonth(payments, year).ToList();
        var maxMonthly = Math.Max(1, monthly.Max(point => point.Revenue));
        monthlyRevenue.Clear();
        foreach (var point in monthly)
        {
            point.BarWidth = (double)point.Revenue / (double)maxMonthly * 100;
            monthlyRevenue.Add(point);
        }

        var yearly = reportingService.GetRevenueByYear(payments).ToList();
        var maxYearly = Math.Max(1, yearly.Max(point => point.Revenue));
        yearlyRevenue.Clear();
        foreach (var point in yearly)
        {
            point.BarWidth = (double)point.Revenue / (double)maxYearly * 100;
            yearlyRevenue.Add(point);
        }
    }

    private void CloseButton_OnClick(object sender, RoutedEventArgs e) => Close();
}
