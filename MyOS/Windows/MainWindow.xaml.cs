using System;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

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

        public struct FileInfo
        {
            public bool Attributes { get; set; } // Атрибуты.
            public bool FullName { get; set; } // Имя файла с расширением.
            public bool CreadtionDate { get; set; } // Дата создания.
            public bool ModificationDate { get; set; } // Дата создания.
            public bool Size { get; set; } // Размер файла.   
        }
        

        private void ShowMenu(object sender, RoutedEventArgs e)
        {
            Menu.Visibility = Menu.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
        }

        private void ShowControlWindow(object sender, RoutedEventArgs e)
        {
            ControlWindow controlWindow = new ControlWindow { Owner = this };
            controlWindow.ShowDialog();
        }

        private void ShowInfo_Click(object sender, RoutedEventArgs e)
        {
            if (FileTable.CurrentCell.Item is DataGridCellInfo ||
                !(FileTable.SelectedItem is ExplorerFile file)) return;
            //Установка иконки
            PropertiesWindow propertiesWindow = new PropertiesWindow(file.MftEntry);
            propertiesWindow.ShowDialog();
        }

        public void UpdateFileTable()
        {
            //List<FileRecord> files= SystemCalls.GetFileList((Path)CurrentDirectory.DataContext);
            //ObservableCollection<ExplorerFile> fileList = new ObservableCollection<ExplorerFile>();
            //for (int i = 0; i < fileList.Count; ++i)
            //{
            //    fileList.Add(new ExplorerFile() { Name = m_pFields.Field[i].AliasName, Value = DisplayedValueForRow(i), Index = i });
            //}

            //// Set ItemSource to populate grid
            //addressGrid.ItemsSource = propertyList;
            FileTable.ItemsSource = SystemCalls.GetFileList((Path)CurrentDirectory.DataContext);
        }

        private void Sign_Click(object sender, RoutedEventArgs e)
        {
            LoginWindow loginWindow = new LoginWindow();
            loginWindow.ShowDialog();
        }

        private string GetErrorMessage(int error, string name, string extension)
        {
            switch (error)
            {
                case -1: return "Недостаточно места на диске!";
                case -2: return "В данном расположении уже существует папка с именем \"" + name +
                            (extension == "" ? "" : '.' + extension) + "\"." + Environment.NewLine +
                            "Задайте другое имя.";
                case -3: return "В данном расположении уже существует файл с именем \"" + name +
                            (extension == "" ? "" : '.' + extension) + "\"." + Environment.NewLine +
                            "Задайте другое имя.";
            }
            return null;
        }

        private void CreateFile_Click(object sender, RoutedEventArgs e)
        {
            FileCreationWindow fileCreation = new FileCreationWindow("Создание файла");
            fileCreation.ShowDialog();
            int dotIndex = fileCreation.FileName.Text.LastIndexOf('.');
            //проверка на длину имени и расширения
            string name, extension;
            if (dotIndex == -1)
            {
                name = fileCreation.FileName.Text;
                extension = "";
            }
            else
            {
                name = fileCreation.FileName.Text.Substring(0, dotIndex);
                extension = fileCreation.FileName.Text.Substring(dotIndex + 1);
            }

            MftHeader newFile = new MftHeader(name, extension);

            string error = GetErrorMessage(SystemCalls.CreateFile(newFile, (Path)CurrentDirectory.DataContext), name, extension);
            if (error != null)
                MessageBox.Show(error, "Ошибка создания файла!", MessageBoxButton.OK, MessageBoxImage.Error);

            //for (int i = 0; i < 100; i++)
            //{
            //    if (i == 1)
            //        MessageBox.Show("");
            //    SystemCalls.CreateFile(new MftHeader("file" + (i + 1), "txt"), (Path)CurrentDirectory.DataContext);
            //}
            UpdateFileTable();
        }

        private void CreateFolder_Click(object sender, RoutedEventArgs e)
        {
            FileCreationWindow fileCreation = new FileCreationWindow("Создание папки");
            fileCreation.ShowDialog();

            MftHeader newFile = new MftHeader(fileCreation.FileName.Text, attributes: MftHeader.Attribute.Directory);
            int result = SystemCalls.CreateFile(newFile, (Path) CurrentDirectory.DataContext);
            string error = GetErrorMessage(result, newFile.FileName, newFile.Extension);
            if (error != null) MessageBox.Show(error, "Ошибка создания файла!", MessageBoxButton.OK, MessageBoxImage.Error);
            UpdateFileTable();
        }

        private void FileTable_OnMouseDoubleClick_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (FileTable.CurrentCell.Item is DataGridCellInfo ||
                !(FileTable.SelectedItem is ExplorerFile file)) return;

            if (file.Attributes == (file.Attributes | (byte) MftHeader.Attribute.Directory))
            {

                ((Path) CurrentDirectory.DataContext).Add(file.FullName);
                CurrentDirectory.Content = ((Path) CurrentDirectory.DataContext).CurrentPath;
                UpdateFileTable();
            }
            else EditFile();
        }

        private void PreviousDirectory_Click(object sender, RoutedEventArgs e)
        {
            ((Path)CurrentDirectory.DataContext).GetPreviosFolder();
            CurrentDirectory.Content = ((Path)CurrentDirectory.DataContext).CurrentPath;
            UpdateFileTable();
        }

        private void Copy_Click(object sender, RoutedEventArgs e)
        {
            if (FileTable.CurrentCell.Item is DataGridCellInfo ||
                !(FileTable.SelectedItem is ExplorerFile file)) return;

            SystemCalls.Copy((Path)CurrentDirectory.DataContext, file);
        }

        private void Paste_Click(object sender, RoutedEventArgs e)
        {
            SystemCalls.Paste((Path)CurrentDirectory.DataContext);
            UpdateFileTable();
            FreeClusters.Content = Bitmap.FreeClusters;
        }

        private void EditFile()
        {
            if (FileTable.CurrentCell.Item is DataGridCellInfo ||
                !(FileTable.SelectedItem is ExplorerFile file)) return;

            int dotIndex = file.FullName.LastIndexOf('.');
            //проверка на длину имени и расширения
            string fileName, extension;
            if (dotIndex == -1 || file.Attributes ==
                (file.Attributes | (byte)MftHeader.Attribute.Directory))
            {
                fileName = file.FullName;
                extension = "";
            }
            else
            {
                fileName = file.FullName.Substring(0, dotIndex);
                extension = file.FullName.Substring(dotIndex + 1);
            }

            int directoryMftEntry = SystemCalls.FindDirectoryMftEntry((Path) CurrentDirectory.DataContext);
            DirectoryRecord record = SystemCalls.GetFileInDirectory(directoryMftEntry, SystemCalls.GetFilePosition(fileName, extension, directoryMftEntry)); 
            DirectoryRecord bufferRecord = new DirectoryRecord
            {
                Attributes = file.Attributes,
                CreationDate = file.CreationDate,
                Extension = extension,
                FileName = fileName,
                //Size = file.Size,
                Number = record.Number
            };

            TextEditorWindow editWindow = new TextEditorWindow(bufferRecord,
                    Encoding.UTF8.GetString(SystemCalls.ReadFileData(SystemCalls.ReadDataAttributes(bufferRecord.Number))),
                    (Path)CurrentDirectory.DataContext)
            { Owner = this };
            editWindow.Show();
        }

        private void Rename_Click(object sender, RoutedEventArgs e)
        {
            if (FileTable.CurrentCell.Item is DataGridCellInfo ||
                !(FileTable.SelectedItem is ExplorerFile file)) return;

            FileCreationWindow fileCreation =
                new FileCreationWindow("Переименование файла") {FileName = {Text = file.FullName}};
            int dotIndex = file.FullName.LastIndexOf('.');
            //проверка на длину имени и расширения
            string prevName, prevExtension;
            if (dotIndex == -1 || file.Attributes ==
                (file.Attributes | (byte)MftHeader.Attribute.Directory))
            {
                prevName = fileCreation.FileName.Text;
                prevExtension = "";
            }
            else
            {
                prevName = fileCreation.FileName.Text.Substring(0, dotIndex);
                prevExtension = fileCreation.FileName.Text.Substring(dotIndex + 1);
            }
            
            fileCreation.ShowDialog();
            dotIndex = fileCreation.FileName.Text.LastIndexOf('.');
            //проверка на длину имени и расширения
            string name, extension;
            if (dotIndex == -1)
            {
                name = fileCreation.FileName.Text;
                extension = "";
            }
            else
            {
                name = fileCreation.FileName.Text.Substring(0, dotIndex);
                extension = fileCreation.FileName.Text.Substring(dotIndex + 1);
            }

            int directoryMftEntry = SystemCalls.FindDirectoryMftEntry((Path)CurrentDirectory.DataContext);
            DirectoryRecord newRecord = SystemCalls.GetFileInDirectory(directoryMftEntry,
                SystemCalls.GetFilePosition(name, extension, directoryMftEntry));
            if (newRecord != null)
            {
                MessageBox.Show(GetErrorMessage(newRecord.Attributes == (newRecord.Attributes | (byte)MftHeader.Attribute.Directory) ? -2 : -3, name, extension), "Ошибка создания файла!", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            DirectoryRecord lastRecord = SystemCalls.GetFileInDirectory(directoryMftEntry, SystemCalls.GetFilePosition(prevName, prevExtension, directoryMftEntry));
            SystemCalls.Rename(lastRecord, name, extension, (Path)CurrentDirectory.DataContext);

            UpdateFileTable();
        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            if (FileTable.CurrentCell.Item is DataGridCellInfo ||
                !(FileTable.SelectedItem is ExplorerFile file)) return;

            int dotIndex = file.FullName.LastIndexOf('.');
            string extension = dotIndex == -1 || file.Attributes ==
                               (file.Attributes | (byte)MftHeader.Attribute.Directory)
                ? ""
                : file.FullName.Substring(dotIndex + 1);
            string fileName =
                file.FullName.Substring(0, extension.Length > 0 ? file.FullName.Length - extension.Length - 1 : file.FullName.Length);
            int directoryMftEntry = SystemCalls.FindDirectoryMftEntry((Path) CurrentDirectory.DataContext);
            DirectoryRecord record = new DirectoryRecord()
            {
                Attributes = file.Attributes,
                CreationDate = file.CreationDate,
                Extension = extension,
                FileName = fileName,
                //Size = file.Size,
                Number = SystemCalls.GetFileInDirectory(directoryMftEntry, SystemCalls.GetFilePosition(fileName, extension, directoryMftEntry)).Number
            };
            SystemCalls.Delete((Path)CurrentDirectory.DataContext, record);
            UpdateFileTable();
            FreeClusters.Content = Bitmap.FreeClusters;
        }

        private void MainWindow_OnLoaded(object sender, RoutedEventArgs e)
        {
            CurrentDirectory.DataContext = new Path();
            CurrentDirectory.Content = ((Path)CurrentDirectory.DataContext).CurrentPath;
            User.Text = Account.User.Name;
            UpdateFileTable();
            //ImageS.Source = new BitmapImage(new Uri("pack://application:,,,/Resources/file1.png", UriKind.Absolute));
            Time.Text = new MyDateTime(DateTime.Now).ToString();
            var timer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = new TimeSpan(0, 1, 0),
                IsEnabled = true
            };
            timer.Tick += (o, t) => { Time.Text = new MyDateTime(DateTime.Now).ToString(); };
            timer.Start();
        }

        private void User_MouseLeftButtonDown(object sender, RoutedEventArgs e)
        {

        }
    }
}
