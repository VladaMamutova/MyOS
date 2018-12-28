using System;
using System.Security.Cryptography;
using System.Text;

namespace MyOS.FileSystem.SpecialDataTypes
{
    static class Extensions
    {
        /// <summary>
        /// Возвращает диапазон байтов массива.
        /// </summary>
        /// <param name="array">Массив, из которого следует получить диапазон байтов.</param>
        /// <param name="startIndex">Начальный индекс диапазона.</param>
        /// <param name="count">Количество байтов диапазона.</param>
        /// <returns></returns>
        public static byte[] GetRange(this byte[] array, int startIndex, int count)
        {
            byte[] arrayRange = new byte[count];
            if (startIndex < 0 || array.Length < startIndex + count) return arrayRange;

            for (int i = 0; i < count; i++)
                arrayRange[i] = array[startIndex + i];
            return arrayRange;
        }

        /// <summary>
        /// Возвращает массив байтов заданной длины, представляющий коды символов переданной строки.
        /// </summary>
        /// <param name="source">Строка, содержащая символы для кодирования.</param>
        /// <param name="resultSize">Размер результирующего массива байтов, должен быть
        /// больше либо равен количеству символов в строке для кодирования.</param>
        /// <returns></returns>
        public static byte[] GetFormatBytes(this string source, int resultSize)
        {
            if (source == null) return new byte[0];
            byte[] resultBytes = new byte[resultSize];
            byte[] resourceBytes = Encoding.UTF8.GetBytes(source);
            if (resultSize < resourceBytes.Length) Array.Copy(resourceBytes, resultBytes, resultSize);
            else resourceBytes.CopyTo(resultBytes, 0);
            return resultBytes;
        }

        /// <summary>
        /// Шифрует пароль, хещируя его с помощью алгоритма SHA512.
        /// </summary>
        /// <param name="password"></param>
        /// <returns></returns>
        public static byte[] ComputeHash(this string password)
        {
            return SHA512.Create().ComputeHash(Encoding.UTF8.GetBytes(password));
        }

        public static void ParseFullName(this string fullName, bool isDirectory, out string name, out string extension)
        {
            if (isDirectory)
                name = fullName;
            else
            {
                if (fullName.Contains("."))
                    name = fullName.Substring(0, fullName.LastIndexOf('.'));
                else name = fullName;
            }

            extension = name.Length + 1 < fullName.Length ? fullName.Substring(name.Length + 1) : "";
        }
    }
}
