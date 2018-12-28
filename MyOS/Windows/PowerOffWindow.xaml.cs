using System.Windows;

namespace MyOS.Windows
{
    /// <summary>
    /// Логика взаимодействия для AccountSettings.xaml
    /// </summary>
    public partial class AccountSettings
    {
        public AccountSettings()
        {
            InitializeComponent();
        }

        private void LogOff_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show(
                    "Вы точно желаете завершить сеанс?", "Завершение сеанса работы", MessageBoxButton.YesNo, MessageBoxImage.Question) ==
                MessageBoxResult.Yes)
            {
                DialogResult = true;
            }
        }
    }
}
