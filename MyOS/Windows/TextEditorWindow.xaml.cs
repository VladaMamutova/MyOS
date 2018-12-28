using System.Text;
using System.Windows;
using System.Windows.Controls;
using MyOS.FileSystem;
using MyOS.FileSystem.SpecialDataTypes;
using MyOS.ViewModels;


namespace MyOS
{
    /// <summary>
    /// Логика взаимодействия для TextEditorWindow.xaml
    /// </summary>
    public partial class TextEditorWindow
    {
        public TextEditorWindow(ExplorerFile file, string fullName)
        {
            InitializeComponent();
            Title = fullName;
            FileContent.Text = _content = Encoding.UTF8.GetString(SystemCalls.ReadFileData(file.MftEntry));
             _record = new DirectoryRecord(file);
        }

        private readonly DirectoryRecord _record;
        private readonly string _content;

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                SystemCalls.Save(_record, Encoding.UTF8.GetBytes(FileContent.Text));
                Save.IsEnabled = false;
            }
            catch (FsException fsException)
            {
                fsException.ShowError(FsException.Command.Save,
                    _record.HasAttribute(MftHeader.Attribute.Directory)
                        ? FsException.Element.Folder
                        : FsException.Element.File);
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void FileContent_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            Save.IsEnabled = _content != FileContent.Text;
        }
    }
}
