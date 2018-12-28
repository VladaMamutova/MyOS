using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using MyOS.FileSystem;
using MyOS.FileSystem.SpecialDataTypes;
using MyOS.ViewModels;

namespace MyOS
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        public void UpdateFileTable()
        {
            List<ExplorerFile> fileList = new List<ExplorerFile>();
            var allFiles = SystemCalls.GetFileList(FileSystem.FileSystem.CurrentPath);
            if (ShowHidden.IsChecked != null && ShowHidden.IsChecked.Value)
                fileList = allFiles;
            else
            {
                foreach (var file in allFiles)
                    if (!file.IsHidden)
                        fileList.Add(file);
            }

            FileTable.ItemsSource = fileList;
            FileCount.Text = FileTable.Items.Count.ToString();
        }

        private void ShowHidden_Checked(object sender, RoutedEventArgs e)
        {
            FileTable.ItemsSource = SystemCalls.GetFileList((Path) CurrentDirectory.DataContext);
            FileCount.Text = FileTable.Items.Count.ToString();
        }

        private void ShowHidden_Unchecked(object sender, RoutedEventArgs e)
        {
            List<ExplorerFile> fileList = new List<ExplorerFile>();
            var allFiles = SystemCalls.GetFileList((Path) CurrentDirectory.DataContext);
            foreach (var file in allFiles)
                if (!file.IsHidden)
                    fileList.Add(file);

            FileCount.Text = fileList.Count.ToString();
            FileTable.ItemsSource = fileList;
        }

        private void Create100Files(object sender, RoutedEventArgs e)
        {
            for (int i = 0; i < 100; i++)
            {
                try
                {
                    SystemCalls.Create((Path)CurrentDirectory.DataContext, "file" + (i + 1), "txt");
                }
                catch (FsException fs)
                {
                    fs.ShowError(FsException.Command.Create, FsException.Element.File);
                }
            }
            UpdateFileTable();
        }
        private void Create100Folders(object sender, RoutedEventArgs e)
        {
            for (int i = 0; i < 100; i++)
            {
                try
                {
                    SystemCalls.Create((Path) CurrentDirectory.DataContext, "folder" + (i + 1), "",
                        MftHeader.Attribute.Directory);
                }
                catch (FsException fs)
                {
                    fs.ShowError(FsException.Command.Create, FsException.Element.Folder);
                }
            }
            UpdateFileTable();
        }

        private void CreateFile_Click(object sender, RoutedEventArgs e)
        {
            NameWindow name =
                new NameWindow(FsException.Element.File, (Path) CurrentDirectory.DataContext);
            name.ShowDialog();
        }

        private void CreateFolder_Click(object sender, RoutedEventArgs e)
        {
            NameWindow name =
                new NameWindow(FsException.Element.Folder, (Path) CurrentDirectory.DataContext);
            name.ShowDialog();
        }

       private void FileTable_OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (FileTable.CurrentCell.Item is DataGridCellInfo ||
                !(FileTable.SelectedItem is ExplorerFile file)) return;

            if (file.Attributes == (file.Attributes | (byte) MftHeader.Attribute.Directory))
            {
                if (!SystemCalls.GetMftHeader(file.MftEntry).HasPermissions(FileSystem.FileSystem.CurrentUser, Permission.Rights.Read))
                {
                    new FsException(FsException.Code.NoReadPermission, file.FullName).ShowError(
                        FsException.Command.Open, FsException.Element.Folder);
                    return;
                }
                ((Path) CurrentDirectory.DataContext).Add(file.FullName);
                CurrentDirectory.Text = ((Path) CurrentDirectory.DataContext).CurrentPath;
                UpdateFileTable();
            }
            else EditFile();
        }

        private void PreviousDirectory_Click(object sender, RoutedEventArgs e)
        {
            Path path = (Path) CurrentDirectory.DataContext;
            path.GetPreviosFolder();
            CurrentDirectory.DataContext = path;
            CurrentDirectory.Text = ((Path) CurrentDirectory.DataContext).CurrentPath;
            UpdateFileTable();
        }

        private void Copy_Click(object sender, RoutedEventArgs e)
        {
            if (FileTable.CurrentCell.Item is DataGridCellInfo ||
                !(FileTable.SelectedItem is ExplorerFile file)) return;

            try
            {
                SystemCalls.Copy((Path) CurrentDirectory.DataContext, new DirectoryRecord(file));
            }
            catch (FsException fsException)
            {
                fsException.ShowError(FsException.Command.Copy,
                    file.IsDirectory ? FsException.Element.Folder : FsException.Element.File);
            }
        }

        private void Paste_Click(object sender, RoutedEventArgs e)
        {
            if (FileSystem.FileSystem.Buffer == null)
            {
                MessageBox.Show("Буфер обмена пуст!");
                return;
            }

            try
            {
                SystemCalls.Paste((Path) CurrentDirectory.DataContext);
            }
            catch (FsException fsException)
            {
                fsException.ShowError(FsException.Command.Copy,
                    FileSystem.FileSystem.Buffer.Record.HasAttribute(MftHeader.Attribute.Directory)
                        ? FsException.Element.Folder
                        : FsException.Element.File);
            }

            UpdateFileTable();
        }

        private void EditFile()
        {
            if (FileTable.CurrentCell.Item is DataGridCellInfo ||
                !(FileTable.SelectedItem is ExplorerFile file)) return;

            if (!SystemCalls.GetMftHeader(file.MftEntry).HasPermissions(FileSystem.FileSystem.CurrentUser, Permission.Rights.Read))
            {
                new FsException(FsException.Code.NoReadPermission, file.FullName).ShowError(FsException.Command.Open,
                    FsException.Element.File);
                return;
            }

            string fullName = ((Path)CurrentDirectory.DataContext).GetAbsolutePath(file.FullName); 
            foreach (Window window in Application.Current.Windows)
            {
                if (window is TextEditorWindow && window.Title == fullName)
                {
                    window.Activate();
                    return;
                }
            }

            TextEditorWindow editWindow = new TextEditorWindow(file, fullName)
                {Owner = this};
            editWindow.Show();
        }

        private void Rename_Click(object sender, RoutedEventArgs e)
        {
            if (FileTable.CurrentCell.Item is DataGridCellInfo ||
                !(FileTable.SelectedItem is ExplorerFile file)) return;

            NameWindow name =
                new NameWindow(file, (Path) CurrentDirectory.DataContext);
            name.ShowDialog();
        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            if (FileTable.CurrentCell.Item is DataGridCellInfo ||
                !(FileTable.SelectedItem is ExplorerFile file)) return;
            try
            {
                SystemCalls.Delete((Path) CurrentDirectory.DataContext, new DirectoryRecord(file));
            }
            catch (FsException fsException)
            {
                fsException.ShowError(FsException.Command.Delete,
                    file.IsDirectory ? FsException.Element.Folder : FsException.Element.File);
            }

            UpdateFileTable();
        }

        private void MainWindow_Activated(object sender, EventArgs e)
        {
            if ((Path)CurrentDirectory.DataContext != null)
                UpdateFileTable();
        }

        private void MainWindow_OnLoaded(object sender, RoutedEventArgs e)
        {
            FileSystem.FileSystem.CurrentPath = new Path();
            User.Text = FileSystem.FileSystem.CurrentUser.Name;
            if (FileSystem.FileSystem.CurrentUser.HomeDirectoryMftEntry != FileSystem.FileSystem.Constants.RootMftEntry)
                FileSystem.FileSystem.CurrentPath.Add(SystemCalls.GetMftHeader(FileSystem.FileSystem.CurrentUser.HomeDirectoryMftEntry).FileName);
            CurrentDirectory.DataContext = FileSystem.FileSystem.CurrentPath;
            CurrentDirectory.Text = FileSystem.FileSystem.CurrentPath.CurrentPath;
            Time.Text = new MyDateTime(DateTime.Now).ToString();
            UpdateFileTable();

            var timer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = new TimeSpan(0, 1, 0),
                IsEnabled = true
            };
            timer.Tick += (o, t) => { Time.Text = new MyDateTime(DateTime.Now).ToString(); };
            timer.Start();

            CreationDateColumn.IsChecked = true;
            ModificationDateColumn.IsChecked = true;
            SizeColumn.IsChecked = true;
            ShowHidden.IsChecked = false;
        }

        private void CreationDateColumn_CheckChanged(object sender, RoutedEventArgs e)
        {
            FileTable.Columns[2].Visibility = CreationDateColumn.IsChecked != null && CreationDateColumn.IsChecked.Value
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        private void ModificationDateColumn_CheckChanged(object sender, RoutedEventArgs e)
        {
            FileTable.Columns[3].Visibility =
                ModificationDateColumn.IsChecked != null && ModificationDateColumn.IsChecked.Value
                    ? Visibility.Visible
                    : Visibility.Collapsed;
        }

        private void SizeColumn_CheckChanged(object sender, RoutedEventArgs e)
        {
            FileTable.Columns[4].Visibility = SizeColumn.IsChecked != null && SizeColumn.IsChecked.Value
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        private void User_MouseEnter(object sender, MouseEventArgs e)
        {
            User.Foreground = Brushes.White;
        }

        private void User_MouseLeave(object sender, MouseEventArgs e)
        {
            User.Foreground = Brushes.LightGray;
        }

        private void ShowInfo_Click(object sender, RoutedEventArgs e)
        {
            if (FileTable.CurrentCell.Item is DataGridCellInfo ||
                !(FileTable.SelectedItem is ExplorerFile file)) return;

            PropertiesWindow propertiesWindow = new PropertiesWindow(file.MftEntry);
            propertiesWindow.ShowDialog();
            UpdateFileTable();
        }

        private void ShowControlWindow(object sender, RoutedEventArgs e)
        {
            foreach (Window window in Application.Current.Windows)
            {
                if (window is ControlWindow)
                {
                    window.Activate();
                    return;
                }
            }

            ControlWindow controlWindow = new ControlWindow(FileSystem.FileSystem.CurrentUser.Id == 0) {Owner = this};
            controlWindow.Show();
        }

        private void User_MouseLeftButtonDown(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Вы точно хотите выйти из учётной записи?", "Выход из системы", MessageBoxButton.YesNo,
                    MessageBoxImage.None) == MessageBoxResult.Yes)
            {
                System.Diagnostics.Process.Start(Application.ResourceAssembly.Location);
                Application.Current.Shutdown();
            }
        }

        private void PowerOff_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Вы точно хотите завершить сеанс?", "Завершение работы", MessageBoxButton.YesNo,
                    MessageBoxImage.None) == MessageBoxResult.Yes)
                Close();
        }
    }
}
