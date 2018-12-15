using System;
using System.Linq;
using System.Windows;
using MessageBox = System.Windows.MessageBox;

namespace MyOS
{
    /// <summary>
    /// Логика взаимодействия для PropertiesWindow.xaml
    /// </summary>
    public partial class PropertiesWindow
    {
        public PropertiesWindow(int mftEntry)
        {
            InitializeComponent();
            if(mftEntry < 0) throw new ArgumentException(nameof(mftEntry));

            _mftHeader = SystemCalls.GetMftHeader(mftEntry);
            _mftFileEntry = mftEntry;
            _previousPermissions = new Permission(_mftHeader.Permission.GetBytes());
            _previousAttributes = _mftHeader.Attributes;
        }

        private readonly MftHeader _mftHeader;
        private readonly int _mftFileEntry;
        private readonly Permission _previousPermissions;
        private readonly byte _previousAttributes;

        private void PropertiesWindow_Loaded(object sender, RoutedEventArgs e)
        {
            FileName.Text = _mftHeader.GetFullName();
            Type.Text = _mftHeader.HasAttribute(MftHeader.Attribute.Directory) ? "Папка с файлами" : _mftHeader.Extension == "" ? "Файл" : _mftHeader.Extension;
            CreationDateTime.Text = _mftHeader.CreationDate.ToString();
            ModificationDateTime.Text = _mftHeader.CreationDate.ToString();
            Size.Text = _mftHeader.Size + " Б";
            Hidden.IsChecked = _mftHeader.HasAttribute(MftHeader.Attribute.Hidden);
            ReadOnly.IsChecked = _mftHeader.HasAttribute(MftHeader.Attribute.ReadOnly);
            if (_mftHeader.UserId == User.AdministratorId) OwnerName.Visibility = Visibility.Collapsed;
            else OwnerName.Content = SystemCalls.GetUserById(_mftHeader.UserId).Name;

            Administrator.IsSelected = true;
            UpdatePermissions();

            Permissions.IsEnabled = _mftHeader.HasPermissions(Account.User.Id, Permission.Rights.F) || Account.User.Id == User.AdministratorId;
            ReadOnly.IsEnabled = Hidden.IsEnabled = _mftHeader.HasPermissions(Account.User.Id, Permission.Rights.W);
        }

        private Permission.UserSign GetCurrentUserSign()
        {
            return Administrator.IsSelected ? Permission.UserSign.Administrator :
                OwnerName.IsSelected ? Permission.UserSign.Owner : Permission.UserSign.Other;
        }

        private void UpdatePermissions()
        {
            Permission.UserSign userSign = GetCurrentUserSign();
            
            FullControl.IsChecked = _mftHeader.Permission.CheckRights(userSign, Permission.Rights.F);
            Modify.IsChecked = _mftHeader.Permission.CheckRights(userSign, Permission.Rights.M);
            Write.IsChecked = _mftHeader.Permission.CheckRights(userSign, Permission.Rights.W);
            Read.IsChecked = _mftHeader.Permission.CheckRights(userSign, Permission.Rights.R);
        }

        private void Selected_Administrator(object sender, RoutedEventArgs e)
        {
            UpdatePermissions();
        }

        private void Selected_Owner(object sender, RoutedEventArgs e)
        {
            Administrator.IsSelected = false;
            UpdatePermissions();
        }

        private void Selected_Other(object sender, RoutedEventArgs e)
        {
            Administrator.IsSelected = false;
            UpdatePermissions();
        }

        private void FullControl_Checked(object sender, RoutedEventArgs e)
        {
            _mftHeader.Permission.SetPermission(GetCurrentUserSign(), Permission.Rights.F, FullControl.IsChecked != null && FullControl.IsChecked.Value);
            UpdatePermissions();
        }

        private void Modify_Checked(object sender, RoutedEventArgs e)
        {
            _mftHeader.Permission.SetPermission(GetCurrentUserSign(), Permission.Rights.M, Modify.IsChecked != null && Modify.IsChecked.Value);
            UpdatePermissions();
        }

        private void Write_Checked(object sender, RoutedEventArgs e)
        {
            _mftHeader.Permission.SetPermission(GetCurrentUserSign(), Permission.Rights.W, Write.IsChecked != null && Write.IsChecked.Value);
            UpdatePermissions();
        }

        private void Read_Checked(object sender, RoutedEventArgs e)
        {
            _mftHeader.Permission.SetPermission(GetCurrentUserSign(), Permission.Rights.R, Read.IsChecked != null && Read.IsChecked.Value);
            UpdatePermissions();
        }

        private void Readonly_CheckChanged(object sender, RoutedEventArgs e)
        {
            _mftHeader.SetAttribute(MftHeader.Attribute.ReadOnly, ReadOnly.IsChecked != null && ReadOnly.IsChecked.Value);
        }

        private void Hidden_CheckChanged(object sender, RoutedEventArgs e)
        {
            _mftHeader.SetAttribute(MftHeader.Attribute.Hidden, Hidden.IsChecked != null && Hidden.IsChecked.Value);
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            if (!_mftHeader.Permission.GetBytes().SequenceEqual(_previousPermissions.GetBytes()) ||
                _mftHeader.Attributes != _previousAttributes)
                SystemCalls.UpdateMftEntry(_mftHeader, _mftFileEntry);
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            if (!_mftHeader.Permission.GetBytes().SequenceEqual(_previousPermissions.GetBytes()) ||
                _mftHeader.Attributes != _previousAttributes)
                if (MessageBox.Show("Сохранить изменения?", "Сохранения свойств объекта",
                        MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                    SystemCalls.UpdateMftEntry(_mftHeader, _mftFileEntry);
            Close();
        }
    }
}
