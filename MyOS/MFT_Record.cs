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
        
        public Signature Sign { get; set; } // Признак записи.
        public byte Attributes { get; set; } // Атрибуты.
        public string Extension { get; set; } // Расширение файла.
        public int Size { get; set; } // Размер.
        public SystemStructures.MyDateTime CreatDate { get; set; } // Дата создания файла.
        public SystemStructures.MyDateTime ModifDate { get; set; } // Дата последней модификации файла.
        public byte UserId { get; set; } // Уникальный идентификатор владельца файла.
        public string FileName { get; set; } // Имя файла.
        public int SecurityDescriptor { get; set; } // Дескриптор безопасности.
        // Размер информации о файле - 51 байт

        public SystemStructures.Data DataAtr { get; set; }
        
        // Под данные файла (под Data) отводится 973 байт (1024 - 51).
    }
}
