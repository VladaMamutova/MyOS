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
            AccessControlList accessList = new AccessControlList();
            accessList.Add(1, new[] {AccessControlList.Rights.F, AccessControlList.Rights.M});
            MessageBox.Show(accessList.ToBytes()[0].ToString() + accessList.ToBytes()[1]);
            accessList.Add(1, new[] { AccessControlList.Rights.R, AccessControlList.Rights.W, AccessControlList.Rights.M });
            MessageBox.Show(accessList.ToBytes().ToString());
            accessList.Add(2, new[] { AccessControlList.Rights.R });
            MessageBox.Show(accessList.ToBytes()[0].ToString() + accessList.ToBytes()[1]);
        }

        private void CreateFile_Click(object sender, RoutedEventArgs e)
        {
            
        }
    }
}
