using System.Windows;

namespace MyOS
{
    /// <summary>
    /// Логика взаимодействия для FileCreationWindow.xaml
    /// </summary>
    public partial class FileCreationWindow
    {
        public FileCreationWindow()
        {
            InitializeComponent();
        }

        public void ShowDialog(string title)
        {
            Title = title;
            FileName.Text = "";
            FileName.Focus();
            ShowDialog();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if(FileName.Text != string.Empty)
                Hide();
        }
    }
}
