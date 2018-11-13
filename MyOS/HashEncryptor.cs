using System;
using System.Security.Cryptography;
using System.Text;

namespace MyOS
{
    static class HashEncryptor
    {
        /// <summary>
        /// Генерирует случайную соль.
        /// </summary>
        /// <returns>Случайная соль.</returns>
        public static string GenerateSalt()
        {
            byte[] data = new byte[0x10];
            // Криптографический генератор случайных чисел заполняет солевой массив.
            new RNGCryptoServiceProvider().GetBytes(data);
            return Convert.ToBase64String(data);
        }

        private static readonly HashAlgorithm Hash = HashAlgorithm.Create("SHA512");

        /// <summary>
        /// Шифрует пароль с указанной солью.
        /// </summary>
        /// <param name="password"></param>
        /// <param name="salt"></param>
        /// <returns></returns>
        public static string EncodePassword(string password, string salt)
        {
            // Получаем массивы байтов из строки пароля и соли.
            byte[] passwordBytes = Encoding.UTF8.GetBytes(password);
            byte[] saltBytes = Convert.FromBase64String(salt);

            // Создаём результирующий массив, содержащий пароль и соль.
            byte[] passwordWithSaltBytes = new byte[passwordBytes.Length + saltBytes.Length];

            // Копируем байты пароля и соли в результирующий массив.
            Buffer.BlockCopy(passwordBytes, 0, passwordWithSaltBytes, 0, passwordBytes.Length);
            Buffer.BlockCopy(saltBytes, 0, passwordWithSaltBytes, passwordBytes.Length, saltBytes.Length);

            // Вычисляем хэш-значение пароля с добавлением соли.
            byte[] hashBytes = Hash.ComputeHash(passwordWithSaltBytes);

            // Возвращаем сконвертированный результат в base64-encoded string.
            return Convert.ToBase64String(hashBytes);
        }

        /// <summary>
        /// Определяет, равны ли два указанных массива байтов. Время метода не зависит от проверяемых значений.
        /// </summary>
        /// <param name="a">Первый сравниваемый массив.</param>
        /// <param name="b">Второй сравниваемый массив.</param>
        /// <returns>Логическое значение, указывающее равен ли пароль.</returns>
        public static bool SlowEquals(byte[] a, byte[] b)
        {
            if (a == null || b == null) return false;
            var difference = a.Length ^ b.Length;
            for (var i = 0; i < a.Length && i < b.Length; i++)
                difference |= a[i] ^ b[i];
            return difference == 0;
        }

        /// <summary>
        /// Кодирует все символы заданной строки в последовательность байтов в кодировке UTF8.
        /// </summary>
        /// <param name="value">Строка для кодирования.</param>
        /// <returns>Закодированная в массив байтов строка.</returns>
        public static byte[] ToBytes(this string value) => Encoding.UTF8.GetBytes(value);
    }
}
