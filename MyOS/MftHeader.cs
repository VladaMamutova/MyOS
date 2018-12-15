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
            IsData = 2
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
        public byte UserId { get; } // Уникальный идентификатор владельца файла.
        public Permission Permission { get; set; } // Права на файл.

        //public MftHeader() { }

        public MftHeader(string fileName, string extension = "", int size = 0, Attribute attributes = Attribute.None)
        {
            Sign = Signature.InUse;
            FileName = fileName;
            Attributes = (byte) attributes;
            Extension = extension;
            Size = size;
            CreationDate = ModificationDate = new MyDateTime(DateTime.Now);
            UserId = Account.User.Id;
            Permission = new Permission(Account.User.Id == User.AdministratorId ? Permission.UserSign.Administrator : Permission.UserSign.Owner);
        }

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
            Permission = new Permission(recordHeader.GetRange(48, 2));
        }

        public byte[] GetBytes()
        {
            List<byte> headerBytes = new List<byte>
                { (byte) Sign, Attributes};
            headerBytes.AddRange(FileName.GetFormatBytes(26));
            headerBytes.AddRange(Extension.GetFormatBytes(5));
            headerBytes.AddRange(BitConverter.GetBytes(Size));
            headerBytes.AddRange(CreationDate.DateTimeBytes);
            headerBytes.AddRange(ModificationDate.DateTimeBytes);
            headerBytes.Add(UserId);
            headerBytes.AddRange(Permission.GetBytes());            
            return headerBytes.ToArray();
        }

        public bool HasPermissions(byte uid, Permission.Rights rights)
        {
            if (Permission.CheckRights(uid == User.AdministratorId ? Permission.UserSign.Administrator :
                uid == UserId ? Permission.UserSign.Owner : Permission.UserSign.Other, rights))
                return true;
            return false;
        }

        public string GetFullName()
        {
            return FileName + (Extension != "" ? "." + Extension : "");
        }

        public void SetAttribute(Attribute attribute, bool state)
        {
            if (state) Attributes |= (byte)attribute;
            else Attributes = (byte)(Attributes & (byte)~attribute);
        }
        public bool HasAttribute(Attribute attribute) => Attributes == (Attributes | (byte) attribute);
    }
}
