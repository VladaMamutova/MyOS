using System;
using System.Collections.Generic;
using System.IO;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

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
            
        }
    }
}
