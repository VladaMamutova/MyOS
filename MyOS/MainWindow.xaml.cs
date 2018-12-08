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
            CurrentDirectory.DataContext = new Path();
            UpdateFileTable();
        }
        
        public void UpdateFileTable()
        {
            FileTable.ItemsSource = SystemCalls.GetFileList((Path)CurrentDirectory.DataContext);
        }

        private void Create_Click(object sender, RoutedEventArgs e)
        {
            SystemCalls.Formatting();
            CurrentDirectory.DataContext = new Path();
            CurrentDirectory.Content = ((Path)CurrentDirectory.DataContext).CurrentPath;
            SystemData.InitializeFreeClusters();
            UpdateFileTable();
        }

        private void TestMyDateTime_Click(object sender, RoutedEventArgs e)
        {
            DateTime now = DateTime.Now;
            MyDateTime myDateTimeNow = new MyDateTime(now);
            MessageBox.Show("DateTime.Now: " + now + Environment.NewLine +
                            "MyDateTime: " + myDateTimeNow + " (" + myDateTimeNow.GetStringUtcHours() + ")");
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

            MyDateTime now = new MyDateTime(DateTime.Now);
            MftHeader newFile = new MftHeader
            {
                Sign = MftHeader.Signature.InUse,
                FileName = name,
                Attributes = (byte)MftHeader.Attribute.None,
                Extension = extension,
                Size = 0,
                CreationDate = now,
                ModificationDate = now,
                UserId = new byte(),
                Permissions = new Permissions(new byte[] { 0, 0 })
            };
            string error = GetErrorMessage(SystemCalls.CreateFile(newFile, (Path)CurrentDirectory.DataContext), name, extension);
            if (error != null)
                MessageBox.Show(error, "Ошибка создания файла!", MessageBoxButton.OK, MessageBoxImage.Error);

            //for (int i = 0; i < 100; i++)
            //{
            //    if (i == 1)
            //        MessageBox.Show("");
            //    SystemCalls.CreateFile("file" + (i + 1), "txt", (Path)CurrentDirectory.DataContext);
            //}
            UpdateFileTable();
        }

        private void CreateFolder_Click(object sender, RoutedEventArgs e)
        {
            FileCreationWindow fileCreation = new FileCreationWindow("Создание папки");
            fileCreation.ShowDialog();
            MyDateTime now = new MyDateTime(DateTime.Now);
            MftHeader newFile = new MftHeader
            {
                Sign = MftHeader.Signature.InUse,
                FileName = fileCreation.FileName.Text,
                Attributes = (byte)MftHeader.Attribute.Directory,
                Extension = "",
                Size = 0,
                CreationDate = now,
                ModificationDate = now,
                UserId = new byte(),
                Permissions = new Permissions(new byte[] { 0, 0 })
            };
            string error = GetErrorMessage(SystemCalls.CreateFile(newFile, (Path) CurrentDirectory.DataContext),
                newFile.FileName, newFile.Extension);
            if (error != null)
                MessageBox.Show(error, "Ошибка создания файла!", MessageBoxButton.OK, MessageBoxImage.Error);
            UpdateFileTable();
        }

        private void FileTable_OnMouseDoubleClick_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (FileTable.CurrentCell.Item is DataGridCellInfo ||
                !(FileTable.SelectedItem is FileRecord file)) return;

            if (file.Attributes != (file.Attributes | (byte) MftHeader.Attribute.Directory)) return;

            ((Path) CurrentDirectory.DataContext).Add(file.FullName);
            CurrentDirectory.Content = ((Path) CurrentDirectory.DataContext).CurrentPath;
            UpdateFileTable();
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
                !(FileTable.SelectedItem is FileRecord file)) return;

            SystemCalls.Copy((Path)CurrentDirectory.DataContext, file);
        }

        private void Paste_Click(object sender, RoutedEventArgs e)
        {
            SystemCalls.Paste((Path)CurrentDirectory.DataContext);
            UpdateFileTable();
        }

        private void Edit_Click(object sender, RoutedEventArgs e)
        {
            if (FileTable.CurrentCell.Item is DataGridCellInfo ||
                !(FileTable.SelectedItem is FileRecord file)) return;

            int dotIndex = file.FullName.LastIndexOf('.');
            //проверка на длину имени и расширения
            string fileName, extension;
            if (dotIndex == -1)
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
            RootRecord record = SystemCalls.GetFileInDirectory(directoryMftEntry, SystemCalls.GetFilePosition(fileName, extension, directoryMftEntry)); 
            RootRecord bufferRecord = new RootRecord
            {
                Attributes = file.Attributes,
                CreadtionDate = file.CreadtionDate,
                Extension = extension,
                FileName = fileName,
                Size = file.Size,
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
                !(FileTable.SelectedItem is FileRecord file)) return;

            FileCreationWindow fileCreation = new FileCreationWindow("Переименование файла");
            fileCreation.FileName.Text = file.FullName;
            int dotIndex = file.FullName.LastIndexOf('.');
            //проверка на длину имени и расширения
            string prevName, prevExtension;
            if (dotIndex == -1)
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
            RootRecord record = SystemCalls.GetFileInDirectory(directoryMftEntry, SystemCalls.GetFilePosition(prevName, prevExtension, directoryMftEntry));
            SystemCalls.Rename(record, name, extension, (Path)CurrentDirectory.DataContext);

            UpdateFileTable();
        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            if (FileTable.CurrentCell.Item is DataGridCellInfo ||
                !(FileTable.SelectedItem is FileRecord file)) return;

            //SystemCalls.Delete((Path)CurrentDirectory.DataContext, file);
        }
    }
}
