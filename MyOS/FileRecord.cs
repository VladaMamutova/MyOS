namespace MyOS
{
    struct FileRecord
    {
        public byte Attributes { get; set; } // Атрибуты.
        public string FullName { get; set; } // Имя файла с расширением.
        public MyDateTime CreadtionDate { get; set; } // Дата создания.
        public int Size { get; set; } // Размер файла.   
    }
}
