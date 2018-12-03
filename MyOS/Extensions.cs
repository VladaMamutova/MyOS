using System;
using System.Text;

namespace MyOS
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

        public static int ToInt32(this byte[] array)
        {
            if (array.Length != 4) return 0;
            return (array[3] << 24) + (array[2] << 16) + (array[1] << 8) + array[0];
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
            resourceBytes.CopyTo(resultBytes, 0);
            return resultBytes;
        }

        public static byte GetClusterInfoByte(this byte[] clustersInfo)
        {
            byte infoByte = 0;
            if (clustersInfo.Length != 4) return 0;
            // Заполняем побитово байт с информацией о кластерах.
            // Порядок кластеров - обратный, для удобства считывания.
            for (int i = clustersInfo.Length - 1; i >= 0; i--)
            {
                // В последние два бита записываем информацию о кластере.
                infoByte = (byte)(infoByte | clustersInfo[i]);
                // Сдвигаем байт влево на два бита, осаобождая место под информацию о следующем кластере.
                if (i != 0) infoByte = (byte)(infoByte << 2);
            }

            return infoByte;
        }
    }
}
