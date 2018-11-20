using System;
using System.IO;
using System.Text;
using System.Windows;
using static MyOS.SystemStructures;

namespace MyOS
{
    class SystemCalls
    {
        private static Users users;
        private static AccessControlList accessControlList;

        static SystemCalls()
        {
            users = new Users(); // По умолчанию создаётся список с администратором.
            accessControlList = new AccessControlList();
        }

        public static void Formatting()
        {
            using (var fileStream = new FileStream("MyOS.txt", FileMode.Create, FileAccess.Write, FileShare.None))
            {
                fileStream.SetLength(419430400); // Устанавливаем размер файла в 400 Мб.
            }

            BinaryWriter bw = new BinaryWriter(File.Open("MyOS.txt", FileMode.Open));
            MyDateTime nowDateTime = new MyDateTime(DateTime.Now);
           
            #region Форматирование MFT-пространства.

            // Создание файла $MFT, представляющего централизованный каталог
            // всех остальных файлов диска и себя самого.
            // Первые 5 записей - служебные.

            // 1 файл - запись о самом MFT.
            MftRecord mft = new MftRecord
            {
                Sign = MftRecord.Signature.InUse, // Признак - запись используется.
                Attributes = 0b11, // Системный файл, только для чтения.
                Extension = "",
                Size = 1024, // Размер MFT. Пока 51 байт под данную запись, потом обновить!!!
                CreatDate = nowDateTime,
                ModifDate = nowDateTime,
                UserId = Constants.AdminUid,
                FileName = "$MFT",
                SecurityDescriptor = 6,
                DataAtr = new Data()
            };
            WriteMftRecordToFile(bw, mft, Constants.MftRecNumber);
            WriteAccessControlListToFile(bw, 6);

            // 2 файл - $Volume.
            MftRecord volume = new MftRecord
            {
                Sign = MftRecord.Signature.InUse, // Признак - запись используется.
                Attributes = 0b11, // Системный файл, только для чтения.
                Extension = "",
                Size = 11,
                CreatDate = nowDateTime,
                ModifDate = nowDateTime,
                UserId = Constants.AdminUid,
                FileName = "$Volume",
                SecurityDescriptor = 7,
                DataAtr = new Data()
            };
            WriteMftRecordToFile(bw, volume, Constants.VolumeRecNumber);
            // Записываем в файл данные файла $Volume.
            bw.Write(GetFormatBytes("H", Constants.volumeNameSize));
            bw.Write(GetFormatBytes("VMFS v1.0", Constants.fsVersionSize));
            bw.Write(GetFormatBytes("0", Constants.stateSize));
            WriteAccessControlListToFile(bw, 7);

            // 3 файл - . (корневой каталог).
            MftRecord rootDirectory = new MftRecord
            {
                Sign = MftRecord.Signature.InUse, // Признак - запись используется.
                Attributes = 0b1000, // Директория.
                Extension = "",
                Size = 0,
                CreatDate = nowDateTime,
                ModifDate = nowDateTime,
                UserId = Constants.AdminUid,
                FileName = ".",
                SecurityDescriptor = 8,
                DataAtr = new Data()
            };
            WriteMftRecordToFile(bw, rootDirectory, Constants.RootDirectoryRecNumber);
            WriteAccessControlListToFile(bw, 8);

            // 4 файл - Bitmap (битовая карта).
            MftRecord bitmap = new MftRecord
            {
                Sign = MftRecord.Signature.InUse, // Признак - запись используется.
                Attributes = 0b11, // Системный файл, только для чтения.
                Extension = "",
                Size = Constants.BitmapSize,
                CreatDate = nowDateTime,
                ModifDate = nowDateTime,
                UserId = Constants.AdminUid,
                FileName = "$Bitmap",
                SecurityDescriptor = 9,
                DataAtr = new Data(new[] {new Data.ExtentPointer(10, 26)})
            };
            WriteMftRecordToFile(bw, bitmap, Constants.BitmapRecNumber);
            WriteAccessControlListToFile(bw, 9);

            // 5 файл - $Users (список пользователей системы).
            MftRecord usersRecord = new MftRecord
            {
                Sign = MftRecord.Signature.InUse, // Признак - запись используется.
                Attributes = 0b11, // Системный файл, только для чтения.
                Extension = "",
                Size = 193, // Размер одной пользовательской записи, представляющей информацию об администраторе.
                CreatDate = nowDateTime,
                ModifDate = nowDateTime,
                UserId = Constants.AdminUid,
                FileName = "$Users",
                SecurityDescriptor = 37,
                DataAtr = new Data()
            };
            // Записываем в файл данные о записи со списком пользователей.
            WriteMftRecordToFile(bw, usersRecord, Constants.UserListRecNumber);
            // Записываем в файл список пользователей.
            WriteUsersToFile(bw);
            WriteAccessControlListToFile(bw, 37);
           
            // Записываем знчения битовой карты, содержащиеся в 26 записях.
            WriteBitmapDataToFile(bw, bitmap);
            
            #endregion
            
            bw.Close();

            MessageBox.Show("Диск отформатирован!");
        }

