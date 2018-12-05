﻿namespace MyOS
{
    /// <summary>
    /// Системные константы.
    /// </summary>
    struct SystemConstants
    {
        public const string SystemFile = "VMFS.bin";

        public const ushort MftRecordSize = 1024; // Фиксированный размер записи в MFT.
        public const ushort MftHeaderLength = 51; // Размер заголовка записи в MFT (информация о файле, без данных).
        public const ushort RootRecordLength = 43;

        public const byte MftRecNumber = 0;
        public const byte MftMirrRecNumber = 1;
        public const byte VolumeRecNumber = 2;
        public const byte RootDirectoryRecNumber = 3;
        public const byte BitmapRecNumber = 4;
        public const byte UserListRecNumber = 5;
        public const byte ServiceFileCount = 6;

        public enum ClusterState : byte
        {
            Free = 0,
            Damaged = 1,
            Service = 0b10,
            Busy = 0b11
        }
    }
}
