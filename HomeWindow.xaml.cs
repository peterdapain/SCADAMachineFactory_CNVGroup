using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace GiamSatNhaMay
{
    public partial class HomeWindow : Window
    {
        private DatabaseDAO dao;
        private StatisticsService statsSvc;
        public HomeWindow()
        {
            InitializeComponent();
            // Initialize DAO
            // SQL
            dao = new DatabaseDAO("Data Source=VUHUUDUC;Initial Catalog=QuanLyNhaMay;Integrated Security=True;TrustServerCertificate=True");
            // Statistic machine
            statsSvc = new StatisticsService(dao);
            // Load Group to WrapPanel
            LoadGroupToPanel("Gia công", wrapPanelGiaCong);
            LoadGroupToPanel("Pallet", wrapPanelPallet);
            LoadGroupToPanel("Kết cấu", wrapPanelKetCau);
            LoadGroupToPanel("Máy gián tiếp", wrapPanelMayGianTiep);
        }
        // Load Group to Panel code
        private void LoadGroupToPanel(string groupName, WrapPanel wrapPanel)
        {
            wrapPanel.Children.Clear();
            // Call GetMachineByGroup from DatabaseDAO
            List<MachineModel> machines = dao.GetMachinesByGroup(groupName);

            foreach (var m in machines)
            {   

                Border border = new Border
                {
                    BorderBrush = Brushes.Gray,
                    BorderThickness = new Thickness(1),
                    Padding = new Thickness(10),
                    Margin = new Thickness(5),
                    Width = 200,
                    Height = 120,
                    CornerRadius = new CornerRadius(8),
                    Background = Brushes.White
                };
                // 
                StackPanel sp = new StackPanel();
                sp.Children.Add(new TextBlock { Text = m.MachineName, FontWeight = FontWeights.Bold, TextAlignment = TextAlignment.Center });
                sp.Children.Add(new TextBlock { Text = $"Status: {m.Status}", FontWeight = FontWeights.Bold });
                sp.Children.Add(new TextBlock { Text = $"Connection: {m.ConnectionSTT} ", FontWeight = FontWeights.Regular });
                sp.Children.Add(new TextBlock { Text = $"Type: {m.GroupName}", FontWeight = FontWeights.Regular });
                border.Child = sp;
                // change color on state
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
                // Popup window click event
                border.MouseLeftButtonUp += (s, e) =>
                {
                    int machineID = m.MachineID;
                    var popup = new PopupWindow(machineID);  
                    popup.Owner = this;
                    popup.ShowDialog();

                };

                wrapPanel.Children.Add(border);
            }
        }
       
        // Info menu
        private void btnInfo_Click(object sender, RoutedEventArgs e)
        {
            InfoWindow f1 = new InfoWindow();
            
            f1.Show();
            
        }
        // Chart menu
        private void btnChart_Click(object sender, RoutedEventArgs e)
        {
            ChartWindow f2 = new ChartWindow(dao);
            f2.Show();
        }
        // Data menu
        private void btnDataGrid_Click(object sender, RoutedEventArgs e)
        {
            DataGridWindow f3 = new DataGridWindow(dao, statsSvc);
            f3.Show();
        }
    }
}
