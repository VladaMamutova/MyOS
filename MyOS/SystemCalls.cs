using System;
using System.Collections.Generic;
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

        #region Метод форматирования и поддерживающие его методы.
        
        public static void Formatting()
        {
            using (var fileStream = new FileStream("MyOS.txt", FileMode.Create, FileAccess.Write, FileShare.None))
                fileStream.SetLength(Constants.VolumeSize); // Устанавливаем размер файла в 400 Мб.
            

            BinaryWriter bw = new BinaryWriter(File.Open("MyOS.txt", FileMode.Open));
            MyDateTime nowDateTime = new MyDateTime(DateTime.Now);

            #region Форматирование MFT-пространства.

            // Создание файла $MFT, представляющего централизованный каталог
            // всех остальных файлов диска и себя самого.
            // Первые 6 записей - служебные.
            // 1 файл - запись о самом MFT.
            // 2 файл - запись о копии первых записей MFT.
            // 3 файл - $Volume.
            // 4 файл - $. (корневой каталог).
            // 5 файл - $Bitmap (битовая карта).
            // 6 файл - $Users (список пользователей системы).

            MftRecord mft = new MftRecord
            {
                Sign = MftRecord.Signature.InUse, // Признак - запись используется.
                Attributes = 0b11, // Системный файл, только для чтения.
                Extension = "",
                Size = 38 * 1024, // Размер MFT. При форматировании по умолчанию создаётся 38 записей MFT по 1024 Б.
                CreationDate = nowDateTime,
                ModificationDate = nowDateTime,
                UserId = Constants.AdminUid,
                FileName = "$MFT",
                SecurityDescriptor = 6,
                DataAtr = new Data()
            };
            MftRecord mftMirr = new MftRecord
            {
                Sign = MftRecord.Signature.InUse, // Признак - запись используется.
                Attributes = 0b11, // Системный файл, только для чтения.
                Extension = "",
                Size = 6144, // Размер MFTMirrow. Всего 6 служебных записей MFT * 1024 = 6144 байт.
                CreationDate = nowDateTime,
                ModificationDate = nowDateTime,
                UserId = Constants.AdminUid,
                FileName = "$MFTMirr",
                SecurityDescriptor = 7,
                DataAtr = new Data()
            };
            MftRecord volume = new MftRecord
            {
                Sign = MftRecord.Signature.InUse, // Признак - запись используется.
                Attributes = 0b11, // Системный файл, только для чтения.
                Extension = "",
                Size = 11,
                CreationDate = nowDateTime,
                ModificationDate = nowDateTime,
                UserId = Constants.AdminUid,
                FileName = "$Volume",
                SecurityDescriptor = 8,
                DataAtr = new Data()
            };
            MftRecord rootDirectory = new MftRecord
            {
                Sign = MftRecord.Signature.InUse, // Признак - запись используется.
                Attributes = 0b1000, // Директория.
                Extension = "",
                Size = 0,
                CreationDate = nowDateTime,
                ModificationDate = nowDateTime,
                UserId = Constants.AdminUid,
                FileName = "$.",
                SecurityDescriptor = 9,
                DataAtr = new Data()
            };
            int[] dataBlocks = new int[Constants.BitmapSize / Constants.MftRecFixSize + 1]; // 
            for (int i = 0; i < dataBlocks.Length; i++)
                dataBlocks[i] = i + 11;
            MftRecord bitmap = new MftRecord
            {
                Sign = MftRecord.Signature.InUse, // Признак - запись используется.
                Attributes = 0b11, // Системный файл, только для чтения.
                Extension = "",
                Size = Constants.BitmapSize,
                CreationDate = nowDateTime,
                ModificationDate = nowDateTime,
                UserId = Constants.AdminUid,
                FileName = "$Bitmap",
                SecurityDescriptor = 10,
                DataAtr = new Data(dataBlocks)
            };
           MftRecord usersRecord = new MftRecord
            {
                Sign = MftRecord.Signature.InUse, // Признак - запись используется.
                Attributes = 0b11, // Системный файл, только для чтения.
                Extension = "",
                Size = 193, // Размер одной пользовательской записи, представляющей информацию об администраторе.
                CreationDate = nowDateTime,
                ModificationDate = nowDateTime,
                UserId = Constants.AdminUid,
                FileName = "$Users",
                SecurityDescriptor = 38,
                DataAtr = new Data()
            };

            
            #region Записываем служебные записи в главную файловую таблицу MFT.

            WriteMftRecordToFile(bw, mft, Constants.MftRecNumber);
            WriteMftRecordToFile(bw, mftMirr, Constants.MftMirrRecNumber);
            WriteMftRecordToFile(bw, volume, Constants.VolumeRecNumber);
            WriteVolumeDataToFile(bw); // Записываем данные в запись $Volume.
            WriteMftRecordToFile(bw, rootDirectory, Constants.RootDirectoryRecNumber);
            WriteMftRecordToFile(bw, bitmap, Constants.BitmapRecNumber);
            WriteBitmapDataToFile(bw, bitmap); // Записываем данные битовой карты $Bitmap.
            WriteMftRecordToFile(bw, usersRecord, Constants.UserListRecNumber);
            WriteUsersDataToFile(bw); // Записываем данные (список пользователей) в запись $Users.

            #endregion

            #region Записываем в файл MFT списки управления доступом для всех служебных файлов.

            WriteAccessControlListToFile(bw, 6);
            WriteAccessControlListToFile(bw, 7);
            WriteAccessControlListToFile(bw, 8);
            WriteAccessControlListToFile(bw, 9);
            WriteAccessControlListToFile(bw, 10);
            WriteAccessControlListToFile(bw, 38);

            #endregion

            #region Записываем копии служебных записей MFT посередине диска.

            WriteMftRecordToFile(bw, mft, Constants.MftRecNumber, true);
            WriteMftRecordToFile(bw, mftMirr, Constants.MftMirrRecNumber, true);
            WriteMftRecordToFile(bw, volume, Constants.VolumeRecNumber, true);
            WriteMftRecordToFile(bw, rootDirectory, Constants.RootDirectoryRecNumber, true);
            WriteMftRecordToFile(bw, bitmap, Constants.BitmapRecNumber, true);
            WriteMftRecordToFile(bw, usersRecord, Constants.UserListRecNumber, true);

            #endregion

            #endregion

            bw.Close();

            MessageBox.Show("Диск отформатирован!");
        }

        static void WriteMftRecordToFile(BinaryWriter bw, MftRecord record, int recordNumber, bool mftMirrow = false)
        {
            if (!mftMirrow) // Если данная запись является зеркальным отображением слежебной записи mft,
                // дополнительно смещаемся на середину диска для записи копии.
                bw.BaseStream.Seek((recordNumber - 1) * Constants.MftRecFixSize, SeekOrigin.Begin);
            else bw.BaseStream.Seek(Constants.VolumeSize / 2 + (recordNumber - 1) * Constants.MftRecFixSize, SeekOrigin.Begin);

            bw.Write((byte) record.Sign);
            bw.Write(record.Attributes);
            bw.Write(GetFormatBytes(record.Extension, 5));
            bw.Write(BitConverter.GetBytes(record.Size));
            bw.Write(record.CreationDate.DateTimeBytes);
            bw.Write(record.ModificationDate.DateTimeBytes);
            bw.Write(BitConverter.GetBytes(record.UserId));
            bw.Write(GetFormatBytes(record.FileName, 25));
            bw.Write(BitConverter.GetBytes(record.SecurityDescriptor));

            bw.Write(record.DataAtr.Header);
            for (int i = 0; i < record.DataAtr.Blocks?.Length; i++)
                bw.Write(BitConverter.GetBytes(record.DataAtr.Blocks[i]));
            
        }

        static void WriteVolumeDataToFile(BinaryWriter bw)
        {
            bw.BaseStream.Seek((Constants.VolumeRecNumber - 1) * Constants.MftRecFixSize + Constants.MftHeaderLength + 1,
                SeekOrigin.Begin);
            bw.Write(GetFormatBytes("H", Constants.VolumeNameSize));
            bw.Write(GetFormatBytes("VMFS v1.0", Constants.FsVersionSize));
            bw.Write(GetFormatBytes("0", Constants.StateSize));
        }

        static void WriteUsersDataToFile(BinaryWriter bw)
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
            byte service = 0b10; // Служебный кластер.
            byte free = 0b0; // Свободный кластер.
            
            int mftClusterСount = Constants.MftAreaSize / Constants.BytesPerClus;
            int clusterCount = Constants.VolumeSize / Constants.BytesPerClus;
            int currentCluster = 1;

            foreach (var record in bitmap.DataAtr.Blocks)
            {
                bw.BaseStream.Seek((record - 1) * Constants.MftRecFixSize, SeekOrigin.Begin);
                bw.Write((byte)MftRecord.Signature.IsData); // Записываем признак записи MFT.
                for (int i = 0; i < Constants.MftRecFixSize - 1; i++) // -1, т.к. 1 байт уже выделен под признак записи MFT).
                {
                    // Кластер представляется 2 битами, значит в байте - информация о 4 кластерах.
                    byte[] clustersInfo = new byte[4]; // Массив, представляющий информацию о 4 кластерах.
                    
                    for (int j = 0; j < 4; j++)
                    {
                        // Если в битовую карту записана информация не о всех кластерах, продолжаем заполнять байты.
                        if (currentCluster <= clusterCount)
                        {
                            clustersInfo[j] = currentCluster <= mftClusterСount ||
                                              currentCluster == clusterCount / 2 ||
                                              currentCluster == clusterCount / 2 + 1
                                ? service
                                : free;
                            if (currentCluster == 10241) clustersInfo[j] = 0b11;
                            currentCluster++;
                        }
                    }
                    bw.Write(GetClusterInfoByte(clustersInfo));
                }
            }
        }

        static void WriteAccessControlListToFile(BinaryWriter bw, int recordNumber)
        {
            bw.BaseStream.Seek((recordNumber - 1) * Constants.MftRecFixSize, SeekOrigin.Begin);
            bw.Write((byte)MftRecord.Signature.IsAccessControlList);
            bw.Write(accessControlList.ToBytes());
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

        static byte GetClusterInfoByte(byte[] clustersInfo)
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

        #endregion

        public static bool HasFreeMemory()
        {
            using (BinaryReader br = new BinaryReader(File.Open("MyOS.txt", FileMode.Open)))
            {
                // Смещаемся на поле данных (на атрибут Data) записи $Bitmap.
                br.BaseStream.Seek(
                    (Constants.BitmapRecNumber - 1) * Constants.MftRecFixSize + Constants.MftHeaderLength + 1,
                    SeekOrigin.Begin);

                if (br.ReadByte() != 1) return false;

                List<int>
                    recordNumbers =
                        new List<int>(); // Список номеров записей MFT, в которых содержатся данные битовой карты.
                int recNumber;
                while ((recNumber = br.ReadInt32()) != 0)
                    recordNumbers.Add(recNumber);

                Data bitmapData = new Data(recordNumbers.ToArray());
                int byteNumber = 0; // Количество прочитанных байт битовой карты.

                foreach (var recordNumber in bitmapData.Blocks)
                {
                    br.BaseStream.Seek((recordNumber - 1) * Constants.MftRecFixSize, SeekOrigin.Begin);
                    if (br.ReadByte() != (byte) MftRecord.Signature.IsData) return false;

                    for (int i = 0; i < Constants.MftRecFixSize - 1; i++) // -1 байт для заголовка Data
                    {
                        if (byteNumber >= Constants.BitmapSize) return false;

                        byte fourClustersInfo = br.ReadByte();
                        byteNumber++;
                        // Кластер представляется 2 битами, значит в байте - информация о 4 кластерах.
                        for (int j = 0; j < 4; j++)
                        {
                            // Обнуляем первые шесть битов, при этом получая 2 младших бита,
                            // представляющих информацию об одном кластере.
                            if ((fourClustersInfo & 0b00000011) == 0) return true; // Кластер свободный.
                            // Сдвигаемся на 2 бита вправо для получения информации о следующем кластере.
                            fourClustersInfo = (byte) (fourClustersInfo >> 2);
                        }
                    }
                }

                return false;
            }
        }

        public static void CreateFile()
        {
            // Проверяем, есть ли место на диске для создания нового файла.
            HasFreeMemory();
        }

    }
}
