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
            public const ushort ClusterSize = 4096; // Размер кластера.
            public const ushort MftRecLength = 51; // Размер записи MFT.

            public const int MftSize = 20971520; // Размер MFT, который занимает 5% от общегоразмера зоны MFT.
            public const ushort FixFileLength = 1024; // Фиксированный размер файлов.

            public const byte MftRecNumber = 0;
            public const int MftRecSize = 10240; // MFT-файл занимает 5% от общей зоны MFT.
            public const byte VolumeRecNumber = 1;
            public const byte RootDirectoryRecNumber = 2;
            public const byte BitmapRecNumber = 3;
            public const byte UserListRecNumber = 4;

            public const int RootSize = 409600; // Размер корневого каталога (100 кластеров * 4096 байтов).
            public const int BitmapSize = 25600; // Размер битовой карты (102 400 класетров * 2 бита / 8 битов в байте = 25600 байтов).

            public const byte NameLength = 1;
            public const string FsVersion = "VMFS v1.0"; // Имя файловой системы.
            public const byte VersionLength = 9;
            public const byte StateLength = 1;

            public const byte AdminUid = 0; // Идентификатор администратора.

            #region Константы
            //private const ushort LOGIN_LENGTH = 20; // Длина логина учетной записи.
            ///*Пароль учетной записи администратора после форматирования в хеш*/
            //private const ushort ADMIN_UID = 1;
            //private const string ADMIN_NAME = "admin"; // Логин учетной записи администратора.
            //private const string ADMIN_PASSWORD_HASH = "21232f297a57a5a743894a0e4a801fc3";

            ///*Длина пароля учетной записи*/
            //private const ushort PASSWORD_LENGTH = 32;
            ///*Идентификатор группы учетной записи*/
            //private const ushort ADMIN_GID = 1;
            ///*Имя группы администратора после форматирования*/
            //private const string ADMIN_GROUP_NAME = "admin";
            ///*Длина имени группы учетной записи*/
            //private const ushort GROUP_NAME_LENGTH = 10;
            ///*Права администратора*/
            //private const string ADMIN_ORDERS = "a";
            ///*Длина прав*/
            //private const ushort ORDERS_LENGTH = 1;

            //private const ushort USERS_TABLE_FIRST_CLUSTER = 1;
            #endregion
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

                uint utc = (uint)(dateTime.Hour - DateTime.UtcNow.Hour);
                // Записываем разницу часов текущего времени от всемирного
                // координированного времени в последние четыре бита.
                ulong ulongDateTime = utc;

                // Сдвигаем число на 5 битов влево, освобождая конечные биты под компонент дня даты.
                ulongDateTime = ulongDateTime << 5;
                // Записываем компонент дня в младшие 5 разрядов.
                ulongDateTime = ulongDateTime | (uint)dateTime.Day;

                // Сдвигаем число на 4 бита влево, освобождая конечные биты под компонент месяца даты.
                ulongDateTime = ulongDateTime << 4;
                // Записываем компонент месяца в младшие 4 разряда.
                ulongDateTime = ulongDateTime | (uint)dateTime.Month;

                ulongDateTime = ulongDateTime << 10;
                ulongDateTime = ulongDateTime | (uint)(dateTime.Year - 2000);

                ulongDateTime = ulongDateTime << 5;
                ulongDateTime = ulongDateTime | (uint)dateTime.Hour;

                ulongDateTime = ulongDateTime << 6;
                ulongDateTime = ulongDateTime | (uint)dateTime.Minute;

                ulongDateTime = ulongDateTime << 6;
                ulongDateTime = ulongDateTime | (uint)dateTime.Second;

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

            public int GetUtcHours() => (int)(GetUlongTimeDate() >> 36) & 0xF;
            public string GetStringUtcHours() => "UTC +" + GetUtcHours() + " часа";
            public int GetDay() => (int)(GetUlongTimeDate() >> 31) & 0x1F;
            public int GetMonth() => (int)(GetUlongTimeDate() >> 27) & 0xF;
            public int GetYear() => (int)((GetUlongTimeDate() >> 17) & 0x3FF) + 2000;
            public int GetHour() => (int)(GetUlongTimeDate() >> 12) & 0x1F;
            public int GetMinute() => (int)(GetUlongTimeDate() >> 6) & 0x3F;
            public int GetSecond() => (int)GetUlongTimeDate() & 0x3F;

            public override string ToString()
            {
                return $"{GetDay():d2}.{GetMonth():d2}.{GetYear()} {GetHour():d2}:{GetMinute():d2}:{GetSecond():d2}";
            }

            //void convert(int value, int size)
            //{
                //uint ihour = (uint)dateTime.Hour;
                //uint jhour = ihour & 0x1F; // проверяем, помещается ли в 5 битов значение часа
                //ulong longDateTime = 0;
                //if (ihour == jhour)
                //    longDateTime = ihour << 6; // освобождаем место под минуты 

                //uint imin = (uint)dateTime.Minute;
                //uint jmin = imin & 0x3F; // проверяем, помещается ли в 5 битов значение часа
                //if (imin == jmin)
                //    longDateTime = longDateTime | imin; 
            //}
        }

        /// <summary>
        /// Запись MFT.
        /// </summary>
        //public struct MFT_RECORD
        //{
        //    // Размер каждой записи - фиксированный, равен 1024 байт.

        //    // Перечисление признаков записи.
        //    enum Signature : byte
        //    {
        //        NOT_USED = 0,
        //        IN_USE = 1,
        //        IS_ACCESS_CONTROL_LIST = 2,
        //        IS_DATA = 3
        //    }

        //    Signature signature; // Признак записи.
        //    private byte attributes; // Атрибуты.
        //    private string extension; // Расширение файла.
        //    private int size; // Размер.
        //    private short creatDate; // Дата создания файла.
        //    private short modifDate; // Дата последней модификации файла.
        //    private short userID; // Уникальный идентификатор владельца файла.
        //    private string fileName; // Имя файла.
        //    private int securityDescriptor; // Дескриптор безопасности.

        //    // Размер информации о файле - 51 байт

        //    // Под данные файла (под Data) отводится 973 байт (1024 - 51).
        //}

        ///// <summary>
        ///// Служебная информация (метка тома, версия ФС, состояние тома).
        ///// </summary>
        //public struct VOLUME
        //{
        //    private string name; // Метка тома.
        //    private string version; // Версия ФС.
        //    private byte state; // Состояние тома.
            
        //    // Размер - 10 байт.
        //}

        ///// <summary>
        ///// Запись корневого каталога.
        ///// </summary>
        //public struct ROOT_RECORD
        //{
        //    private string fileName; // Имя файла.
        //    private byte attributes; // Атрибуты.
        //    private string extension; // Расширение.
        //    private int size; // Размер файла.
        //    private short modifDate; // Дата последней модификации.
        //    private int number; // Номер записи в MFT.

        //    // Размер - 46 байт.
        //}

        /// <summary>
        /// Запись в списке пользователей.
        /// </summary>
        //public struct USERS
        //{
        //    private string login; // Логин.
        //    private string name; // Имя пользователя.
        //    private byte UID; // Уникаотный идентификатор.
        //    private string password; // Пароль.
        //    private string salt; // Соль для пароля.
        //    private string homeDirectory; // Домашняя директория.

        //    // Размер - 193 байт.
        //}
    }
}
