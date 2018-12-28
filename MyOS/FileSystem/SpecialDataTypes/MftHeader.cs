using System;
using System.Collections.Generic;
using System.Text;

namespace MyOS.FileSystem.SpecialDataTypes
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
            InUse = 1
        }

        [Flags]
        public enum Attribute : byte
        {
            None = 0,
            ReadOnly = 0b1,
            Hidden = 0b10,
            Directory = 0b100
        }

        public Signature Sign { get; set; } // Признак записи.
        public string FileName { get; set; } // Имя файла.
        public string Extension { get; set; } // Расширение файла.
        public byte Attributes { get; set; } // Атрибуты.
        public int Size { get; set; } // Размер.
        public MyDateTime CreationDate { get; set; } // Дата создания файла.
        public MyDateTime ModificationDate { get; set; } // Дата последней модификации файла.
        public byte UserId { get; private set; } // Уникальный идентификатор владельца файла.
        public Permission Permissions { get; set; } // Права на файл.

        public MftHeader(string fileName, string extension, Attribute attributes = Attribute.None, int size = 0, int userId = -1)
        {
            Sign = Signature.InUse;
            FileName = fileName;
            Extension = extension;
            Attributes = (byte)attributes;
            Size = size;
            CreationDate = ModificationDate = new MyDateTime(DateTime.Now);
            if (userId < 0) UserId = FileSystem.CurrentUser.Id;
            else UserId = (byte)userId;
            Permissions = new Permission();
        }

        public MftHeader(byte[] recordHeader)
        {
            Sign = (Signature) recordHeader[0];
            FileName = Encoding.UTF8.GetString(recordHeader.GetRange(1, 26)).Trim('\0');
            Extension = Encoding.UTF8.GetString(recordHeader.GetRange(27, 5)).Trim('\0');
            Attributes = recordHeader[32];
            Size = BitConverter.ToInt32(recordHeader, 33);
            CreationDate = new MyDateTime(recordHeader.GetRange(37, 5));
            ModificationDate = new MyDateTime(recordHeader.GetRange(42, 5));
            UserId = recordHeader[47];
            Permissions = new Permission(recordHeader.GetRange(48, 2));
        }

        public byte[] GetBytes()
        {
            List<byte> headerBytes = new List<byte> {(byte) Sign};
            headerBytes.AddRange(FileName.GetFormatBytes(26));
            headerBytes.AddRange(Extension.GetFormatBytes(5));
            headerBytes.Add(Attributes);
            headerBytes.AddRange(BitConverter.GetBytes(Size));
            headerBytes.AddRange(CreationDate.DateTimeBytes);
            headerBytes.AddRange(ModificationDate.DateTimeBytes);
            headerBytes.Add(UserId);
            headerBytes.AddRange(Permissions.GetBytes());            
            return headerBytes.ToArray();
        }

        public bool HasPermissions(UserRecord user, Permission.Rights rights)
        {
            if (user.Id == UserId)
                return Permissions.CheckRights(Permission.UserSign.Owner, rights);
            return Permissions.CheckRights(
                user.IsAdministrator ? Permission.UserSign.Administrator : Permission.UserSign.Other, rights);
        }

        public void ChangeUser(int newUserId)
        {
            UserId = (byte) newUserId;
        }

        public string GetFullName()
        {
            return FileName + (Extension != "" ? "." + Extension : "");
        }

        public bool HasAttribute(Attribute attribute) => Attributes == (Attributes | (byte)attribute);
        public void SetAttribute(Attribute attribute, bool state)
        {
            if (state) Attributes |= (byte)attribute;
            else Attributes = (byte)(Attributes & (byte)~attribute);
        }

        private string AttributesToString()
        {
            string attributes = "";
            attributes += HasAttribute(Attribute.Directory) ? "D" : "-";
            attributes += HasAttribute(Attribute.Hidden) ? "H" : "-";
            return attributes + (HasAttribute(Attribute.ReadOnly) ? "R" : "-");
        }

        public override string ToString()
        {
            return (Sign == Signature.NotUsed
                ? "NotUsed"
                : "InUse") + $"   {AttributesToString()}   {FileName}   " +
                  $"{Extension}   {Size} Б   " +
                  $"{CreationDate.GetFullDateTime()}   {ModificationDate.GetFullDateTime()}   " +
                  $"{UserId}   {Permissions}";
        }
    }
}
