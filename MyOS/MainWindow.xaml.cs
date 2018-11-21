using System;
using System.Windows;

namespace MyOS
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Create_Click(object sender, RoutedEventArgs e)
        {
            SystemCalls.Formatting();
        }

        private void TestMyDateTime_Click(object sender, RoutedEventArgs e)
        {
            DateTime now = DateTime.Now;
            SystemStructures.MyDateTime myDateTimeNow = new SystemStructures.MyDateTime(now);
            MessageBox.Show("DateTime.Now: " + now + Environment.NewLine +
                            "MyDateTime: " + myDateTimeNow + " (" + myDateTimeNow.GetStringUtcHours() + ")");
        }

        private void CreateFile_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(SystemCalls.HasFreeMemory().ToString());
            //SystemCalls.CreateFile();
        }
    }
}
