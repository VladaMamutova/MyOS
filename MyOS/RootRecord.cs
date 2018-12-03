using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace MyOS
{
    /// <summary>
    /// Запись корневого каталога.
    /// </summary>
    class RootRecord
    {
        public string FileName { get; set; } // Имя файла.
        public byte Attributes { get; set; } // Атрибуты.
        public string Extension { get; set; } // Расширение.
        public int Size { get; set; } // Размер файла.
        public MyDateTime CreadtionDate { get; set; } // Дата последней модификации.
        public int Number { get; set; } // Номер записи в MFT.
        
        // Размер - 43 байт.

        public RootRecord() { }
        public RootRecord(byte[] recordBytes)
        {
            if (recordBytes.Length != SystemConstants.RootRecordLength) return;
            FileName = Encoding.UTF8.GetString(recordBytes.GetRange(0, 24)).Trim('\0');
            Attributes = recordBytes[24];
            Extension = Encoding.UTF8.GetString(recordBytes.GetRange(25, 5)).Trim('\0');
            Size = recordBytes.GetRange(30, 4).ToInt32();
            CreadtionDate = new MyDateTime(recordBytes.GetRange(34, 5));
            Number = recordBytes.GetRange(39, 4).ToInt32();
        }

        public byte[] GetBytes()
        {
            List<byte> recordBytes = new List<byte>();
            recordBytes.AddRange(FileName.GetFormatBytes(24));
            recordBytes.Add(Attributes);
            recordBytes.AddRange(Extension.GetFormatBytes(5));
            recordBytes.AddRange(BitConverter.GetBytes(Size));
            recordBytes.AddRange(CreadtionDate.DateTimeBytes);
            recordBytes.AddRange(BitConverter.GetBytes(Number));
            return recordBytes.ToArray();
        }

        public override string ToString()
        {
            return FileName + (Extension != "" ? "." + Extension : "") + (Attributes == (Attributes | (byte)MftRecord.Attribute.Directory) ? "\\" : "") + "  " + Size + "Б  " + CreadtionDate;
        }
    }
}
