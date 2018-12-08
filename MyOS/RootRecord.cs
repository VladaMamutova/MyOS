using System;
using System.Collections.Generic;
using System.Text;

namespace MyOS
{
    /// <summary>
    /// Запись корневого каталога.
    /// </summary>
    public class RootRecord
    {
        public const int Length = 45;
        public string FileName { get; set; } // Имя файла.
        public byte Attributes { get; set; } // Атрибуты.
        public string Extension { get; set; } // Расширение.
        public int Size { get; set; } // Размер файла.
        public MyDateTime CreadtionDate { get; set; } // Дата последней модификации.
        public int Number { get; set; } // Номер записи в MFT.

        public RootRecord() { }

        public RootRecord(byte[] recordBytes)
        {
            if (recordBytes.Length != Length) return;
            FileName = Encoding.UTF8.GetString(recordBytes.GetRange(0, 26)).Trim('\0');
            Attributes = recordBytes[26];
            Extension = Encoding.UTF8.GetString(recordBytes.GetRange(27, 5)).Trim('\0');
            Size = recordBytes.GetRange(32, 4).ToInt32();
            CreadtionDate = new MyDateTime(recordBytes.GetRange(36, 5));
            Number = recordBytes.GetRange(41, 4).ToInt32();
        }

        public RootRecord(MftHeader mftEntry, int mftEntryNumber)
        {
            FileName = mftEntry.FileName;
            Attributes = mftEntry.Attributes;
            Extension = mftEntry.Extension;
            Size = mftEntry.Size;
            CreadtionDate = mftEntry.CreationDate;
            Number = mftEntryNumber;
        }

        public byte[] GetBytes()
        {
            List<byte> recordBytes = new List<byte>();
            recordBytes.AddRange(FileName.GetFormatBytes(26));
            recordBytes.Add(Attributes);
            recordBytes.AddRange(Extension.GetFormatBytes(5));
            recordBytes.AddRange(BitConverter.GetBytes(Size));
            recordBytes.AddRange(CreadtionDate.DateTimeBytes);
            recordBytes.AddRange(BitConverter.GetBytes(Number));
            return recordBytes.ToArray();
        }

        public string GetFullName()
        {
            return FileName + (Extension != "" ? "." + Extension : "");
        }

        public override string ToString()
        {
            return GetFullName() + "  " + Size + "Б  " + CreadtionDate;
        }
    }
}
