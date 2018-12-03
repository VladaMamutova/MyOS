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
                _fileCreation = new FileCreation();
            CurrentDirectory.DataContext = new Path();
            UpdateFileTable();
        }

        private readonly FileCreation _fileCreation;

        private void UpdateFileTable()
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
            //_fileCreation.ShowDialog("Создание файла");
            //int dotIndex = _fileCreation.FileName.Text.LastIndexOf('.');
            ////проверка на длину имени и расширения
            //if (dotIndex != -1)
            //    SystemCalls.CreateFile(_fileCreation.FileName.Text.Substring(0, dotIndex),
            //        _fileCreation.FileName.Text.Substring(dotIndex), (Path) CurrentDirectory.DataContext);
            //else SystemCalls.CreateFile(_fileCreation.FileName.Text, "", (Path)CurrentDirectory.DataContext);
            for (int i = 0; i < 22; i++)
            {
                if (i == 21)
                    MessageBox.Show("");
                SystemCalls.CreateFile("file" + (i + 1), "txt", (Path)CurrentDirectory.DataContext);
            }
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

    }
}
