using System;
using System.Linq;
using System.Windows;
using MyOS.FileSystem;
using MyOS.FileSystem.SpecialDataTypes;

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
            _previousPermissions = new Permission(_mftHeader.Permissions.GetBytes());
            _previousAttributes = _mftHeader.Attributes;
        }

        private readonly MftHeader _mftHeader;
        private readonly int _mftFileEntry;
        private readonly Permission _previousPermissions;
        private readonly byte _previousAttributes;
        private Permission.UserSign _currentUserSign;

        private void PropertiesWindow_Loaded(object sender, RoutedEventArgs e)
        {
            FileName.Text = _mftHeader.GetFullName();
            bool isDirectory = _mftHeader.HasAttribute(MftHeader.Attribute.Directory);
            Type.Text = isDirectory ? "Папка с файлами" :
                _mftHeader.Extension == "" ? "Файл" : _mftHeader.Extension;
            CreationDateTime.Text = _mftHeader.CreationDate.ToString();
            ModificationDateTime.Text = _mftHeader.CreationDate.ToString();
            Size.Text = (isDirectory ? SystemCalls.GetDirectorySize(_mftFileEntry) : _mftHeader.Size).ToString("N0") + " Б";
            Hidden.IsChecked = _mftHeader.HasAttribute(MftHeader.Attribute.Hidden);
            ReadOnly.IsChecked = _mftHeader.HasAttribute(MftHeader.Attribute.ReadOnly);
            OwnerName.Content = SystemCalls.GetUserById(_mftHeader.UserId).Name;

            _currentUserSign = _mftHeader.UserId == FileSystem.FileSystem.CurrentUser.Id ? Permission.UserSign.Owner :
                FileSystem.FileSystem.CurrentUser.IsAdministrator ? Permission.UserSign.Administrator : Permission.UserSign.Other;
            
            switch (_currentUserSign)
            {
                case Permission.UserSign.Administrator: UserList.SelectedItem = Administrator;
                    break;
                case Permission.UserSign.Owner:
                    UserList.SelectedItem = OwnerName;
                    break;
                case Permission.UserSign.Other:
                    UserList.SelectedItem = AllUsers;
                    break;
            }
            

            Permissions.IsEnabled = _mftHeader.HasPermissions(FileSystem.FileSystem.CurrentUser, Permission.Rights.FullControl) ||
                                    FileSystem.FileSystem.CurrentUser.IsAdministrator;
            ReadOnly.IsEnabled = Hidden.IsEnabled =
                _mftHeader.HasPermissions(FileSystem.FileSystem.CurrentUser, Permission.Rights.Write);
        }
        
        private void UpdatePermissions()
        {
            FullControl.IsChecked = _mftHeader.Permissions.CheckRights(_currentUserSign, Permission.Rights.FullControl);
            Modify.IsChecked = _mftHeader.Permissions.CheckRights(_currentUserSign, Permission.Rights.Modify);
            Write.IsChecked = _mftHeader.Permissions.CheckRights(_currentUserSign, Permission.Rights.Write);
            Read.IsChecked = _mftHeader.Permissions.CheckRights(_currentUserSign, Permission.Rights.Read);
        }

        private void Selected_Administrator(object sender, RoutedEventArgs e)
        {
            _currentUserSign = Permission.UserSign.Administrator;
            UpdatePermissions();
        }

        private void Selected_Owner(object sender, RoutedEventArgs e)
        {
            _currentUserSign = Permission.UserSign.Owner;
            UpdatePermissions();
        }

        private void Selected_Other(object sender, RoutedEventArgs e)
        {
            _currentUserSign = Permission.UserSign.Other;
            UpdatePermissions();
        }

        private void ChangePermission(Permission.Rights right, bool state)
        {
            _mftHeader.Permissions.SetPermission(_currentUserSign, right, state);
            UpdatePermissions();
        }

        private void FullControl_Checked(object sender, RoutedEventArgs e)
        {
            ChangePermission(Permission.Rights.FullControl, FullControl.IsChecked != null && FullControl.IsChecked.Value);
        }

        private void Modify_Checked(object sender, RoutedEventArgs e)
        {
            ChangePermission(Permission.Rights.Modify,
                Modify.IsChecked != null && Modify.IsChecked.Value);
        }

        private void Write_Checked(object sender, RoutedEventArgs e)
        {
            ChangePermission(Permission.Rights.Write,
                Write.IsChecked != null && Write.IsChecked.Value);
        }

        private void Read_Checked(object sender, RoutedEventArgs e)
        {
            ChangePermission(Permission.Rights.Read, Read.IsChecked != null && Read.IsChecked.Value);
        }

        private void Readonly_CheckChanged(object sender, RoutedEventArgs e)
        {
            _mftHeader.SetAttribute(MftHeader.Attribute.ReadOnly,
                ReadOnly.IsChecked != null && ReadOnly.IsChecked.Value);
        }

        private void Hidden_CheckChanged(object sender, RoutedEventArgs e)
        {
            _mftHeader.SetAttribute(MftHeader.Attribute.Hidden, Hidden.IsChecked != null && Hidden.IsChecked.Value);
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            if (!_mftHeader.Permissions.GetBytes().SequenceEqual(_previousPermissions.GetBytes()) ||
                _mftHeader.Attributes != _previousAttributes)
                SystemCalls.UpdateMftEntry(_mftHeader, _mftFileEntry);
            DialogResult = true;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            if (!_mftHeader.Permissions.GetBytes().SequenceEqual(_previousPermissions.GetBytes()) ||
                _mftHeader.Attributes != _previousAttributes)
                if (MessageBox.Show("Сохранить изменения?", "Сохранения свойств объекта",
                        MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    SystemCalls.UpdateMftEntry(_mftHeader, _mftFileEntry);
                    DialogResult = true;
                }

            Close();
        }
    }
}
