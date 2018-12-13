
namespace MyOS.Windows
{
    /// <summary>
    /// Логика взаимодействия для FilePropertiesWindow.xaml
    /// </summary>
    public partial class FilePropertiesWindow
    {
        public FilePropertiesWindow(MftHeader mftHeader)
        {
            InitializeComponent();
            FileName.Text = mftHeader.GetFullName();
            Type.Text = mftHeader.IsDirectory() ? "Папка с файлами" : mftHeader.Extension == "" ? "Файл" : mftHeader.Extension;
            CreationDateTime.Text = mftHeader.CreationDate.ToString();
            ModificationDateTime.Text = mftHeader.CreationDate.ToString();
            Size.Text = mftHeader.Size + " Б";

        }
    }
}
