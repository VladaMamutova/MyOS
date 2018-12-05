using System.Windows;


namespace MyOS
{
    /// <summary>
    /// Логика взаимодействия для TextEditorWindow.xaml
    /// </summary>
    public partial class TextEditorWindow
    {
        public TextEditorWindow(RootRecord file, string content, Path path)
        {
            InitializeComponent();
            Title = file.FileName + (file.Extension.Length == 0 ? "" : '.' + file.Extension);
            FileContent.Text = content;
            _buffer = new SystemBuffer
            {
                Path = new Path(path),
                Record = file
            };
        }

        private readonly SystemBuffer _buffer;

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            SystemCalls.SaveFile(_buffer, FileContent.Text);
            ((MainWindow)Owner).UpdateFileTable();
        }
    }
}
