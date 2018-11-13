namespace MyOS
{
    /// <summary>
    /// Запись корневого каталога.
    /// </summary>
    class Root_Record
    {
        private string fileName; // Имя файла.
        private byte attributes; // Атрибуты.
        private string extension; // Расширение.
        private int size; // Размер файла.
        private short modifDate; // Дата последней модификации.
        private int number; // Номер записи в MFT.

        // Размер - 46 байт.
    }
}
