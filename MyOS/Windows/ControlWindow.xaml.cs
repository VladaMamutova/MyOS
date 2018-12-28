using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using MyOS.FileSystem;
using MyOS.FileSystem.SpecialDataTypes;
using MyOS.ViewModels;
using Application = System.Windows.Application;
using MessageBox = System.Windows.MessageBox;

namespace MyOS
{
    /// <summary>
    /// Логика взаимодействия для ControlWindow.xaml
    /// </summary>
    public partial class ControlWindow
    {
        private readonly bool _isSuperRoot;
        public ControlWindow(bool isSuperRoot)
        {
            InitializeComponent();
            _isSuperRoot = isSuperRoot;
            if (!_isSuperRoot)
            {
                AccountsItem.Visibility = Visibility.Collapsed;
                MftItem.Visibility = Visibility.Collapsed;
                BitmapItem.Visibility = Visibility.Collapsed;
                FormattingItem.Visibility = Visibility.Collapsed;
                ResizeMode = ResizeMode.NoResize;
            }
            else MinWidth = 800;
        }

        private void ControlWindow_OnActivated(object sender, EventArgs e)
        {
            UpdateFileSystemInfo();
            if (!_isSuperRoot) return;
            
            UpdateUserTable();
            UpdateMft();
            UpdateBitmapData();
        }

        private void UpdateFileSystemInfo()
        {
            FsName.Text = FileSystem.FileSystem.FileSystemVersion;
            VolumeName.Text = FileSystem.FileSystem.VolumeName.ToString();
            VolumeState.Text = FileSystem.FileSystem.State == 0 ? "Исправен" : "Повреждён";
            VolumeSize.Text = FileSystem.FileSystem.VolumeSize.ToString("N0") + " Б";
            int busySpace = FileSystem.FileSystem.GetBusySpace();
            BusySpace.Text = busySpace.ToString("N0") + " Б";
            FreeSpace.Text = (FileSystem.FileSystem.VolumeSize - busySpace).ToString("N0") + " Б";
        }

        private void UpdateMft()
        {
            List<MftHeader> allMftHeaders = SystemCalls.GetAllMftHeaders();
            MftHeaders.ItemsSource = allMftHeaders;
            MftZoneSize.Text = FileSystem.FileSystem.MftAreaSize.ToString("N0") + " Б";
            MftSize.Text =
            (allMftHeaders.Count(mftHeader => mftHeader.Sign == MftHeader.Signature.InUse) *
             FileSystem.FileSystem.Constants.MftRecordSize).ToString("N0") + " Б";
            MftHeaders.SelectedIndex = 0;
        }

        private void UpdateBitmapData()
        {
            ClustersCount.Text = FileSystem.FileSystem.GetClusterCount().ToString("N0");
            ServiceClusters.Text = FileSystem.FileSystem.ServiceClusters.ToString("N0");
            BusyClusters.Text = FileSystem.FileSystem.GetBusyClusterCount().ToString("N0");
            ClusterSize.Text = FileSystem.FileSystem.BytesPerCluster.ToString("N0") + " Б";
            GetBitmapData();
        }

        public void GetBitmapData()
        {
            byte[] bytes = SystemCalls.ReadFileData(FileSystem.FileSystem.Constants.BitmapMftEntry);
            BitmapSize.Text = bytes.Length.ToString("N0") + " Б";
            List<BitmapRow> grid = new List<BitmapRow>();
            for (int i = 0; i < bytes.Length;)
            {
                BitmapRow row = new BitmapRow();
                row.SetRowNumber(i * 4);
                for (int j = 0; j < 10 && i < bytes.Length; j++)
                    row.Add(j, Convert.ToString(bytes[i++], 2).PadLeft(8, '0'));
                grid.Add(row);
            }

            BitmapDataGrid.ItemsSource = grid;
        }

        private void Format_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Форматирование приведёт к безвозвратной потере всех данных," +
            " в том числе всех учётных записей пользователей." + Environment.NewLine + "Продолжить?",
                    "Форматирование", MessageBoxButton.YesNo, MessageBoxImage.Information) == MessageBoxResult.Yes)
            {
                // Размер тома - // 1/2/4/8 * 100 (МБ) * 1024 (КБ) * 1024 Б.
                int volumeSize = (1 << VolumeSizeComboBox.SelectedIndex) * 100 * 1024 * 1024;
                int bytesPerCluster = (1 << BytesPerClusterComboBox.SelectedIndex) * 1024;
                string fsName = FsName.Text;
                string volumeName = ((ComboBoxItem) VolumeNameComboBox.SelectedItem).Content.ToString();

                FormattingWindow formattingWindow = new FormattingWindow(new FormattingOptions(volumeSize, bytesPerCluster, fsName, volumeName));
                formattingWindow.ShowDialog();
                System.Diagnostics.Process.Start(Application.ResourceAssembly.Location);
                Application.Current.Shutdown();
            }
        }

        public void UpdateUserTable()
        {
            List<UserAccount> userAccounts = new List<UserAccount>();
            var allUsers = SystemCalls.GetAllRecords<UserRecord>(FileSystem.FileSystem.Constants.UserListMftEntry);
            foreach (var user in allUsers)
                userAccounts.Add(new UserAccount(user));

            UserTable.ItemsSource = userAccounts;
        }

        private void AddUser(object sender, RoutedEventArgs e)
        {
            LoginWindow loginWindow = new LoginWindow(true);
            if (loginWindow.ShowDialog() == true)
                UpdateUserTable();
        }

        private void DeleteUser(object sender, RoutedEventArgs e)
        {
            if (UserTable.CurrentCell.Item is DataGridCellInfo ||
                !(UserTable.SelectedItem is UserAccount user)) return;

            if (user.Id == 0) MessageBox.Show("Удалить администратора нельзя!", "Удаление пользователя",
                MessageBoxButton.OK, MessageBoxImage.Error);
            else if (user.Id == FileSystem.FileSystem.CurrentUser.Id)
            {
                if (MessageBox.Show(
                        "Вы пытаетесь удалить свою текущую учётную запись. " +
                        "При завершении данной операции будет осуществлён выход из системы. Продолжить?",
                        "Удаление учётной записи пользователя", MessageBoxButton.YesNo, MessageBoxImage.Question) ==
                    MessageBoxResult.Yes)
                {
                    DialogResult = true;
                }
            }
            else
            {
                SystemCalls.UpdateRecord(FileSystem.FileSystem.Constants.UserListMftEntry, SystemCalls.GetUserById(user.Id),
                    UserRecord.Empty);
                SystemCalls.ChangeOwnerToAdministrator(user.Id);
                UpdateUserTable();
            }
        }
        
        private void MftHeaders_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            MftEnty.Text = MftHeaders.SelectedIndex.ToString();
        }
    }
}
