using System.Windows;

namespace MyOS
{
    /// <summary>
    /// Логика взаимодействия для FileCreationWindow.xaml
    /// </summary>
    public partial class FileCreationWindow
    {
        public FileCreationWindow(string title)
        {
            InitializeComponent();
            Title = title;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if(FileName.Text != string.Empty)
                Close();
        }
    }
}
