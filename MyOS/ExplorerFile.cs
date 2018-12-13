using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace MyOS
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
                string uri = value == (value | (byte) MftHeader.Attribute.Directory)
                    ? "pack://application:,,,/Resources/folder1.png"
                    : "pack://application:,,,/Resources/file1.png";
                ImageSource = new BitmapImage(new Uri(uri, UriKind.Absolute));
            }
        } // Атрибуты.
       
        public string FullName { get; set; } // Имя файла с расширением.
        public MyDateTime CreationDate { get; set; } // Дата создания.
        public MyDateTime ModificationDate { get; set; } // Дата последнего изменения.
        public int Size { get; set; } // Размер файла.   
        public int MftEntry { get; set; } // Номер записи MFT.

        public ImageSource ImageSource { get; set; }

        public ExplorerFile()
        {
            Attributes = (byte)MftHeader.Attribute.None;
            ImageSource = new BitmapImage(new Uri("pack://application:,,,/Resources/file.png", UriKind.Absolute));
            FullName = String.Empty;
            CreationDate = MyDateTime.MinValue;
            ModificationDate = MyDateTime.MinValue;
            Size = 0;
        }
    }
}
