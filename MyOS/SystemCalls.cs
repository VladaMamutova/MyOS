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
        #region Метод форматирования и поддерживающие его методы.
        
        public static void Formatting()
        {
            using (var fileStream = new FileStream(Constants.SystemFile, FileMode.Create, FileAccess.Write, FileShare.None))
                fileStream.SetLength(Constants.VolumeSize); // Устанавливаем размер файла в 400 Мб.

            BinaryWriter bw = new BinaryWriter(File.Open(Constants.SystemFile, FileMode.Open));
            MyDateTime nowDateTime = new MyDateTime(DateTime.Now);
            User administrator = new User("admin", "Administrator", 0, "admin", HashEncryptor.GenerateSalt(), "."); // По умолчанию в системе создаётся администратор.
            AccessControlList accessControlList = new AccessControlList();
            accessControlList.Add(administrator.Uid, new[] { AccessControlList.Rights.F });

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
                Size = 38 * Constants.MftRecFixSize, // Размер MFT. При форматировании по умолчанию создаётся 38 записей MFT по 1024 Б.
                CreationDate = nowDateTime,
                ModificationDate = nowDateTime,
                UserId = administrator.Uid,
                FileName = "$MFT",
                SecurityDescriptor = Constants.ServiceFileCount,
                DataAttributes = new Data(GetSequenceOfDataBlocks(38, Constants.MftRecNumber))
            };
            MftRecord mftMirr = new MftRecord
            {
                Sign = MftRecord.Signature.InUse, // Признак - запись используется.
                Attributes = 0b11, // Системный файл, только для чтения.
                Extension = "",
                Size = Constants.ServiceFileCount * Constants.MftRecFixSize, // Всего 6 служебных записей MFT * 1024 = 6144 байт.
                CreationDate = nowDateTime,
                ModificationDate = nowDateTime,
                UserId = administrator.Uid,
                FileName = "$MFTMirr",
                SecurityDescriptor = Constants.ServiceFileCount + 1,
                DataAttributes = new Data(GetSequenceOfDataBlocks(GetDataClustersCount(Constants.ServiceFileCount * Constants.MftRecFixSize),
                    Constants.VolumeSize / Constants.BytesPerCluster / 2))
            };
            MftRecord volume = new MftRecord
            {
                Sign = MftRecord.Signature.InUse, // Признак - запись используется.
                Attributes = 0b11, // Системный файл, только для чтения.
                Extension = "",
                Size = 11,
                CreationDate = nowDateTime,
                ModificationDate = nowDateTime,
                UserId = administrator.Uid,
                FileName = "$Volume",
                SecurityDescriptor = Constants.ServiceFileCount + 2,
                DataAttributes = new Data()
            };
            MftRecord rootDirectory = new MftRecord
            {
                Sign = MftRecord.Signature.InUse, // Признак - запись используется.
                Attributes = 0b1000, // Директория.
                Extension = "",
                Size = 0,
                CreationDate = nowDateTime,
                ModificationDate = nowDateTime,
                UserId = administrator.Uid,
                FileName = "$.",
                SecurityDescriptor = Constants.ServiceFileCount + 3,
                DataAttributes = new Data()
            };
            MftRecord bitmap = new MftRecord
            {
                Sign = MftRecord.Signature.InUse, // Признак - запись используется.
                Attributes = 0b11, // Системный файл, только для чтения.
                Extension = "",
                Size = Constants.BitmapSize,
                CreationDate = nowDateTime,
                ModificationDate = nowDateTime,
                UserId = administrator.Uid,
                FileName = "$Bitmap",
                SecurityDescriptor = Constants.ServiceFileCount + 4,
                DataAttributes = new Data(GetSequenceOfDataBlocks(GetDataRecordCount(Constants.BitmapSize),
                    Constants.ServiceFileCount + 6))
            };
            MftRecord usersRecord = new MftRecord
            {
                Sign = MftRecord.Signature.InUse, // Признак - запись используется.
                Attributes = 0b11, // Системный файл, только для чтения.
                Extension = "",
                Size = 193, // Размер одной пользовательской записи, представляющей информацию об администраторе.
                CreationDate = nowDateTime,
                ModificationDate = nowDateTime,
                UserId = administrator.Uid,
                FileName = "$Users",
                SecurityDescriptor = Constants.ServiceFileCount + 5,
                DataAttributes = new Data()
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
            WriteUsersDataToFile(bw, administrator); // Записываем данные (список пользователей) в запись $Users.

            #endregion

            #region Записываем в файл MFT списки управления доступом для всех служебных файлов.

            WriteAccessControlListToFile(bw, accessControlList, mft.SecurityDescriptor);
            WriteAccessControlListToFile(bw, accessControlList, mftMirr.SecurityDescriptor);
            WriteAccessControlListToFile(bw, accessControlList, volume.SecurityDescriptor);
            WriteAccessControlListToFile(bw, accessControlList, rootDirectory.SecurityDescriptor);
            WriteAccessControlListToFile(bw, accessControlList, bitmap.SecurityDescriptor);
            WriteAccessControlListToFile(bw, accessControlList, usersRecord.SecurityDescriptor);

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
                bw.BaseStream.Seek(recordNumber * Constants.MftRecFixSize, SeekOrigin.Begin);
            else bw.BaseStream.Seek(Constants.VolumeSize / 2 + recordNumber * Constants.MftRecFixSize, SeekOrigin.Begin);

            bw.Write((byte)record.Sign);
            bw.Write(record.Attributes);
            bw.Write(record.Extension.GetFormatBytes(5));
            bw.Write(BitConverter.GetBytes(record.Size));
            bw.Write(record.CreationDate.DateTimeBytes);
            bw.Write(record.ModificationDate.DateTimeBytes);
            bw.Write(record.UserId);
            bw.Write(record.FileName.GetFormatBytes(25));
            bw.Write(BitConverter.GetBytes(record.SecurityDescriptor));

            bw.Write(record.DataAttributes.Header);
            for (int i = 0; i < record.DataAttributes.Blocks?.Length; i++)
                bw.Write(BitConverter.GetBytes(record.DataAttributes.Blocks[i]));
        }

        static void WriteVolumeDataToFile(BinaryWriter bw)
        {
            bw.BaseStream.Seek(Constants.VolumeRecNumber * Constants.MftRecFixSize + Constants.MftHeaderLength + 1,
                SeekOrigin.Begin);
            bw.Write("H".GetFormatBytes(Constants.VolumeNameSize));
            bw.Write("VMFS v1.0".GetFormatBytes(Constants.FsVersionSize));
            bw.Write("0".GetFormatBytes(Constants.StateSize));
        }

        static void WriteUsersDataToFile(BinaryWriter bw, User user)
        {
            bw.BaseStream.Seek(Constants.UserListRecNumber * Constants.MftRecFixSize + Constants.MftHeaderLength + 1,
                SeekOrigin.Begin);

            bw.Write(user.Login.GetFormatBytes(20));
            bw.Write(user.Name.GetFormatBytes(30));
            bw.Write(user.Uid);
            bw.Write(Encoding.GetEncoding(1251).GetBytes(user.Salt));
            bw.Write(Encoding.GetEncoding(1251).GetBytes(user.Password));
            bw.Write(user.HomeDirectory.GetFormatBytes(30));
        }

        static void WriteBitmapDataToFile(BinaryWriter bw, MftRecord bitmap)
        {
            byte service = 0b10; // Служебный кластер.
            byte free = 0b0; // Свободный кластер.
            
            int mftClusterСount = Constants.MftAreaSize / Constants.BytesPerCluster;
            int clusterCount = Constants.VolumeSize / Constants.BytesPerCluster;
            int currentCluster = 1;

            foreach (var recordNumber in bitmap.DataAttributes.Blocks)
            {
                bw.BaseStream.Seek(recordNumber * Constants.MftRecFixSize, SeekOrigin.Begin);
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
                    bw.Write(clustersInfo.GetClusterInfoByte());
                }
            }
        }

        static void WriteAccessControlListToFile(BinaryWriter bw, AccessControlList accessControlList, int recordNumber)
        {
            bw.BaseStream.Seek(recordNumber * Constants.MftRecFixSize, SeekOrigin.Begin);
            bw.Write((byte)MftRecord.Signature.IsAccessControlList);
            bw.Write(accessControlList.ToBytes());
        }
        
        static int GetDataRecordCount(int dataSize)
        {
            return (int)Math.Ceiling((double)dataSize / Constants.MftRecFixSize +
                                     (double)dataSize / Constants.MftRecFixSize / Constants.MftRecFixSize);
        }

        static int GetDataClustersCount(int dataSize)
        {
            return (int)Math.Ceiling((double)dataSize / Constants.BytesPerCluster +
                                     (double)dataSize / Constants.BytesPerCluster / Constants.BytesPerCluster);
        }

        static int[] GetSequenceOfDataBlocks(int number, int firstBlock)
        {
            // Определяем количество Mft-записей (учитывая заголовок Mft-записи),
            // в которых будут храниться данные записи с заданным номером.
            int[] dataBlocks = new int[number];
            for (int i = 0; i < dataBlocks.Length; i++)
                dataBlocks[i] = firstBlock + i;
            return dataBlocks;
        }

        #endregion

        public static bool HasFreeMemory()
        {
            using (BinaryReader br = new BinaryReader(File.Open(Constants.SystemFile, FileMode.Open)))
            {
                // Смещаемся на поле данных (на атрибут Data) записи $Bitmap.
                br.BaseStream.Seek(Constants.BitmapRecNumber * Constants.MftRecFixSize + Constants.MftHeaderLength + 1,
                    SeekOrigin.Begin);

                if (br.ReadByte() != 1) return false;

                // Список номеров записей MFT, в которых содержатся данные битовой карты.
                List<int> recordNumbers = new List<int>();
                int recNumber;
                while ((recNumber = br.ReadInt32()) != 0)
                    recordNumbers.Add(recNumber);

                Data bitmapData = new Data(recordNumbers.ToArray());
                int byteNumber = 0; // Количество прочитанных байт битовой карты.

                foreach (var recordNumber in bitmapData.Blocks)
                {
                    br.BaseStream.Seek(recordNumber * Constants.MftRecFixSize, SeekOrigin.Begin);
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
