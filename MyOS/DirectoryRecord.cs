using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MyOS
{
    /// <summary>
    /// Запись корневого каталога.
    /// </summary>
    public class DirectoryRecord
    {
        public const int Length = 41;
        public string FileName { get; set; } // Имя файла.
        public byte Attributes { get; set; } // Атрибуты.
        public string Extension { get; set; } // Расширение.
        //public int Size { get; set; } // Размер файла.
        public MyDateTime CreationDate { get; set; } // Дата последней модификации.
        public int Number { get; set; } // Номер записи в MFT.

        //public static readonly RootRecord Empty;

        //static RootRecord() { Empty = new RootRecord(new byte[Length]); }

        public DirectoryRecord() { }

        public DirectoryRecord(byte[] recordBytes)
        {
            if (recordBytes.Length != Length) return;
            FileName = Encoding.UTF8.GetString(recordBytes.GetRange(0, 26)).Trim('\0');
            Attributes = recordBytes[26];
            Extension = Encoding.UTF8.GetString(recordBytes.GetRange(27, 5)).Trim('\0');
            //Size = recordBytes.GetRange(32, 4).ToInt32();
            CreationDate = new MyDateTime(recordBytes.GetRange(32, 5));
            Number = recordBytes.GetRange(37, 4).ToInt32();
        }

        public DirectoryRecord(MftHeader mftEntry, int mftEntryNumber)
        {
            FileName = mftEntry.FileName;
            Attributes = mftEntry.Attributes;
            Extension = mftEntry.Extension;
            //Size = mftEntry.Size;
            CreationDate = mftEntry.CreationDate;
            Number = mftEntryNumber;
        }

        public byte[] GetBytes()
        {
            List<byte> recordBytes = new List<byte>();
            recordBytes.AddRange(FileName.GetFormatBytes(26));
            recordBytes.Add(Attributes);
            recordBytes.AddRange(Extension.GetFormatBytes(5));
            //recordBytes.AddRange(BitConverter.GetBytes(Size));
            recordBytes.AddRange(CreationDate.DateTimeBytes);
            recordBytes.AddRange(BitConverter.GetBytes(Number));
            return recordBytes.ToArray();
        }

        public string GetFullName()
        {
            return FileName + (Extension != "" ? "." + Extension : "");
        }

        public bool IsEmpty()
        {
            DirectoryRecord empty = new DirectoryRecord(new byte[Length]);
            if (FileName != empty.FileName) return false;
            if (Attributes != empty.Attributes) return false;
            if (Extension != empty.Extension) return false;
            //if (Size != empty.Size) return false;
            if (CreationDate.DateTimeBytes.Where((dateByte, i) => dateByte != empty.CreationDate.DateTimeBytes[i]).Any())
                return false;

            return Number == empty.Number;
        }

        public override string ToString()
        {
            return GetFullName();//+ "  " + Size + "Б  " + CreationDate;
        }
    }
}
