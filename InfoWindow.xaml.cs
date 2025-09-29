using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace GiamSatNhaMay
{
    public partial class InfoWindow : Window
    {
        private InfoWindowDAO dao;
        private int selectedMachineID = -1;

        public InfoWindow()
        {
            InitializeComponent();
            // initialize DatabaseDAO
            dao = new InfoWindowDAO("Data Source=VUHUUDUC;Initial Catalog=QuanLyNhaMay;Integrated Security=True;TrustServerCertificate=True");
            LoadAllMachines();
        }

        private void LoadAllMachines()
        {
            // HomeWindow copy
            wrapPanelGiaCong.Children.Clear();
            wrapPanelPallet.Children.Clear();
            wrapPanelKetCau.Children.Clear();
            wrapPanelMayGianTiep.Children.Clear();

            List<MachineModel> machines = dao.GetAllMachines();

            foreach (var m in machines)
            {   
                // Create panel
                Border border = new Border
                {
                    BorderBrush = Brushes.Gray,
                    BorderThickness = new Thickness(1),
                    Padding = new Thickness(10),
                    Margin = new Thickness(5),
                    Width = 180,
                    Height = 100,
                    CornerRadius = new CornerRadius(8),
                    Background = Brushes.LightGray,
                    Cursor = System.Windows.Input.Cursors.Hand
                };
                // Add content
                StackPanel sp = new StackPanel();
                sp.Children.Add(new TextBlock { Text = m.MachineName, FontWeight = FontWeights.Bold, TextAlignment = TextAlignment.Center });          
                sp.Children.Add(new TextBlock { Text = $"Type: {m.TypeName}" });
                border.Child = sp;

                // Change color on state
                switch (m.Status.ToUpper())
                {
                    case "RUN":
                        border.Background = (SolidColorBrush)(new BrushConverter().ConvertFrom("#4CAF50")); // xanh lá dịu, đậm vừa
                        break;
                    case "STOP":
                        border.Background = (SolidColorBrush)(new BrushConverter().ConvertFrom("#E57373")); // đỏ nhạt nhưng rõ
                        break;
                    case "ERROR":
                        border.Background = (SolidColorBrush)(new BrushConverter().ConvertFrom("#FFD54F")); // vàng cam dịu
                        break;
                    default:
                        border.Background = Brushes.LightGray;
                        break;
                }
                // Load MachineInfo when panel is clicked
                border.MouseLeftButtonUp += (s, e) =>
                {
                    selectedMachineID = m.MachineID;
                    LoadMachineInfo(m.MachineID);
                };

                // Expander on name
                switch (m.GroupName)
                {
                    case "Gia công":
                        wrapPanelGiaCong.Children.Add(border);
                        break;
                    case "Pallet":
                        wrapPanelPallet.Children.Add(border);
                        break;
                    case "Kết cấu":
                        wrapPanelKetCau.Children.Add(border);
                        break;
                    case "Máy gián tiếp":
                        wrapPanelMayGianTiep.Children.Add(border);
                        break;
                }
            }
        }
        // Load MachineInfo from DAO SQL
        private void LoadMachineInfo(int machineID)
        {
            var info = dao.GetMachineInfo(machineID);
            txtManufacturer.Text = info.Manufacturer;
            txtStartDate.Text = info.StartDate?.ToString("yyyy-MM-dd") ?? "";
            txtLastMaintenance.Text = info.LastMaintenance?.ToString("yyyy-MM-dd") ?? "";
            txtContactPerson.Text = info.ContactPerson;
            txtErrorCodes.Text = info.ErrorCodes;
        }
        // Button Save
        private void btnSave_Click(object sender, RoutedEventArgs e)
        {   
            // If no Panel selected
            if (selectedMachineID == -1)
            {
                MessageBox.Show("Vui lòng chọn máy trước khi lưu.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var info = new MachineInfo
                {
                    Manufacturer = txtManufacturer.Text,
                    StartDate = DateTime.TryParse(txtStartDate.Text, out var dt) ? dt : (DateTime?)null,
                    ContactPerson = txtContactPerson.Text
                };
                // Update from InfoDAO
                dao.UpdateMachineInfo(selectedMachineID, info);

                MessageBox.Show("Đã lưu thông tin thành công!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);

                // Reload to update
                LoadMachineInfo(selectedMachineID);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khi lưu: " + ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