        static void WriteMftRecordToFile(BinaryWriter bw, MftRecord record, int recordNumber)
        {
            bw.BaseStream.Seek((recordNumber - 1) * Constants.MftRecFixSize, SeekOrigin.Begin);

            bw.Write((byte) record.Sign);
            bw.Write(record.Attributes);
            bw.Write(GetFormatBytes(record.Extension, 5));
            bw.Write(BitConverter.GetBytes(record.Size));
            bw.Write(record.CreatDate.DateTimeBytes);
            bw.Write(record.ModifDate.DateTimeBytes);
            bw.Write(BitConverter.GetBytes(record.UserId));
            bw.Write(GetFormatBytes(record.FileName, 25));
            bw.Write(BitConverter.GetBytes(record.SecurityDescriptor));

            bw.Write(record.DataAtr.Header);
            for (int i = 0; i < record.DataAtr.Extents?.Length; i++)
            {
                bw.Write(BitConverter.GetBytes(record.DataAtr.Extents[i].RecNumber));
                bw.Write(record.DataAtr.Extents[i].Count);
            }
        }

        static void WriteAccessControlListToFile(BinaryWriter bw, int recordNumber)
        {
            bw.BaseStream.Seek((recordNumber - 1) * Constants.MftRecFixSize, SeekOrigin.Begin);
            bw.Write((byte)MftRecord.Signature.IsAccessControlList);
            bw.Write(accessControlList.ToBytes());
        }

        static void WriteUsersToFile(BinaryWriter bw)
        {
            bw.BaseStream.Seek((Constants.UserListRecNumber - 1) * Constants.MftRecFixSize + Constants.MftHeaderLength + 1,
                SeekOrigin.Begin);

            foreach (var user in users.List)
            {
                bw.Write(GetFormatBytes(user.Login, 20));
                bw.Write(GetFormatBytes(user.Name, 30));
                bw.Write(BitConverter.GetBytes(user.Uid));
                bw.Write(Encoding.GetEncoding(1251).GetBytes(user.Salt));
                bw.Write(Encoding.GetEncoding(1251).GetBytes(user.Password));
                bw.Write(GetFormatBytes(user.HomeDirectory, 30));
            }
        }

        static void WriteBitmapDataToFile(BinaryWriter bw, MftRecord bitmap)
        {
            bw.BaseStream.Seek((bitmap.DataAtr.Extents[0].RecNumber - 1) * Constants.MftRecFixSize, SeekOrigin.Begin);
            byte serviceClusters = 0b10101010; // 4 служебных кластера в байте.
            byte freeClusters = 0; // 4 свободных кластера в байте.

            // Информация о кластере в битовой карте записывается двумя битами, поэтому
            // количество байтов, в которых будет записана информация о кластерах MFT-зоны:
            int mftBytes = Constants.MftAreaSize / Constants.BytesPerClus * 2 / 8;
            int restBitmapBytes = Constants.BitmapSize - mftBytes;

            for (int i = 0; i < 26; i++)
            {
                bw.Write((byte)MftRecord.Signature.IsData);
                for (int j = 0; j < Constants.MftRecFixSize - 1; j++) // -1 байт для заголовка Dataпризнака записи, который был записан выше
                {
                    if (mftBytes > 0) // Если не записаны все байты под MFT-зону.
                    {
                        bw.Write(serviceClusters);
                        mftBytes--;
                    }
                    else if (restBitmapBytes > 0) // Продолжаем запись, если все байты битовой карты не записаны.
                    {
                        bw.Write(freeClusters);
                        restBitmapBytes--;
                    }
                }
            }
        }
        /// <summary>
        /// Возвращает массив байтов заданной длины, в котором закодированы все символы переданной строки.
        /// </summary>
        /// <param name="source">Строка, содержащая символы для кодирования.</param>
        /// <param name="resultSize">Размер результирующего массива байтов, должен быть
        /// больше либо равен количеству символов в строке для кодирования.</param>
        /// <returns></returns>
        static byte[] GetFormatBytes(string source, int resultSize)
        {
            byte[] resultBytes = new byte[resultSize];
            byte[] resourceBytes = Encoding.GetEncoding(1251).GetBytes(source);
            if (resultSize < resourceBytes.Length) Array.Copy(resourceBytes, resultBytes, resultSize);
            resourceBytes.CopyTo(resultBytes, 0);
            return resultBytes;
        }
    }
}
