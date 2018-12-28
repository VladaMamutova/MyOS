using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MyOS.ViewModels;

namespace MyOS.FileSystem.SpecialDataTypes
{
    /// <summary>
    /// Запись корневого каталога.
    /// </summary>
    public class DirectoryRecord : IRecord
    {
        public const int Length = 36;
        public string FileName { get; set; } // Имя файла.
        public byte Attributes { get; set; } // Атрибуты.
        public string Extension { get; set; } // Расширение.
        public int MftEntry { get; set; } // Номер записи в MFT.

        public static readonly DirectoryRecord Empty;

        static DirectoryRecord() { Empty = new DirectoryRecord(new byte[Length]); }

        public DirectoryRecord() { }

        public DirectoryRecord(byte[] recordBytes)
        {
            if (recordBytes.Length != Length) return;
            FileName = Encoding.UTF8.GetString(recordBytes.GetRange(0, 26)).Trim('\0');
            Extension = Encoding.UTF8.GetString(recordBytes.GetRange(26, 5)).Trim('\0');
            Attributes = recordBytes[31];
            MftEntry = BitConverter.ToInt32(recordBytes, 32);
        }

        public int GetLength() => Length;
        public void SetRecord(byte[] recordBytes)
        {
            if (recordBytes.Length != Length) return;
            FileName = Encoding.UTF8.GetString(recordBytes.GetRange(0, 26)).Trim('\0');
            Extension = Encoding.UTF8.GetString(recordBytes.GetRange(27, 5)).Trim('\0');
            Attributes = recordBytes[31];
            MftEntry = BitConverter.ToInt32(recordBytes, 32);
        }

        public bool Equals(byte[] recordBytes) => GetBytes().SequenceEqual(recordBytes);
        
        public bool EqualsByName(string name) { return GetFullName() == name; }

        public DirectoryRecord(MftHeader mftEntry, int mftEntryNumber)
        {
            FileName = mftEntry.FileName;
            Extension = mftEntry.Extension;
            Attributes = mftEntry.Attributes;
            MftEntry = mftEntryNumber;
        }

        public DirectoryRecord(DirectoryRecord record)
        {
            FileName = record.FileName;
            Extension = record.Extension;
            Attributes = record.Attributes;
            MftEntry = record.MftEntry;
        }

        public DirectoryRecord(ExplorerFile record)
        {
            record.FullName.ParseFullName(record.IsDirectory, out var fileName, out var extension);
            FileName = fileName;
            Extension = extension;
            Attributes = record.Attributes;
            MftEntry = record.MftEntry;
        }

        public byte[] GetBytes()
        {
            List<byte> recordBytes = new List<byte>();
            recordBytes.AddRange(FileName.GetFormatBytes(26));
            recordBytes.AddRange(Extension.GetFormatBytes(5));
            recordBytes.Add(Attributes);
            recordBytes.AddRange(BitConverter.GetBytes(MftEntry));
            return recordBytes.ToArray();
        }

        public string GetFullName()
        {
            return FileName + (Extension != "" ? "." + Extension : "");
        }

        public override string ToString()
        {
            return GetFullName() + " " + MftEntry;
        }

        public bool HasAttribute(MftHeader.Attribute attribute) => Attributes == (Attributes | (byte)attribute);
    }
}
