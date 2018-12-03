using System;
using System.Collections.Generic;

namespace MyOS
{
    /// <summary>
    /// Запись MFT.
    /// </summary>
    class MftRecord
    {
        // Размер каждой записи - фиксированный, равен 1024 байт.

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

        /// <summary>
        /// Атрибут Data записей MFT.
        /// </summary>
        //public struct Data
        //{
        //    public byte Header;
        //    public List<int> Blocks;

        //    public Data(byte header = 0)
        //    {
        //        Header = header;
        //        Blocks = new List<int>();
        //    }
        //    public Data(int[] blocks)
        //    {
        //        //Header = (byte)(blocks == null || blocks.Length == 0 ? 0 : 1);
        //        if (blocks == null || blocks.Length == 0)
        //        {
        //            Header = 0;
        //            Blocks = new List<int>();
        //        }
        //        else
        //        {
        //            Header = 1;
        //            Blocks = new List<int>(blocks);
        //        }
        //    }
        //}

        public Signature Sign { get; set; } // Признак записи.
        public byte Attributes { get; set; } // Атрибуты.
        public string Extension { get; set; } // Расширение файла.
        public int Size { get; set; } // Размер.
        public MyDateTime CreationDate { get; set; } // Дата создания файла.
        public MyDateTime ModificationDate { get; set; } // Дата последней модификации файла.
        public byte UserId { get; set; } // Уникальный идентификатор владельца файла.
        public string FileName { get; set; } // Имя файла.
        public int SecurityDescriptor { get; set; } // Дескриптор безопасности.
        // Размер информации о файле - 51 байт.

        public List<int> Data { get; set; }
        
        // Под данные файла (под Data) отводится 973 байт (1024 - 51).
    }
}
