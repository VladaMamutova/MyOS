using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using MyOS.FileSystem;
using MyOS.FileSystem.SpecialDataTypes;

namespace MyOS
{
    /// <summary>
    /// Логика взаимодействия для LoginWindow.xaml
    /// </summary>
    public partial class LoginWindow
    {
        public LoginWindow(bool isRegistration)
        {
            InitializeComponent();
            _isRegistrationWindow = isRegistration;
            SetWindowType();
        }
        private readonly bool _isRegistrationWindow;

        private void SetWindowType()
        {
            if (_isRegistrationWindow)
            {
                SignIn.Content = "Зарегистрировать";
                UserTypeRow.Visibility = Visibility.Visible;
            }
            else
            {
                SignIn.Content = "Войти";
                UserTypeRow.Visibility = Visibility.Collapsed;
            }
        }

        private void SignIn_Click(object sender, RoutedEventArgs e)
        {
            if (!_isRegistrationWindow)
            {
                try
                {
                    SystemCalls.SignIn(UserName.Text, Password.Password);
                    DialogResult = true;
                }
                catch (FsException fsException)
                {
                   fsException.ShowError(FsException.Command.SignIn, FsException.Element.User);
                }
            }
            else
            {
                try
                {
                    SystemCalls.SignUp(UserName.Text, Password.Password,
                        UserTypeRow.IsChecked != null && UserTypeRow.IsChecked.Value);
                    DialogResult = true;
                }
                catch (FsException fsException)
                {
                    fsException.ShowError(FsException.Command.SignUp, FsException.Element.User);
                }
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void Move(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }

        private void TextBox_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            SignIn.IsEnabled = UserName?.Text.TrimEnd() != "" && Password?.Password != "";
        }

        private void Password_OnPasswordChanged(object sender, RoutedEventArgs e)
        {
            SignIn.IsEnabled = UserName?.Text.TrimEnd() != "" && Password?.Password != "";
        }
    }
}
