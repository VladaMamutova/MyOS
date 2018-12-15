using System.Windows;

namespace MyOS
{
    /// <summary>
    /// Логика взаимодействия для ControlWindow.xaml
    /// </summary>
    public partial class ControlWindow
    {
        public ControlWindow()
        {
            InitializeComponent();
        }

        private void Format_Click(object sender, RoutedEventArgs e)
        {
            SystemCalls.Formatting();
            ((MainWindow)Owner).CurrentDirectory.DataContext = new Path();
            ((MainWindow)Owner).CurrentDirectory.Content = ((Path)((MainWindow)Owner).CurrentDirectory.DataContext).CurrentPath;
            Bitmap.InitializeFreeClusters();
            ((MainWindow)Owner).UpdateFileTable();
        }
    }
}
