using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace MyOS
{
    /// <summary>
    /// Логика взаимодействия для LoginWindow.xaml
    /// </summary>
    public partial class LoginWindow
    {
        public LoginWindow(bool isRegistration = false)
        {
            InitializeComponent();
            if(isRegistration) ChangeWindowState();
        }
        private bool _isRegistrationWindow;

        private void ChangeWindowState()
        {
            if (!_isRegistrationWindow)
            {
                SignIn.Content = "Зарегистрироваться";
                UserTypeRow.Visibility = Visibility.Visible;
                NeedNewAccountRow.Visibility = Visibility.Collapsed;
            }
            else
            {
                SignIn.Content = "Войти";
                UserTypeRow.Visibility = Visibility.Collapsed;
                NeedNewAccountRow.Visibility = Visibility.Visible;
            }
            _isRegistrationWindow = !_isRegistrationWindow;
            UserName.Text = Password.Password = "";
        }
        
        private void Register_Click(object sender, RoutedEventArgs e)
        {
            ChangeWindowState();
        }

        private void SignIn_Click(object sender, RoutedEventArgs e)
        {
            if (!_isRegistrationWindow)
            {
                if (Account.SignIn(UserName.Text, Password.Password))
                {
                    DialogResult = true;
                    Close();
                }
                else
                {
                    MessageBox.Show("Пользователь c таким именем и паролем не зарегистрирован в системе!",
                        "Ошибка входа в систему", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                if (Account.SignUp(UserName.Text, Password.Password))
                {
                    UserName.Text = Password.Password = "";
                    NeedNewAccountRow.Visibility = Visibility.Visible;
                    SignIn.Content = "Войти";
                }
                else MessageBox.Show("Пользователь c таким именем уже зарешистрирован в системе!",
                    "Ошибка регистрации в систему", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            if(_isRegistrationWindow) ChangeWindowState();
            else
            {
                DialogResult = false;
                Close();
            }
        }

        private void Move(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }

        private void Register_MouseEnter(object sender, MouseEventArgs e)
        {
            //NeedNewAccount.TextDecorations.Add(TextDecorations.Underline);
            NeedNewAccountRow.Foreground = Brushes.Black;
            Cursor = Cursors.Hand;
        }

        private void Register_MouseLeave(object sender, MouseEventArgs e)
        {
            //foreach (var item in TextDecorations.Underline)
            //{
            //    NeedNewAccount.TextDecorations.Remove(item);
            //}
            NeedNewAccountRow.Foreground = Brushes.Gray;
            Cursor = Cursors.Arrow;
        }
    }
}
