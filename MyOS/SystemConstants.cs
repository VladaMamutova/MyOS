namespace MyOS
{
    /// <summary>
    /// Системные константы.
    /// </summary>
    struct SystemConstants
    {
        public const string SystemFile = "VMFS.bin";

        public const ushort MftRecordSize = 1024; // Фиксированный размер записи в MFT.

        // Фиксированные номера метафайлов в MFT.
        public const byte MftRecNumber = 0;
        public const byte MftMirrRecNumber = 1;
        public const byte VolumeRecNumber = 2;
        public const byte RootDirectoryRecNumber = 3;
        public const byte BitmapRecNumber = 4;
        public const byte UserListRecNumber = 5;
        public const byte ServiceFileCount = 6;
    }
}
