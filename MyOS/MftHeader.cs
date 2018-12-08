using System;
using System.Collections.Generic;
using System.Text;

namespace MyOS
{
    /// <summary>
    /// Заголовок MFT-записи.
    /// </summary>
    public class MftHeader
    {
        public const int Length = 50;
        // Перечисление признаков записи.
        public enum Signature : byte
        {
            NotUsed = 0,
            InUse = 1,
            IsAccessControlList = 2,
            IsData = 3
        }

        [Flags]
        public enum Attribute : byte
        {
            Directory = 0b1000,
            Hidden = 0b100,
            System = 0b10,
            ReadOnly = 0b1,
            None = 0
        }
        public Signature Sign { get; set; } // Признак записи.
        public string FileName { get; set; } // Имя файла.
        public byte Attributes { get; set; } // Атрибуты.
        public string Extension { get; set; } // Расширение файла.
        public int Size { get; set; } // Размер.
        public MyDateTime CreationDate { get; set; } // Дата создания файла.
        public MyDateTime ModificationDate { get; set; } // Дата последней модификации файла.
        public byte UserId { get; set; } // Уникальный идентификатор владельца файла.
        public Permissions Permissions { get; set; } // Дескриптор безопасности.

        public MftHeader() { }

        public MftHeader(byte[] recordHeader)
        {
            Sign = (Signature) recordHeader[0];
            Attributes = recordHeader[1];
            FileName = Encoding.UTF8.GetString(recordHeader.GetRange(2, 26)).Trim('\0');
            Extension = Encoding.UTF8.GetString(recordHeader.GetRange(28, 5)).Trim('\0');
            Size = recordHeader.GetRange(33, 4).ToInt32();
            CreationDate = new MyDateTime(recordHeader.GetRange(37, 5));
            ModificationDate = new MyDateTime(recordHeader.GetRange(42, 5));
            UserId = recordHeader[47];
            Permissions = new Permissions(recordHeader.GetRange(48, 2));
        }

        public byte[] GetBytes()
        {
            List<byte> headerBytes = new List<byte>();
            headerBytes.Add((byte)Sign);
            headerBytes.Add(Attributes);
            headerBytes.AddRange(FileName.GetFormatBytes(26));
            headerBytes.AddRange(Extension.GetFormatBytes(5));
            headerBytes.AddRange(BitConverter.GetBytes(Size));
            headerBytes.AddRange(CreationDate.DateTimeBytes);
            headerBytes.AddRange(ModificationDate.DateTimeBytes);
            headerBytes.Add(UserId);
            headerBytes.AddRange(Permissions.PermissionBytes);            
            return headerBytes.ToArray();

        }
    }
}
