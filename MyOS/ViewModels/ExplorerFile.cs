using System;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using MyOS.FileSystem;
using MyOS.FileSystem.SpecialDataTypes;

namespace MyOS.ViewModels
{
    public class ExplorerFile
    {
        private byte _attributes;
        public byte Attributes
        {
            get => _attributes;
            set
            {
                _attributes = value;
                IsHidden = value == (value | (byte)MftHeader.Attribute.Hidden);
                IsDirectory = value == (value | (byte)MftHeader.Attribute.Directory);
                string uri = IsDirectory
                    ? (IsHidden ? "pack://application:,,,/Resources/hidden_folder.png" : "pack://application:,,,/Resources/folder.png")
                    : (IsHidden
                    ? "pack://application:,,,/Resources/hidden_file.png"
                    : "pack://application:,,,/Resources/file.png");
                ImageSource = new BitmapImage(new Uri(uri, UriKind.Absolute));
            }
        } // Атрибуты.
       
        public string FullName { get; set; } // Имя файла с расширением.
        public MyDateTime CreationDate { get; set; } // Дата создания.
        public MyDateTime ModificationDate { get; set; } // Дата последнего изменения.
        public string Size { get; set; } // Размер файла.   
        public int MftEntry { get; set; } // Номер записи MFT.
        public bool IsHidden { get; private set; }
        public bool IsDirectory { get; private set; }

        public ImageSource ImageSource { get; private set; }

        public ExplorerFile(MftHeader mftHeader, int mftEntry)
        {
            Attributes = mftHeader.Attributes;
            FullName = mftHeader.GetFullName();
            CreationDate = mftHeader.CreationDate;
            ModificationDate = mftHeader.ModificationDate;
            Size = IsDirectory ? "" : mftHeader.Size + " Б";
            MftEntry = mftEntry;
        }
    }
}
