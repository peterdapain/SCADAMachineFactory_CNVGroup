using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using LiveCharts;
using LiveCharts.Wpf;

namespace GiamSatNhaMay
{
    public partial class PopupWindow : Window
    {
        public PopupWindow(int machineID)
        {
            InitializeComponent();
            LoadMachineRunStopTime(machineID);
        }

        private void LoadMachineRunStopTime(int machineID)
        {
            var dao = new DatabaseDAO("Data Source=VUHUUDUC;Initial Catalog=QuanLyNhaMay;Integrated Security=True;TrustServerCertificate=True");
            var logs = dao.GetMachineLogsToday(machineID); // logs hôm nay theo thứ tự LogTime tăng dần

            // Trạng thái lúc 00:00
            string statusAtStartOfDay = dao.GetMachineStatusAtStartOfDay(machineID);

            // Tính thời gian RUN / STOP / ERROR
            TimeSpan runTime = CalculateTime(logs, "RUN", statusAtStartOfDay);
            TimeSpan stopTime = CalculateTime(logs, "STOP", statusAtStartOfDay);
            TimeSpan errorTime = CalculateTime(logs, "ERROR", statusAtStartOfDay);

            // Hiển thị text
            PopupStatus.Text = $"RUN:   {FormatTime(runTime)}";
            PopupPower.Text = $"STOP:  {FormatTime(stopTime)}";
            PopupError.Text = $"ERROR: {FormatTime(errorTime)}";

            // Biểu đồ tròn
            PieChartRunStop.Series = new SeriesCollection
            {
                new PieSeries
                {
                    Title = "RUN",
                    Values = new ChartValues<double> { runTime.TotalSeconds },
                    Fill = Brushes.Green,
                    DataLabels = true,
                    LabelPoint = cp => FormatTime(TimeSpan.FromSeconds(cp.Y))
                },
                new PieSeries
                {
                    Title = "STOP",
                    Values = new ChartValues<double> { stopTime.TotalSeconds },
                    Fill = Brushes.Red,
                    DataLabels = true,
                    LabelPoint = cp => FormatTime(TimeSpan.FromSeconds(cp.Y))
                },
                new PieSeries
                {
                    Title = "ERROR",
                    Values = new ChartValues<double> { errorTime.TotalSeconds },
                    Fill = Brushes.Orange,
                    DataLabels = true,
                    LabelPoint = cp => FormatTime(TimeSpan.FromSeconds(cp.Y))
                }
            };

            // Biểu đồ cột
            ColumnChartInfo.Series = new SeriesCollection
            {
                new ColumnSeries
                {
                    Title = "RUN",
                    Values = new ChartValues<double> { runTime.TotalSeconds },
                    Fill = Brushes.Green,
                    DataLabels = true,
                    LabelPoint = p => FormatTime(TimeSpan.FromSeconds(p.Y)),
                    ColumnPadding = 20
                },
                new ColumnSeries
                {
                    Title = "STOP",
                    Values = new ChartValues<double> { stopTime.TotalSeconds },
                    Fill = Brushes.Red,
                    DataLabels = true,
                    LabelPoint = p => FormatTime(TimeSpan.FromSeconds(p.Y)),
                    ColumnPadding = 20
                },
                new ColumnSeries
                {
                    Title = "ERROR",
                    Values = new ChartValues<double> { errorTime.TotalSeconds },
                    Fill = Brushes.Orange,
                    DataLabels = true,
                    LabelPoint = p => FormatTime(TimeSpan.FromSeconds(p.Y)), 
                    ColumnPadding = 20
                }
            };
        }

        private TimeSpan CalculateTime(List<MachineLogEntry> logs, string status, string statusAtStartOfDay)
        {
            TimeSpan total = TimeSpan.Zero;
            DateTime now = DateTime.Now;
            DateTime startOfDay = DateTime.Today;

            if (statusAtStartOfDay == status)
            {
                if (logs.Count > 0)
                    total += logs[0].LogTime - startOfDay;
                else
                    return now - startOfDay;
            }

            for (int i = 0; i < logs.Count; i++)
            {
                if (logs[i].Status == status)
                {
                    DateTime start = logs[i].LogTime;
                    DateTime end = (i + 1 < logs.Count) ? logs[i + 1].LogTime : now;
                    total += (end - start);
                }
            }
            return total;
        }

        private string FormatTime(TimeSpan time)
        {
            if (time.TotalHours >= 24)
                return $"{(int)time.TotalDays}d {time.Hours:D2}:{time.Minutes:D2}:{time.Seconds:D2}";
            return $"{time.Hours:D2}:{time.Minutes:D2}:{time.Seconds:D2}";
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
