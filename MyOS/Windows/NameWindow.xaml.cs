using System.Windows;
using System.Windows.Controls;
using MyOS.FileSystem;
using MyOS.FileSystem.SpecialDataTypes;
using MyOS.ViewModels;

namespace MyOS
{
    /// <summary>
    /// Логика взаимодействия для FileCreationWindow.xaml
    /// </summary>
    public partial class NameWindow
    {

        private readonly FsException.Command _command;
        private readonly FsException.Element _element;
        private readonly DirectoryRecord _record;
        private readonly Path _path;

        public NameWindow(FsException.Element element, Path path)
        {
            InitializeComponent();
            _command = FsException.Command.Create;
            _element = element;
            _path = path;
            Title = FsException.GetCaption(_command, _element);
        }

        public NameWindow(ExplorerFile file, Path path)
        {
            InitializeComponent();
            _command = FsException.Command.Rename;
            _element = file.IsDirectory ? FsException.Element.Folder : FsException.Element.File;
            _record = new DirectoryRecord(file);
            _path = path;
            Title = FsException.GetCaption(_command, _element);
            FileName.Text = file.FullName;
        }

        private void Load(object sender, RoutedEventArgs e)
        {
            FileName.Select(0,
                FileName.Text.Contains(".") && _element == FsException.Element.File
                    ? FileName.Text.LastIndexOf('.')
                    : FileName.Text.Length);
            FileName.Focus();
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                FileName.Text.ParseFullName(_element == FsException.Element.Folder, out var fileName,
                    out var extension);
                if (fileName.Length > 26) throw new FsException(FsException.Code.NameTooLong, fileName);
                if (extension.Length > 5) throw new FsException(FsException.Code.ExtensionTooLong, "." + extension);

                if (_command == FsException.Command.Create)
                {
                    SystemCalls.Create(_path, fileName, extension,
                        _element == FsException.Element.Folder
                            ? MftHeader.Attribute.Directory
                            : MftHeader.Attribute.None);
                }
                else SystemCalls.Rename(_path, _record, FileName.Text);
                DialogResult = true;
            }
            catch (FsException ex)
            {
                ex.ShowError(_command, _element);
                Close();
            }
        }

        private void FileName_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_record != null)
                Оk.IsEnabled = FileName.Text.TrimEnd().TrimEnd().Length > 0 && _record.FileName != FileName.Text;
            else Оk.IsEnabled = FileName.Text.TrimEnd().TrimEnd().Length > 0;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
