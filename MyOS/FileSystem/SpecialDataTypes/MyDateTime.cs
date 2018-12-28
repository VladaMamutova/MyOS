using System;

namespace MyOS.FileSystem.SpecialDataTypes
{
    /// <summary>
    /// Представляет 5-байтную пользовательскую структуру даты и времени.
    /// </summary>
    public struct MyDateTime
    {
        public byte[] DateTimeBytes;
        public static readonly MyDateTime MinValue = new MyDateTime(new byte[5]);

        public MyDateTime(byte[] dateTimeBytes)
        {
            DateTimeBytes = new byte[5];
            if (dateTimeBytes.Length == 5)
                DateTimeBytes = dateTimeBytes;
        }

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

            // Сдвигаем число на 4 бита влево, освобождая конечные биты под компонент месяца даты,
            // и записываем компонент месяца в младшие 4 разряда.
            ulongDateTime = ulongDateTime << 4 | (uint)dateTime.Month;

            ulongDateTime = ulongDateTime << 10 | (uint) (dateTime.Year - 2000);
            ulongDateTime = ulongDateTime << 5 | (uint)dateTime.Hour;
            ulongDateTime = ulongDateTime << 6 | (uint)dateTime.Minute;
            ulongDateTime = ulongDateTime << 6 | (uint)dateTime.Second;

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

        public int GetUtcHours() => (int)(GetUlongTimeDate() >> 36 & 0xF);
        public string GetStringUtcHours() => "UTC +" + GetUtcHours() + " часа";
        public int GetDay() => (int)(GetUlongTimeDate() >> 31 & 0x1F);
        public int GetMonth() => (int)(GetUlongTimeDate() >> 27 & 0xF);
        public int GetYear() => (int)(GetUlongTimeDate() >> 17 & 0x3FF) + 2000;
        public int GetHour() => (int)(GetUlongTimeDate() >> 12 & 0x1F);
        public int GetMinute() => (int)(GetUlongTimeDate() >> 6 & 0x3F);
        public int GetSecond() => (int)(GetUlongTimeDate() & 0x3F);

        public override string ToString()
        {
            return GetDate() + $" {GetHour():d2}:{GetMinute():d2}";
        }

        public string GetFullDateTime()
        {
            return $"{GetDay():d2}.{GetMonth():d2}.{GetYear()} {GetHour():d2}:{GetMinute():d2}:{GetSecond():d2}";
        }

        public string GetDate() => $"{GetDay():d2}.{GetMonth():d2}.{GetYear()}";

        public string GetTime() => $"{GetHour():d2}:{GetMinute():d2}:{GetSecond():d2}";
    }
}
