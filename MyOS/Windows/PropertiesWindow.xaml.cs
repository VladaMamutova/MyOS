using System.Windows;

namespace MyOS
{
    /// <summary>
    /// Логика взаимодействия для PropertiesWindow.xaml
    /// </summary>
    public partial class PropertiesWindow
    {
        public PropertiesWindow(MftHeader mftHeader)
        {
            InitializeComponent();

            if(mftHeader == null) return;
            
            FileName.Text = mftHeader.GetFullName();
            Type.Text = mftHeader.IsDirectory() ? "Папка с файлами" : mftHeader.Extension == "" ? "Файл" : mftHeader.Extension;
            CreationDateTime.Text = mftHeader.CreationDate.ToString();
            ModificationDateTime.Text = mftHeader.CreationDate.ToString();
            Size.Text = mftHeader.Size + " Б";
            Hidden.IsChecked = mftHeader.IsHidden();
            ReadOnly.IsChecked = mftHeader.IsReadOnly();
            mftFileHeader = mftHeader;

            _newPermissions = new Permission(new byte[2]);
            Administrator.IsSelected = true;
            if (mftHeader.UserId == User.AdministratorId) OwnerName.Visibility = Visibility.Collapsed;
            else OwnerName.Content = SystemCalls.GetUserById(mftHeader.UserId).Name;
            Permissions.IsEnabled = mftHeader.UserId == Account.User.Id || mftHeader.UserId == User.AdministratorId;
        }

        private MftHeader mftFileHeader;
        private Permission _newPermissions;

        private void UpdatePermissions(Permission.UserSign userSign, Permission permission)
        {
            FullControl.IsChecked = permission.CheckRights(userSign, Permission.Rights.F);
            Modify.IsChecked = permission.CheckRights(userSign, Permission.Rights.M);
            Write.IsChecked = permission.CheckRights(userSign, Permission.Rights.W);
            Read.IsChecked = permission.CheckRights(userSign, Permission.Rights.R);
        }

        private void Selected_Administrator(object sender, RoutedEventArgs e)
        {
            UpdatePermissions(Permission.UserSign.Administrator, mftFileHeader.Permission);
        }

        private void Selected_Owner(object sender, RoutedEventArgs e)
        {
            UpdatePermissions(Permission.UserSign.Owner, mftFileHeader.Permission);
        }

        private void Selected_Other(object sender, RoutedEventArgs e)
        {
            UpdatePermissions(Permission.UserSign.Other, mftFileHeader.Permission);
        }
    }
}
