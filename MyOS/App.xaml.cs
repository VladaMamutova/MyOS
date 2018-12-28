using System.IO;
using System.Windows;
using MyOS.FileSystem.SpecialDataTypes;

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

            if (!File.Exists(FileSystem.FileSystem.Constants.SystemFile))
            {
                // Если файл тома не найден, то форматируем его с параметрами по умолчанию.
                FormattingWindow formattingWindow = new FormattingWindow(new FormattingOptions(419430400, 4096, "VMFS v2.0", "C"));
                formattingWindow.ShowDialog();
            }

            LoginWindow loginWindow = new LoginWindow(false);
            if (loginWindow.ShowDialog() == true)
                MainWindow.Show();
            else MainWindow.Close();
        }
    }
}
