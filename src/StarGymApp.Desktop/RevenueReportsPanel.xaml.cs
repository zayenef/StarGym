using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using StarGymApp.Core.Models;
using StarGymApp.Core.Services;

namespace StarGymApp.Desktop;

public partial class RevenueReportsPanel : UserControl
{
    private readonly ObservableCollection<RevenuePeriodPoint> monthlyRevenue = new();
    private readonly ObservableCollection<RevenuePeriodPoint> yearlyRevenue = new();
    private readonly ReportingService reportingService = new();
    private IReadOnlyList<Payment> payments = Array.Empty<Payment>();

    public RevenueReportsPanel()
    {
        InitializeComponent();
        MonthlyChart.ItemsSource = monthlyRevenue;
        YearlyChart.ItemsSource = yearlyRevenue;
    }

    public void SetPayments(IEnumerable<Payment> paymentRecords)
    {
        payments = paymentRecords.ToList();
        var years = payments.Where(payment => payment.Status == "Paid").Select(payment => payment.PaymentDate.Year).Distinct().OrderByDescending(year => year).ToList();
        var selectedYear = YearFilter.SelectedItem is int currentYear && years.Contains(currentYear)
            ? currentYear
            : years.FirstOrDefault();
        YearFilter.ItemsSource = years;
        if (years.Count == 0)
        {
            SelectedYearText.Text = string.Empty;
            monthlyRevenue.Clear();
            yearlyRevenue.Clear();
            MonthlyTimelineCanvas.Children.Clear();
            return;
        }

        YearFilter.SelectedItem = selectedYear;
        RefreshRevenueCharts(selectedYear);
    }

    private void YearFilter_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (YearFilter.SelectedItem is not int year) return;
        RefreshRevenueCharts(year);
    }

    private void RefreshRevenueCharts(int year)
    {
        SelectedYearText.Text = year.ToString();
        var monthly = reportingService.GetRevenueByMonth(payments, year).ToList();
        var maxMonthly = Math.Max(1, monthly.Max(point => point.Revenue));
        monthlyRevenue.Clear();
        foreach (var point in monthly)
        {
            point.BarWidth = (double)point.Revenue / (double)maxMonthly * 100;
            monthlyRevenue.Add(point);
        }
        DrawMonthlyTimeline(monthly);

        var yearly = reportingService.GetRevenueByYear(payments).ToList();
        var maxYearly = Math.Max(1, yearly.Max(point => point.Revenue));
        yearlyRevenue.Clear();
        foreach (var point in yearly)
        {
            point.BarWidth = (double)point.Revenue / (double)maxYearly * 100;
            yearlyRevenue.Add(point);
        }
    }

    private void MonthlyTimelineCanvas_OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (YearFilter.SelectedItem is int year)
        {
            DrawMonthlyTimeline(reportingService.GetRevenueByMonth(payments, year));
        }
    }

    private void DrawMonthlyTimeline(IReadOnlyList<RevenuePeriodPoint> points)
    {
        if (MonthlyTimelineCanvas is null) return;
        MonthlyTimelineCanvas.Children.Clear();
        if (points.Count == 0 || MonthlyTimelineCanvas.ActualWidth < 100) return;
        var left = 48d;
        var right = 18d;
        var top = 24d;
        var bottom = 48d;
        var width = Math.Max(100, MonthlyTimelineCanvas.ActualWidth - left - right);
        var height = MonthlyTimelineCanvas.ActualHeight - top - bottom;
        var max = Math.Max(1, points.Max(point => point.Revenue));

        for (var index = 0; index < 4; index++)
        {
            var y = top + height * index / 3;
            var gridLine = new Line { X1 = left, X2 = left + width, Y1 = y, Y2 = y, Stroke = new SolidColorBrush(Color.FromRgb(224, 232, 243)), StrokeThickness = 1 };
            MonthlyTimelineCanvas.Children.Add(gridLine);
            var label = new TextBlock { Text = (max * (3 - index) / 3).ToString("N0"), Foreground = new SolidColorBrush(Color.FromRgb(107, 126, 153)), FontSize = 10 };
            Canvas.SetLeft(label, 0); Canvas.SetTop(label, y - 8); MonthlyTimelineCanvas.Children.Add(label);
        }

        var pointsOnChart = new PointCollection();
        for (var index = 0; index < points.Count; index++)
        {
            var x = left + width * index / Math.Max(1, points.Count - 1);
            var y = top + height - ((double)points[index].Revenue / (double)max * height);
            pointsOnChart.Add(new Point(x, y));
            var dot = new Ellipse { Width = 10, Height = 10, Fill = new SolidColorBrush(Color.FromRgb(23, 105, 224)), Stroke = Brushes.White, StrokeThickness = 2 };
            Canvas.SetLeft(dot, x - 5); Canvas.SetTop(dot, y - 5); MonthlyTimelineCanvas.Children.Add(dot);
            var value = new TextBlock { Text = points[index].Revenue.ToString("N0"), Foreground = new SolidColorBrush(Color.FromRgb(16, 35, 63)), FontSize = 10, FontWeight = FontWeights.Bold };
            Canvas.SetLeft(value, Math.Max(left, Math.Min(left + width - 44, x - 20))); Canvas.SetTop(value, Math.Max(0, y - 23)); MonthlyTimelineCanvas.Children.Add(value);
            var month = new TextBlock { Text = points[index].PeriodLabel, Foreground = new SolidColorBrush(Color.FromRgb(107, 126, 153)), FontSize = 10 };
            Canvas.SetLeft(month, Math.Max(left - 8, Math.Min(left + width - 28, x - 14))); Canvas.SetTop(month, top + height + 12); MonthlyTimelineCanvas.Children.Add(month);
        }

        MonthlyTimelineCanvas.Children.Add(new Polyline { Points = pointsOnChart, Stroke = new SolidColorBrush(Color.FromRgb(23, 105, 224)), StrokeThickness = 3, StrokeLineJoin = PenLineJoin.Round });
    }
}
