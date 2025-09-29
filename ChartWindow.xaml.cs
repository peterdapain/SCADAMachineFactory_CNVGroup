using LiveCharts;
using LiveCharts.Wpf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace GiamSatNhaMay
{
    public partial class ChartWindow : Window
    {
        private DatabaseDAO dao;
        private StatisticsService statsSvc;
        private HashSet<int> selectedMachineIds = new HashSet<int>();

        public ChartWindow(DatabaseDAO dao)
        {
            InitializeComponent();
            this.dao = dao ?? throw new ArgumentNullException(nameof(dao));
            this.statsSvc = new StatisticsService(this.dao);

            dpStartDate.SelectedDate = DateTime.Now.Date;
            dpEndDate.SelectedDate = DateTime.Now.Date;
            tbStartTime.Text = "00:00";
            tbEndTime.Text = DateTime.Now.ToString("HH:mm");

            LoadTypes();
        }
        //
        #region Load types & machines
        private void LoadTypes()
        {   
            // get types from DatabaseDAO
            var types = dao.GetMachineTypes();
            var items = new List<string> { "-- Tất cả --" };
            items.AddRange(types);
            cbTypeFilter.ItemsSource = items;
            cbTypeFilter.SelectedIndex = 0;
        }

        private void CbTypeFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var sel = cbTypeFilter.SelectedItem as string;
            List<MachineOption> machines;
            if (string.IsNullOrEmpty(sel) || sel == "-- Tất cả --")
                machines = dao.GetAllMachines().Select(t => new MachineOption { Id = t.MachineID, Name = t.MachineName }).ToList();
            else
                machines = dao.GetMachinesByTypeName(sel);

            icMachines.ItemsSource = machines;
            selectedMachineIds.Clear();
            ChartPanel.Children.Clear();
        }
        #endregion

        #region Checkbox events
        private async void MachineCheckChanged(object sender, RoutedEventArgs e)
        {
            if (!GetSelectedTimeRange(out DateTime start, out DateTime end)) return;
            var cb = sender as CheckBox;
            if (cb?.Tag == null) return;

            int machineId = (int)cb.Tag;
            string machineName = cb.Content?.ToString() ?? machineId.ToString();

            if (cb.IsChecked == true)
            {   
                // Selected Machine on checkbox
                selectedMachineIds.Add(machineId);
                txtStatus.Text = $"Đang tải dữ liệu {machineName} ...";
                btnRun.IsEnabled = false;
                try
                {   
                    // Get stactis
                    var buckets = await Task.Run(() => statsSvc.GetBucketedStatistics(machineId, start, end));
                    // call function Chart
                    AddChartForMachine(machineName, buckets, end);
                    UpdateTotals();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Lỗi khi thống kê máy {machineName}: {ex.Message}");
                }
                finally
                {
                    btnRun.IsEnabled = true;
                    txtStatus.Text = "";
                }
            }
            else
            {
                selectedMachineIds.Remove(machineId);
                RemoveChartForMachine(machineName);
                UpdateTotals();
            }
        }
        #endregion

        #region Button actions
        private async void BtnRun_Click(object sender, RoutedEventArgs e)
        {
            if (!GetSelectedTimeRange(out DateTime start, out DateTime end)) return;
            var checkedMachines = icMachines.ItemsSource
                                        ?.Cast<MachineOption>()
                                        .Where(m => selectedMachineIds.Contains(m.Id))
                                        .ToList() ?? new List<MachineOption>();
            if (checkedMachines.Count == 0)
            {
                MessageBox.Show("Vui lòng chọn ít nhất 1 máy.");
                return;
            }

            btnRun.IsEnabled = false;
            txtStatus.Text = "Đang thống kê...";
            ChartPanel.Children.Clear();

            var tasks = checkedMachines.Select(m => Task.Run(() =>
            {
                var buckets = statsSvc.GetBucketedStatistics(m.Id, start, end);
                return (m.Name, buckets);
            })).ToArray();

            var results = await Task.WhenAll(tasks);
            foreach (var r in results)
            {
                AddChartForMachine(r.Name, r.buckets, end);
            }

            UpdateTotals();
            txtStatus.Text = "Hoàn thành";
            btnRun.IsEnabled = true;
        }

        private void BtnClearSelection_Click(object sender, RoutedEventArgs e)
        {
            selectedMachineIds.Clear();
            var src = icMachines.ItemsSource;
            icMachines.ItemsSource = null;
            icMachines.ItemsSource = src;
            ChartPanel.Children.Clear();
            UpdateTotals();
        }
        #endregion

        #region Chart helpers
        private void AddChartForMachine(string machineName, List<BucketStatistic> buckets, DateTime endTime)
        {
            RemoveChartForMachine(machineName);

            var runVals = new ChartValues<double>(buckets.Select(b => b.RunMinutes));
            var stopVals = new ChartValues<double>(buckets.Select(b => b.StopMinutes));
            var errVals = new ChartValues<double>(buckets.Select(b => b.ErrorMinutes));
            var labels = buckets.Select(b => b.Label).ToList();

            // Đảm bảo kéo tới endTime nếu cần
            if (labels.Count == 0 || labels.Last() != endTime.ToString("HH:mm"))
            {
                labels.Add(endTime.ToString("HH:mm"));
                runVals.Add(0);
                stopVals.Add(0);
                errVals.Add(0);
            }
            // chart setting
            var chart = new CartesianChart
            {
                Width = 570,
                Height = 300,
                LegendLocation = LegendLocation.Top,
                Series = new SeriesCollection
                {
                    new StackedColumnSeries { Title = "RUN", Values = runVals, Fill=Brushes.Green },
                    new StackedColumnSeries { Title = "STOP", Values = stopVals, Fill=Brushes.Red },
                    new StackedColumnSeries { Title = "ERROR", Values = errVals, Fill=Brushes.Orange }
                },
                AxisX = new AxesCollection
                {
                    new Axis { Title = "Thời gian", Labels = labels, LabelsRotation = 30 }
                },
                AxisY = new AxesCollection
                {
                    new Axis { Title = "Phút" }
                }
            };

            var border = new Border
            {
                BorderBrush = Brushes.Gray,
                BorderThickness = new Thickness(1),
                Margin = new Thickness(6),
                Child = new StackPanel
                {
                    Children =
                    {
                        new TextBlock { Text = machineName, FontWeight=FontWeights.Bold, Margin=new Thickness(4), HorizontalAlignment=HorizontalAlignment.Center },
                        chart
                    }
                }
            };

            ChartPanel.Children.Add(border);
        }

        private void RemoveChartForMachine(string machineName)
        {
            var toRemove = ChartPanel.Children.OfType<Border>()
                .FirstOrDefault(b => (b.Child as StackPanel)?.Children.OfType<TextBlock>().FirstOrDefault()?.Text == machineName);
            if (toRemove != null) ChartPanel.Children.Remove(toRemove);
        }
        // total
        private void UpdateTotals()
        {
            double totalRun = 0, totalStop = 0, totalErr = 0;
            foreach (var border in ChartPanel.Children.OfType<Border>())
            {
                var chart = (border.Child as StackPanel)?.Children.OfType<CartesianChart>().FirstOrDefault();
                if (chart == null) continue;
                foreach (var s in chart.Series)
                {
                    var sum = s.Values.Cast<object>().Sum(v => Convert.ToDouble(v));
                    if (s.Title == "RUN") totalRun += sum;
                    if (s.Title == "STOP") totalStop += sum;
                    if (s.Title == "ERROR") totalErr += sum;
                }
            }
            txtTotalRun.Text = $"RUN: {Math.Round(totalRun, 1)} phút";
            txtTotalStop.Text = $"STOP: {Math.Round(totalStop, 1)} phút";
            txtTotalError.Text = $"ERROR: {Math.Round(totalErr, 1)} phút";
        }
        // get time
        private bool GetSelectedTimeRange(out DateTime start, out DateTime end)
        {
            start = end = DateTime.Now;
            if (!dpStartDate.SelectedDate.HasValue || !dpEndDate.SelectedDate.HasValue) return false;
            if (!TimeSpan.TryParse(tbStartTime.Text, out TimeSpan startTime)) return false;
            if (!TimeSpan.TryParse(tbEndTime.Text, out TimeSpan endTime)) return false;
            start = dpStartDate.SelectedDate.Value.Date + startTime;
            end = dpEndDate.SelectedDate.Value.Date + endTime;
            return end > start;
        }
        #endregion
    }
}
