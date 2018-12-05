using System;
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
            Closed += (sender, e) => { _fileCreation.Close(); };
                _fileCreation = new FileCreationWindow();
            CurrentDirectory.DataContext = new Path();
            UpdateFileTable();
        }

        private readonly FileCreationWindow _fileCreation;

        public void UpdateFileTable()
        {
            FileTable.ItemsSource = SystemCalls.GetFileList((Path)CurrentDirectory.DataContext);
        }

        private void Create_Click(object sender, RoutedEventArgs e)
        {
            SystemCalls.Formatting();
            UpdateFileTable();
        }

        private void TestMyDateTime_Click(object sender, RoutedEventArgs e)
        {
            DateTime now = DateTime.Now;
            MyDateTime myDateTimeNow = new MyDateTime(now);
            MessageBox.Show("DateTime.Now: " + now + Environment.NewLine +
                            "MyDateTime: " + myDateTimeNow + " (" + myDateTimeNow.GetStringUtcHours() + ")");
        }

        private void CreateFile_Click(object sender, RoutedEventArgs e)
        {
            _fileCreation.ShowDialog("Создание файла");
            int dotIndex = _fileCreation.FileName.Text.LastIndexOf('.');
            //проверка на длину имени и расширения
            if (dotIndex != -1)
                SystemCalls.CreateFile(_fileCreation.FileName.Text.Substring(0, dotIndex),
                    _fileCreation.FileName.Text.Substring(dotIndex + 1), (Path)CurrentDirectory.DataContext);
            else SystemCalls.CreateFile(_fileCreation.FileName.Text, "", (Path)CurrentDirectory.DataContext);
            //for (int i = 0; i < 22; i++)
            //{
            //    if (i == 21)
            //        MessageBox.Show("");
            //    SystemCalls.CreateFile("file" + (i + 1), "txt", (Path)CurrentDirectory.DataContext);
            //}
            UpdateFileTable();
        }

        private void CreateFolder_Click(object sender, RoutedEventArgs e)
        {
            _fileCreation.ShowDialog("Создание папки");
            SystemCalls.CreateFile(_fileCreation.FileName.Text, "", (Path)CurrentDirectory.DataContext, MftRecord.Attribute.Directory);
            UpdateFileTable();
        }

        private void FileTable_OnMouseDoubleClick_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (FileTable.CurrentCell.Item is DataGridCellInfo ||
                !(FileTable.CurrentCell.Item is FileRecord file)) return;

            if (file.Attributes != (file.Attributes | (byte) MftRecord.Attribute.Directory)) return;

            ((Path) CurrentDirectory.DataContext).Add(file.FileName);
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
        }

        private void Edit_Click(object sender, RoutedEventArgs e)
        {
            if (FileTable.CurrentCell.Item is DataGridCellInfo ||
                !(FileTable.SelectedItem is FileRecord file)) return;

            int dotIndex = file.FileName.LastIndexOf('.');
            string extension = dotIndex == -1 || file.Attributes ==
                               (file.Attributes | (byte)MftRecord.Attribute.Directory)
                ? ""
                : file.FileName.Substring(dotIndex + 1, file.FileName.Length - dotIndex - 1);
            string fileName =
                file.FileName.Substring(0, extension.Length > 0 ? file.FileName.Length - extension.Length - 1 : file.FileName.Length);
            RootRecord bufferRecord = new RootRecord
            {
                Attributes = file.Attributes,
                CreadtionDate = file.CreadtionDate,
                Extension = extension,
                FileName = fileName,
                Size = file.Size,
                Number = SystemCalls.HasFileWithSuchName(fileName, extension,
                    SystemCalls.GetDirectoryMftRecordNumber((Path) CurrentDirectory.DataContext))
            };
            TextEditorWindow editWindow = new TextEditorWindow(bufferRecord,
                    SystemCalls.ReadFileData(bufferRecord.Number, bufferRecord.Size),
                    (Path) CurrentDirectory.DataContext)
                {Owner = this};
            editWindow.Show();
        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            if (FileTable.CurrentCell.Item is DataGridCellInfo ||
                !(FileTable.SelectedItem is FileRecord file)) return;

            //SystemCalls.Delete((Path)CurrentDirectory.DataContext, file);
        }
    }
}
