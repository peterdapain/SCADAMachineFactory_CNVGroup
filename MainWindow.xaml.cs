using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
namespace GiamSatNhaMay;

public partial class MainWindow : Window
{
    private DatabaseDAO db;
    public MainWindow()
    {
        InitializeComponent();
        db = new DatabaseDAO("Data Source=VUHUUDUC;Initial Catalog=QuanLyNhaMay;Integrated Security=True;TrustServerCertificate=True");
        // Placeholder effect for Username
        txtUsername.TextChanged += (s, e) =>
        {
            placeholderUsername.Visibility = string.IsNullOrEmpty(txtUsername.Text)
                                             ? Visibility.Visible
                                             : Visibility.Collapsed;
        };

        // Placeholder effect for Password
        txtPassword.PasswordChanged += (s, e) =>
        {
            placeholderPassword.Visibility = string.IsNullOrEmpty(txtPassword.Password)
                                             ? Visibility.Visible
                                             : Visibility.Collapsed;
        };

    }
    // Button login
    private void BtnLogin_Click(object sender, RoutedEventArgs e)
    {
        string username = txtUsername.Text;
        string password = txtPassword.Password;

        if (db.CheckLogin(username, password))
        {
            this.Hide();
            HomeWindow f = new HomeWindow();
            f.ShowDialog();
            this.Show();        

        }
        else
        {
            MessageBox.Show("Sai Username hoặc Password!", "Login Failed", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }
    // button Exit
    private void BtnExit_Click(object sender, RoutedEventArgs e)
    {
        this.Close();
    }
    // Window Closing
    private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {

        MessageBoxResult result = MessageBox.Show(
            "Thoát phần mềm?",
            "Xác nhận",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result == MessageBoxResult.No)
        {
            e.Cancel = true;
        }
    }
}