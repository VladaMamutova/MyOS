using System.IO;
using System.Windows;

namespace MyOS
{
    /// <summary>
    /// Логика взаимодействия для App.xaml
    /// </summary>
    public partial class App
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            MainWindow = new MainWindow();

            if (!File.Exists(SystemConstants.SystemFile))
                SystemCalls.Formatting();

            LoginWindow loginWindow = new LoginWindow();
            if (loginWindow.ShowDialog() == true)
                MainWindow.ShowDialog();
            else MainWindow.Close();
        }
    }
}
