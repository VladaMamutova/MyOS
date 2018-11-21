using System;

namespace MyOS
{
    /// <summary>
    /// Класс с определением структур для служебных файлов.
    /// </summary>
    static class SystemStructures
    {
        /// <summary>
        /// Системные константы.
        /// </summary>
        public struct Constants
        {
            public const ushort MftRecFixSize = 1024; // Фиксированный размер записи в MFT.
            public const ushort MftHeaderLength = 51; // Размер заголовка записи в MFT (информация о файле, без данных).

            public const byte MftRecNumber = 1;
            public const byte MftMirrRecNumber = 2;
            public const byte VolumeRecNumber = 3;
            public const byte RootDirectoryRecNumber = 4;
            public const byte BitmapRecNumber = 5;
            public const byte UserListRecNumber = 6;

            public const int VolumeSize = 419430400; // Размер раздела с операционной системой.
            public const int MftAreaSize = 41943040; // Размер MFT-пространства (10% от всего размера).
            public const int RootSize = 409600; // Размер корневого каталога (100 кластеров * 4096 байтов).
            public const int BitmapSize = 25600; // Размер битовой карты (102 400 кластеров * 2 бита / 8 битов в байте = 25600 байтов).
            public const ushort BytesPerClus = 4096; // Размер кластера в байтах.
            //public const int ClusterCount = 102400;
            //public const int FreeClusters = 92160;

            public const byte AdminUid = 1; // Идентификатор администратора.
            public const byte UserRecSize = 193; // Размер заявки пользователя в списке пользователей.
            public const byte VolumeNameSize = 1;
            public const byte FsVersionSize = 9;
            public const byte StateSize = 1;
        }

        /// <summary>
        /// Атрибут Data записей MFT.
        /// </summary>
        public struct Data
        {
            public byte Header;
            public int[] Blocks;

            public Data(int[] blocks)
            {
                Header = (byte)(blocks == null ? 0 : 1);
                Blocks = blocks;
            }
        }

        /// <summary>
        /// Представляет 5-байтную пользовательскую структуру даты и времени.
        /// </summary>
        public struct MyDateTime
        {
            public byte[] DateTimeBytes;
            
            public MyDateTime(DateTime dateTime)
            {
                DateTimeBytes = new byte[5];

                uint utc = (uint) (dateTime.Hour - DateTime.UtcNow.Hour);
                // Записываем разницу часов текущего времени от всемирного
                // координированного времени в последние четыре бита.
                ulong ulongDateTime = utc;

                // Сдвигаем число на 5 битов влево, освобождая конечные биты под компонент дня даты.
                ulongDateTime = ulongDateTime << 5;
                // Записываем компонент дня в младшие 5 разрядов.
                ulongDateTime = ulongDateTime | (uint) dateTime.Day;

                // Сдвигаем число на 4 бита влево, освобождая конечные биты под компонент месяца даты.
                ulongDateTime = ulongDateTime << 4;
                // Записываем компонент месяца в младшие 4 разряда.
                ulongDateTime = ulongDateTime | (uint) dateTime.Month;

                ulongDateTime = ulongDateTime << 10;
                ulongDateTime = ulongDateTime | (uint) (dateTime.Year - 2000);

                ulongDateTime = ulongDateTime << 5;
                ulongDateTime = ulongDateTime | (uint) dateTime.Hour;

                ulongDateTime = ulongDateTime << 6;
                ulongDateTime = ulongDateTime | (uint) dateTime.Minute;

                ulongDateTime = ulongDateTime << 6;
                ulongDateTime = ulongDateTime | (uint) dateTime.Second;

                byte[] longDateTimeBytes = BitConverter.GetBytes(ulongDateTime);

                for (int i = 0; i < 5; i++)
                    DateTimeBytes[i] = longDateTimeBytes[i];
            }

            private ulong GetUlongTimeDate()
            {
                byte[] ulongDateTimeBytes = new byte[8];
                DateTimeBytes.CopyTo(ulongDateTimeBytes, 0);
                return BitConverter.ToUInt64(ulongDateTimeBytes, 0);
            }

            public int GetUtcHours() => (int) (GetUlongTimeDate() >> 36 & 0xF);
            public string GetStringUtcHours() => "UTC +" + GetUtcHours() + " часа";
            public int GetDay() => (int) (GetUlongTimeDate() >> 31 & 0x1F);
            public int GetMonth() => (int) (GetUlongTimeDate() >> 27 & 0xF);
            public int GetYear() => (int) (GetUlongTimeDate() >> 17 & 0x3FF) + 2000;
            public int GetHour() => (int) (GetUlongTimeDate() >> 12 & 0x1F);
            public int GetMinute() => (int) (GetUlongTimeDate() >> 6 & 0x3F);
            public int GetSecond() => (int) (GetUlongTimeDate() & 0x3F);

            public override string ToString()
            {
                return $"{GetDay():d2}.{GetMonth():d2}.{GetYear()} {GetHour():d2}:{GetMinute():d2}:{GetSecond():d2}";
            }
        }
    }
}
