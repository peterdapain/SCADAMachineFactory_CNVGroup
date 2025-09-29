using OfficeOpenXml;
using OfficeOpenXml.Style;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Drawing;

namespace GiamSatNhaMay
{
    public partial class DataGridWindow : Window
    {
        private readonly DatabaseDAO dao;
        private readonly StatisticsService statsSvc;
        private readonly HashSet<int> selectedMachineIds = new HashSet<int>();

        public DataGridWindow(DatabaseDAO dao, StatisticsService statsSvc)
        {
            InitializeComponent();
            this.dao = dao;
            this.statsSvc = statsSvc;
            Loaded += DataGridWindow_Loaded;
        }

        private void DataGridWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                var types = dao.GetMachineTypes();
                var items = new List<string> { "-- Tất cả --" };
                items.AddRange(types);

                cbTypeFilter.ItemsSource = items;
                cbTypeFilter.SelectedIndex = 0;

                dpStartDate.SelectedDate = DateTime.Today.AddDays(-7);
                dpEndDate.SelectedDate = DateTime.Today;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi tải dữ liệu: " + ex.Message);
            }
        }

        private void cbTypeFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cbTypeFilter.SelectedItem == null) return;
            string typeName = cbTypeFilter.SelectedItem.ToString();

            List<MachineOption> machines;
            if (typeName == "-- Tất cả --")
                machines = dao.GetAllMachines()
                              .Select(m => new MachineOption { Id = m.MachineID, Name = m.MachineName })
                              .ToList();
            else
                machines = dao.GetMachinesByTypeName(typeName);

            icMachines.ItemsSource = machines;
        }

        private void MachineCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox cb && cb.Tag is int id)
                selectedMachineIds.Add(id);
        }

        private void MachineCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox cb && cb.Tag is int id)
                selectedMachineIds.Remove(id);
        }

        private void btnRun_Click(object sender, RoutedEventArgs e)
        {
            if (dpStartDate.SelectedDate == null || dpEndDate.SelectedDate == null)
            {
                MessageBox.Show("Vui lòng chọn ngày bắt đầu và kết thúc.");
                return;
            }

            DateTime start = dpStartDate.SelectedDate.Value.Date;
            DateTime end = dpEndDate.SelectedDate.Value.Date.AddDays(1).AddTicks(-1);

            ChartPanel.Children.Clear();

            foreach (var machineId in selectedMachineIds)
            {
                try
                {
                    var logs = dao.GetMachineLogs(machineId, start, end);
                    if (logs.Count == 0) continue;

                    var dailyStats = statsSvc.CalculateDailyStatistics(machineId, logs, start, end);

                    // Lấy tên máy thay vì ID
                    var machine = dao.GetAllMachines().FirstOrDefault(m => m.MachineID == machineId);
                    string machineName = string.IsNullOrEmpty(machine.MachineName)
                        ? $"Máy {machineId}"
                        : machine.MachineName;

                    // Thêm hiệu suất: Run / Tổng
                    var effValues = dailyStats.Select(s =>
                    {
                        double total = s.RunMinutes + s.StopMinutes + s.ErrorMinutes;
                        return total > 0 ? Math.Round(s.RunMinutes / total * 100, 1) : 0;
                    }).ToList();

                    var transposed = new List<RowData>
                    {
                        new RowData
                        {
                            Label = "RUN (phút)",
                            Values = dailyStats.Select(s => s.RunMinutes).ToList(),
                            Dates = dailyStats.Select(s => s.DateLabel).ToList(),
                            ValuesTotal = dailyStats.Sum(s => s.RunMinutes)
                        },
                        new RowData
                        {
                            Label = "STOP (phút)",
                            Values = dailyStats.Select(s => s.StopMinutes).ToList(),
                            Dates = dailyStats.Select(s => s.DateLabel).ToList(),
                            ValuesTotal = dailyStats.Sum(s => s.StopMinutes)
                        },
                        new RowData
                        {
                            Label = "ERROR (phút)",
                            Values = dailyStats.Select(s => s.ErrorMinutes).ToList(),
                            Dates = dailyStats.Select(s => s.DateLabel).ToList(),
                            ValuesTotal = dailyStats.Sum(s => s.ErrorMinutes)
                        },
                        new RowData
                        {
                            Label = "Hiệu suất (%)",
                            Values = dailyStats.Select(s =>
                            {
                                double total = s.RunMinutes + s.StopMinutes + s.ErrorMinutes;
                                return total > 0 ? Math.Round(s.RunMinutes / total * 100, 1) : 0;
                            }).ToList(),
                            Dates = dailyStats.Select(s => s.DateLabel).ToList(),
                            ValuesTotal = dailyStats.Sum(s => s.RunMinutes) /
                                          Math.Max(1, dailyStats.Sum(s => s.RunMinutes + s.StopMinutes + s.ErrorMinutes)) * 100
                        }
                    };


                    DataGrid dg = new DataGrid
                    {
                        AutoGenerateColumns = false,
                        IsReadOnly = true,
                        Margin = new Thickness(0, 0, 0, 20),
                        HeadersVisibility = DataGridHeadersVisibility.Column,
                        ItemsSource = transposed,
                        AlternatingRowBackground = System.Windows.Media.Brushes.AliceBlue,
                        RowHeaderWidth = 0
                    };

                    // Cột đầu tiên (Loại)
                    dg.Columns.Add(new DataGridTextColumn
                    {
                        Header = "Thông số",
                        Binding = new System.Windows.Data.Binding("Label"),
                        Width = 150
                    });

                    // Các cột theo ngày
                    for (int i = 0; i < dailyStats.Count; i++)
                    {
                        int idx = i;
                        dg.Columns.Add(new DataGridTextColumn
                        {
                            Header = dailyStats[i].DateLabel,
                            Binding = new System.Windows.Data.Binding($"Values[{idx}]")
                            {
                                StringFormat = transposed.Any(r => r.Label.Contains("Hiệu suất")) ? "F1" : "F0"
                            },
                            Width = 100
                        });
                    }

                    // Cột tổng
                    dg.Columns.Add(new DataGridTextColumn
                    {
                        Header = "Tổng",
                        Binding = new System.Windows.Data.Binding("ValuesTotal")
                        {
                            StringFormat = transposed.Any(r => r.Label.Contains("Hiệu suất")) ? "F1" : "F0"
                        },
                        Width = 100
                    });

                    GroupBox gb = new GroupBox
                    {
                        Header = machineName,
                        Content = dg,
                        Margin = new Thickness(0, 0, 0, 20),
                        FontWeight = FontWeights.SemiBold,
                        BorderBrush = System.Windows.Media.Brushes.SteelBlue,
                        BorderThickness = new Thickness(1),
                        Padding = new Thickness(5)
                    };

                    ChartPanel.Children.Add(gb);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Lỗi xử lý máy {machineId}: {ex.Message}");
                }
            }
        }
        private void btnExport_Click(object sender, RoutedEventArgs e)
        {
            if (dpStartDate.SelectedDate == null || dpEndDate.SelectedDate == null)
            {
                MessageBox.Show("Vui lòng chọn ngày bắt đầu và kết thúc.");
                return;
            }

            DateTime start = dpStartDate.SelectedDate.Value.Date;
            DateTime endDate = dpEndDate.SelectedDate.Value.Date.AddDays(1).AddTicks(-1);

            var sfd = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "Excel files (*.xlsx)|*.xlsx",
                FileName = $"BaoCao_{DateTime.Now:yyyyMMddHHmmss}.xlsx"
            };

            if (sfd.ShowDialog() != true) return;

            try
            {
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

                using var package = new ExcelPackage();

                foreach (var machineId in selectedMachineIds)
                {
                    var logs = dao.GetMachineLogs(machineId, start, endDate);
                    if (logs == null || logs.Count == 0) continue;

                    var dailyStats = statsSvc.CalculateDailyStatistics(machineId, logs, start, endDate);
                    var machine = dao.GetAllMachines().FirstOrDefault(m => m.MachineID == machineId);
                    string sheetName = machine.MachineName ?? $"Máy_{machineId}";

                    foreach (char c in Path.GetInvalidFileNameChars())
                        sheetName = sheetName.Replace(c.ToString(), "");
                    if (sheetName.Length > 31)
                        sheetName = sheetName.Substring(0, 31);

                    var ws = package.Workbook.Worksheets.Add(sheetName);
                    int currentRow = 1;

                    // ===== Logo công ty =====
                    string logoPath = @"D:\Project\C#_WPF_GiamSatNhaMay\GiamSatNhaMay\Image\Logo_CNVGroup1.png";
                    if (File.Exists(logoPath))
                    {
                        var pic = ws.Drawings.AddPicture("Logo", new FileInfo(logoPath));
                        pic.SetPosition(0, 1, 0, 0);
                        pic.SetSize(200, 70);
                    }

                    // ===== Tên công ty và cơ sở =====
                    ws.Cells[currentRow, 3].Value = "Công Ty Cổ Phần Tập Đoàn Công Nghiệp Việt";
                    ws.Cells[currentRow, 3, currentRow, dailyStats.Count].Merge = true;
                    ws.Cells[currentRow, 3].Style.Font.Bold = true;
                    ws.Cells[currentRow, 3].Style.Font.Size = 10;
                    ws.Cells[currentRow, 3].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                    currentRow++;

                    ws.Cells[currentRow, 3].Value = "ĐC: 137A Nguyễn Văn Cừ, Q. Long Biên, Hà Nội";
                    ws.Cells[currentRow, 3, currentRow, dailyStats.Count].Merge = true;
                    ws.Cells[currentRow, 3].Style.Font.Italic = true;
                    ws.Cells[currentRow, 3].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                    currentRow++;

                    ws.Cells[currentRow, 3].Value = "ĐT: (84-24) 22 207 918 – Fax: (84-24) 22 207 919";
                    ws.Cells[currentRow, 3, currentRow, dailyStats.Count].Merge = true;
                    ws.Cells[currentRow, 3].Style.Font.Italic = true;
                    ws.Cells[currentRow, 3].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                    currentRow += 2;

                    // ===== Tiêu đề báo cáo =====
                    bool groupByWeek = (endDate - start).TotalDays > 14;

                    List<string> columnLabels;
                    if (!groupByWeek)
                    {
                        // Theo từng ngày
                        columnLabels = dailyStats.Select(s => s.DateLabel).ToList();
                    }
                    else
                    {
                        // Nhóm theo tuần
                        columnLabels = dailyStats
                            .Select((s, idx) => new { Stat = s, Index = idx })
                            .GroupBy(x => x.Index / 7)
                            .Select(g => $"Tuần {g.Key + 1} ({g.First().Stat.DateLabel}-{g.Last().Stat.DateLabel})")
                            .ToList();
                    }

                    int totalColumns = columnLabels.Count + 2; // 1 cột "Thông số" + cột dữ liệu + 1 cột tổng

                    ws.Cells[currentRow, 1, currentRow, totalColumns].Merge = true;
                    ws.Cells[currentRow, 1].Value = "BÁO CÁO SẢN XUẤT";
                    ws.Cells[currentRow, 1].Style.Font.Bold = true;
                    ws.Cells[currentRow, 1].Style.Font.Size = 16;
                    ws.Cells[currentRow, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    currentRow++;

                    ws.Cells[currentRow, 1, currentRow, totalColumns].Merge = true;
                    ws.Cells[currentRow, 1].Value = $"Ngày xuất: {DateTime.Now:dd/MM/yyyy HH:mm}";
                    ws.Cells[currentRow, 1].Style.Font.Italic = true;
                    ws.Cells[currentRow, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                    currentRow += 2;

                    // ===== HEADER =====
                    ws.Cells[currentRow, 1].Value = "Thông số";
                    for (int i = 0; i < columnLabels.Count; i++)
                        ws.Cells[currentRow, i + 2].Value = columnLabels[i];
                    ws.Cells[currentRow, totalColumns].Value = "Tổng";

                    using (var headerRange = ws.Cells[currentRow, 1, currentRow, totalColumns])
                    {
                        headerRange.Style.Font.Bold = true;
                        headerRange.Style.Fill.PatternType = ExcelFillStyle.Solid;
                        headerRange.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightSteelBlue);
                        headerRange.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        headerRange.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                        headerRange.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                        headerRange.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                        headerRange.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                    }

                    currentRow++;

                    // ===== DATA =====
                    string[] labels = { "RUN (phút)", "STOP (phút)", "ERROR (phút)", "Hiệu suất (%)" };
                    for (int r = 0; r < labels.Length; r++)
                    {
                        ws.Cells[currentRow + r, 1].Value = labels[r];
                        double sum = 0;

                        for (int c = 0; c < columnLabels.Count; c++)
                        {
                            double val = 0;
                            if (!groupByWeek)
                            {
                                var s = dailyStats[c];
                                switch (labels[r])
                                {
                                    case "RUN (phút)": val = s.RunMinutes; break;
                                    case "STOP (phút)": val = s.StopMinutes; break;
                                    case "ERROR (phút)": val = s.ErrorMinutes; break;
                                    case "Hiệu suất (%)":
                                        double total = s.RunMinutes + s.StopMinutes + s.ErrorMinutes;
                                        val = total > 0 ? Math.Round(s.RunMinutes / total * 100, 1) : 0;
                                        break;
                                }
                            }
                            else
                            {
                                int startIdx = c * 7;
                                int endIdx = Math.Min(startIdx + 7, dailyStats.Count);
                                double run = 0, stop = 0, error = 0;
                                for (int i = startIdx; i < endIdx; i++)
                                {
                                    run += dailyStats[i].RunMinutes;
                                    stop += dailyStats[i].StopMinutes;
                                    error += dailyStats[i].ErrorMinutes;
                                }
                                switch (labels[r])
                                {
                                    case "RUN (phút)": val = run; break;
                                    case "STOP (phút)": val = stop; break;
                                    case "ERROR (phút)": val = error; break;
                                    case "Hiệu suất (%)":
                                        double total = run + stop + error;
                                        val = total > 0 ? Math.Round(run / total * 100, 1) : 0;
                                        break;
                                }
                            }

                            ws.Cells[currentRow + r, c + 2].Value = val;
                            ws.Cells[currentRow + r, c + 2].Style.Numberformat.Format = "0.00";
                            sum += val;
                        }

                        // Cột Tổng
                        if (labels[r] == "Hiệu suất (%)")
                            ws.Cells[currentRow + r, totalColumns].Value = Math.Round(
                                dailyStats.Sum(s => s.RunMinutes) /
                                Math.Max(1, dailyStats.Sum(s => s.RunMinutes + s.StopMinutes + s.ErrorMinutes)) * 100, 1);
                        else
                            ws.Cells[currentRow + r, totalColumns].Value = sum;

                        // Alternating row màu sắc
                        if (r % 2 == 0)
                        {
                            ws.Cells[currentRow + r, 1, currentRow + r, totalColumns].Style.Fill.PatternType = ExcelFillStyle.Solid;
                            ws.Cells[currentRow + r, 1, currentRow + r, totalColumns].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.AliceBlue);
                        }

                        // Border cho từng row
                        using (var rowRange = ws.Cells[currentRow + r, 1, currentRow + r, totalColumns])
                        {
                            rowRange.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                            rowRange.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                            rowRange.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                            rowRange.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                        }
                    }

                    // ===== STYLE CỘT TỔNG =====
                    ws.Cells[currentRow, totalColumns, currentRow + labels.Length - 1, totalColumns].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    ws.Cells[currentRow, totalColumns, currentRow + labels.Length - 1, totalColumns].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGoldenrodYellow);
                    ws.Cells[currentRow, totalColumns, currentRow + labels.Length - 1, totalColumns].Style.Font.Bold = true;

                    // ===== Tăng độ rộng cột =====
                    for (int col = 1; col <= totalColumns; col++)
                    {
                        if (col == 1) ws.Column(col).Width = 20;
                        else ws.Column(col).Width = 15;
                    }
                }

                package.SaveAs(new FileInfo(sfd.FileName));
                MessageBox.Show("Xuất Excel thành công!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi xuất Excel: " + ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
    

}
