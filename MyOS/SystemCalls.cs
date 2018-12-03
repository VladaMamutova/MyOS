using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Navigation;

namespace MyOS
{
    static class SystemCalls
    {
        #region Метод форматирования и поддерживающие его методы.
        
        public static void Formatting()
        {
            using (var fileStream = new FileStream(SystemConstants.SystemFile, FileMode.Create, FileAccess.Write, FileShare.None))
                fileStream.SetLength(SystemData.VolumeSize); // Устанавливаем размер файла в 400 Мб.

            BinaryWriter bw = new BinaryWriter(File.Open(SystemConstants.SystemFile, FileMode.Open));
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
            // 4 файл - $\ (корневой каталог).
            // 5 файл - $Bitmap (битовая карта).
            // 6 файл - $Users (список пользователей системы).

            MftRecord mft = new MftRecord
            {
                Sign = MftRecord.Signature.InUse, // Признак - запись используется.
                Attributes = 0b11, // Системный файл, только для чтения.
                Extension = "",
                Size = 38 * SystemConstants.MftRecordSize, // Размер MFT. При форматировании по умолчанию создаётся 38 записей MFT по 1024 Б.
                CreationDate = nowDateTime,
                ModificationDate = nowDateTime,
                UserId = administrator.Uid,
                FileName = "$MFT",
                SecurityDescriptor = SystemConstants.ServiceFileCount,
                Data = new List<int>(GetSequenceOfDataBlocks(38, SystemConstants.MftRecNumber))
            };
            MftRecord mftMirr = new MftRecord
            {
                Sign = MftRecord.Signature.InUse, // Признак - запись используется.
                Attributes = 0b11, // Системный файл, только для чтения.
                Extension = "",
                Size = SystemConstants.ServiceFileCount * SystemConstants.MftRecordSize, // Всего 6 служебных записей MFT * 1024 = 6144 байт.
                CreationDate = nowDateTime,
                ModificationDate = nowDateTime,
                UserId = administrator.Uid,
                FileName = "$MFTMirr",
                SecurityDescriptor = SystemConstants.ServiceFileCount + 1,
                Data = new List<int>(GetSequenceOfDataBlocks(GetDataBlockCount(SystemConstants.ServiceFileCount * SystemConstants.MftRecordSize, SystemData.BytesPerCluster),
                    SystemData.VolumeSize / SystemData.BytesPerCluster / 2))
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
                SecurityDescriptor = SystemConstants.ServiceFileCount + 2,
                Data = new List<int>()
            };
            MftRecord rootDirectory = new MftRecord
            {
                Sign = MftRecord.Signature.InUse, // Признак - запись используется.
                Attributes = 0b1010, // Директория.
                Extension = "",
                Size = 0,
                CreationDate = nowDateTime,
                ModificationDate = nowDateTime,
                UserId = administrator.Uid,
                FileName = "$/",
                SecurityDescriptor = SystemConstants.ServiceFileCount + 3,
                Data = new List<int>()
            };
            MftRecord bitmap = new MftRecord
            {
                Sign = MftRecord.Signature.InUse, // Признак - запись используется.
                Attributes = 0b11, // Системный файл, только для чтения.
                Extension = "",
                Size = SystemData.BitmapSize,
                CreationDate = nowDateTime,
                ModificationDate = nowDateTime,
                UserId = administrator.Uid,
                FileName = "$Bitmap",
                SecurityDescriptor = SystemConstants.ServiceFileCount + 4,
                Data = new List<int>(GetSequenceOfDataBlocks(GetDataBlockCount(SystemData.BitmapSize, SystemConstants.MftRecordSize - 1),
                    SystemConstants.ServiceFileCount + 6))
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
                SecurityDescriptor = SystemConstants.ServiceFileCount + 5,
                Data = new List<int>()
            };


            #region Записываем служебные записи в главную файловую таблицу MFT.

            WriteMftRecordToFile(bw, mft, SystemConstants.MftRecNumber);
            WriteMftRecordToFile(bw, mftMirr, SystemConstants.MftMirrRecNumber);
            WriteMftRecordToFile(bw, volume, SystemConstants.VolumeRecNumber);
            WriteVolumeDataToFile(bw); // Записываем данные в запись $Volume.
            WriteMftRecordToFile(bw, rootDirectory, SystemConstants.RootDirectoryRecNumber);
            WriteMftRecordToFile(bw, bitmap, SystemConstants.BitmapRecNumber);
            WriteBitmapDataToFile(bw, bitmap); // Записываем данные битовой карты $Bitmap.
            WriteMftRecordToFile(bw, usersRecord, SystemConstants.UserListRecNumber);
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

            WriteMftRecordToFile(bw, mft, SystemConstants.MftRecNumber, true);
            WriteMftRecordToFile(bw, mftMirr, SystemConstants.MftMirrRecNumber, true);
            WriteMftRecordToFile(bw, volume, SystemConstants.VolumeRecNumber, true);
            WriteMftRecordToFile(bw, rootDirectory, SystemConstants.RootDirectoryRecNumber, true);
            WriteMftRecordToFile(bw, bitmap, SystemConstants.BitmapRecNumber, true);
            WriteMftRecordToFile(bw, usersRecord, SystemConstants.UserListRecNumber, true);

            #endregion

            #endregion

            bw.Close();

            
            MessageBox.Show("Диск отформатирован!");
        }

        public static void WriteMftRecordToFile(BinaryWriter bw, MftRecord record, int recordNumber, bool mftMirrow = false)
        {
            if (!mftMirrow) // Если данная запись является зеркальным отображением слежебной записи mft,
                // дополнительно смещаемся на середину диска для записи копии.
                bw.BaseStream.Seek(recordNumber * SystemConstants.MftRecordSize, SeekOrigin.Begin);
            else bw.BaseStream.Seek(SystemData.VolumeSize / 2 + recordNumber * SystemConstants.MftRecordSize, SeekOrigin.Begin);

            bw.Write((byte)record.Sign);
            bw.Write(record.Attributes);
            bw.Write(record.Extension.GetFormatBytes(5));
            bw.Write(BitConverter.GetBytes(record.Size));
            bw.Write(record.CreationDate.DateTimeBytes);
            bw.Write(record.ModificationDate.DateTimeBytes);
            bw.Write(record.UserId);
            bw.Write(record.FileName.GetFormatBytes(25));
            bw.Write(BitConverter.GetBytes(record.SecurityDescriptor));

            bw.Write((byte) (record.Data.Count == 0 ? 0 : 1));
            foreach (var block in record.Data)
                bw.Write(BitConverter.GetBytes(block));
        }

        static void WriteVolumeDataToFile(BinaryWriter bw)
        {
            bw.BaseStream.Seek(SystemConstants.VolumeRecNumber * SystemConstants.MftRecordSize + SystemConstants.MftHeaderLength + 1,
                SeekOrigin.Begin);
            bw.Write("H".GetFormatBytes(SystemData.VolumeNameSize));
            bw.Write("VMFS v1.0".GetFormatBytes(SystemData.FsVersionSize));
            bw.Write("0".GetFormatBytes(SystemData.StateSize));
        }

        static void WriteUsersDataToFile(BinaryWriter bw, User user)
        {
            bw.BaseStream.Seek(SystemConstants.UserListRecNumber * SystemConstants.MftRecordSize + SystemConstants.MftHeaderLength + 1,
                SeekOrigin.Begin);

            bw.Write(user.Login.GetFormatBytes(20));
            bw.Write(user.Name.GetFormatBytes(30));
            bw.Write(user.Uid);
            bw.Write(Encoding.UTF8.GetBytes(user.Salt));
            bw.Write(Encoding.UTF8.GetBytes(user.Password));
            bw.Write(user.HomeDirectory.GetFormatBytes(30));
        }

        static void WriteBitmapDataToFile(BinaryWriter bw, MftRecord bitmap)
        {
            byte service = 0b10; // Служебный кластер.
            byte free = 0b0; // Свободный кластер.
            
            int mftClusterСount = SystemData.MftAreaSize / SystemData.BytesPerCluster;
            int clusterCount = SystemData.VolumeSize / SystemData.BytesPerCluster;
            int currentCluster = 1;

            foreach (var recordNumber in bitmap.Data)
            {
                bw.BaseStream.Seek(recordNumber * SystemConstants.MftRecordSize, SeekOrigin.Begin);
                bw.Write((byte)MftRecord.Signature.IsData); // Записываем признак записи MFT.
                for (int i = 0; i < SystemConstants.MftRecordSize - 1; i++) // -1, т.к. 1 байт уже выделен под признак записи MFT).
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
            bw.BaseStream.Seek(recordNumber * SystemConstants.MftRecordSize, SeekOrigin.Begin);
            bw.Write((byte)MftRecord.Signature.IsAccessControlList);
            bw.Write(accessControlList.ToBytes());
        }
        
        public static int GetDataBlockCount(int dataSize, int blockSize)
        {
            if (dataSize <= SystemConstants.MftRecordSize - SystemConstants.MftHeaderLength - 1)
                return 0;
            return (int)Math.Ceiling((double)dataSize / blockSize);
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
            using (BinaryReader br = new BinaryReader(File.Open(SystemConstants.SystemFile, FileMode.Open)))
            {
                // Смещаемся на поле данных (на атрибут Data) записи $Bitmap.
                br.BaseStream.Seek(SystemConstants.BitmapRecNumber * SystemConstants.MftRecordSize, SeekOrigin.Begin);

                MftRecord bitmap = ReadMftRecord(br);

               int byteNumber = 0; // Количество прочитанных байт битовой карты.

                foreach (var recordNumber in bitmap.Data)
                {
                    br.BaseStream.Seek(recordNumber * SystemConstants.MftRecordSize, SeekOrigin.Begin);
                    if (br.ReadByte() != (byte) MftRecord.Signature.IsData) return false;

                    for (int i = 0; i < SystemConstants.MftRecordSize - 1; i++) // -1 байт для заголовка Data
                    {
                        if (byteNumber >= SystemData.BitmapSize) return false;

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

        public static List<FileRecord> GetFileList(Path path)
        {
            List<FileRecord> files = new List<FileRecord>();
            int directoryRecordNumber = GetDirectoryMftRecordNumber(path);
            if(directoryRecordNumber == -1) return new List<FileRecord>();
            using (BinaryReader br = new BinaryReader(File.Open(SystemConstants.SystemFile, FileMode.Open)))
            {
                br.BaseStream.Seek(directoryRecordNumber * SystemConstants.MftRecordSize, SeekOrigin.Begin);
                MftRecord destinationDirectory = ReadMftRecord(br);
                int fileCount = destinationDirectory.Size / SystemConstants.RootRecordLength;
                if (fileCount == 0) return new List<FileRecord>();

                if (destinationDirectory.Data.Count == 0)
                    for (int i = 0; i < fileCount; i++)
                    {
                        RootRecord file = ReadRootRecord(br);
                        files.Add(new FileRecord{Attributes = file.Attributes, FileName = file.FileName, CreadtionDate = file.CreadtionDate, Size = file.Size});
                    }
                else
                {
                    int currentOffset = 0;
                    int virtualOffset = 0;
                    int dataSizeInRecord = SystemConstants.MftRecordSize - 1;
                    int blockIndex = 0;
                    br.BaseStream.Seek(destinationDirectory.Data[blockIndex] * SystemConstants.MftRecordSize + 1, SeekOrigin.Begin);
                    while (currentOffset < destinationDirectory.Size)
                    {
                        if (virtualOffset + SystemConstants.RootRecordLength > dataSizeInRecord)
                        {
                            byte[] recordHead = br.ReadBytes(Math.Abs(virtualOffset - dataSizeInRecord));
                            br.BaseStream.Seek(destinationDirectory.Data[++blockIndex] * SystemConstants.MftRecordSize, SeekOrigin.Begin);
                            if (br.ReadByte() == (byte) MftRecord.Signature.IsData)
                            {
                                byte[] recordTail = br.ReadBytes(SystemConstants.RootRecordLength - recordHead.Length);
                                List<byte> recordBytes = new List<byte>(recordHead);
                                recordBytes.AddRange(recordTail);
                                RootRecord file = new RootRecord(recordBytes.ToArray());
                                files.Add(new FileRecord
                                {
                                    Attributes = file.Attributes,
                                    FileName = file.FileName,
                                    CreadtionDate = file.CreadtionDate,
                                    Size = file.Size
                                });
                                currentOffset += SystemConstants.RootRecordLength;
                                virtualOffset = 1 + recordTail.Length;
                            }
                            
                        }
                        else
                        {
                            RootRecord file = ReadRootRecord(br);
                            files.Add(new FileRecord
                            {
                                Attributes = file.Attributes,
                                FileName = file.FileName,
                                CreadtionDate = file.CreadtionDate,
                                Size = file.Size
                            });
                            currentOffset += SystemConstants.RootRecordLength;
                            virtualOffset += SystemConstants.RootRecordLength;
                        }
                    }
                }
            }

            return files; //string.Join(Environment.NewLine, files)
        }

        public static bool HasFileWithSuchName(string newFileName, string newFileExtension, int directoryRecordNumberInMft)
        {
            using (BinaryReader br = new BinaryReader(File.Open(SystemConstants.SystemFile, FileMode.Open)))
            {
                if (directoryRecordNumberInMft > 0)
                {
                    // Перемещаемся на запись каталога, в котором пользователь хочет создать файл.
                    br.BaseStream.Seek(directoryRecordNumberInMft * SystemConstants.MftRecordSize, SeekOrigin.Begin);
                    MftRecord destinationDirectory = ReadMftRecord(br);
                    int fileCount = destinationDirectory.Size / SystemConstants.RootRecordLength;
                    if (destinationDirectory.Data.Count == 0)
                    {
                        for (int j = 0; j < fileCount; j++)
                        {
                            RootRecord file = ReadRootRecord(br);
                            if (newFileName == file.FileName && newFileExtension == file.Extension)
                                return true;
                        }
                        return false;
                    }

                    int currentOffset = 0;
                    int virtualOffset = 0;
                    int dataSizeInRecord = SystemConstants.MftRecordSize - 1;
                    int blockIndex = 0;
                    br.BaseStream.Seek(destinationDirectory.Data[blockIndex] * SystemConstants.MftRecordSize,
                        SeekOrigin.Begin);
                    if (br.ReadByte() == (byte)MftRecord.Signature.IsData)
                    {
                        while (currentOffset < destinationDirectory.Size)
                        {
                            if (virtualOffset + SystemConstants.RootRecordLength > dataSizeInRecord)
                            {
                                byte[] recordHead = br.ReadBytes(Math.Abs(virtualOffset - dataSizeInRecord));
                                br.BaseStream.Seek(
                                    destinationDirectory.Data[++blockIndex] * SystemConstants.MftRecordSize,
                                    SeekOrigin.Begin);
                                if (br.ReadByte() == (byte)MftRecord.Signature.IsData)
                                {
                                    byte[] recordTail =
                                        br.ReadBytes(SystemConstants.RootRecordLength - recordHead.Length);
                                    List<byte> recordBytes = new List<byte>(recordHead);
                                    recordBytes.AddRange(recordTail);
                                    RootRecord file = new RootRecord(recordBytes.ToArray());
                                    if (newFileName == file.FileName && newFileExtension == file.Extension)
                                        return true;

                                    currentOffset += SystemConstants.RootRecordLength;
                                    virtualOffset = 1 + recordTail.Length;
                                }

                            }
                            else
                            {
                                RootRecord file = ReadRootRecord(br);
                                if (newFileName == file.FileName && newFileExtension == file.Extension)
                                    return true;

                                currentOffset += SystemConstants.RootRecordLength;
                                virtualOffset += SystemConstants.RootRecordLength;
                            }
                        }
                    }
                    return false;
                }
            }
            return false;

        }

        public static MftRecord ReadMftRecord(BinaryReader br)
        {
            byte[] recordHeader = br.ReadBytes(SystemConstants.MftHeaderLength);

            return new MftRecord
            {
                Sign = (MftRecord.Signature)recordHeader[0],
                Attributes = recordHeader[1],
                Extension = Encoding.UTF8.GetString(recordHeader.GetRange(2, 5)).Trim('\0'),
                Size = recordHeader.GetRange(7, 4).ToInt32(),
                CreationDate = new MyDateTime(recordHeader.GetRange(11, 5)),
                ModificationDate = new MyDateTime(recordHeader.GetRange(16, 5)),
                UserId = recordHeader[21],
                FileName = Encoding.UTF8.GetString(recordHeader.GetRange(22, 25)).Trim('\0'),
                SecurityDescriptor = recordHeader.GetRange(47, 4).ToInt32(),
                Data = 
                    new List<int>(ReadDataBlocks(br, GetDataBlockCount(recordHeader.GetRange(7, 4).ToInt32(), SystemConstants.MftRecordSize)))
            };

        }

        public static RootRecord ReadRootRecord(BinaryReader br)
        {
            byte[] rootRecord = br.ReadBytes(43);
            return new RootRecord
            {
                FileName = Encoding.UTF8.GetString(rootRecord.GetRange(0, 24)).Trim('\0'),
                Attributes = rootRecord[24],
                Extension = Encoding.UTF8.GetString(rootRecord.GetRange(25, 5)).Trim('\0'),
                Size = rootRecord.GetRange(30, 4).ToInt32(),
                CreadtionDate = new MyDateTime(rootRecord.GetRange(34, 5)),
                Number = rootRecord.GetRange(39, 4).ToInt32()
            };
        }

        public static List<int> ReadDataBlocks(BinaryReader br, int blockCount)
        {
            // Если заголовок Атрибута Data равен 0, то поле не содержит номеров блоков с данными.
            if (br.ReadByte() == 0 || blockCount == 0) return new List<int>();

            List<int> blocks = new List<int>(blockCount); // Список номеров блоков, в которых содержатся данные.
            for (int i = 0; i < blockCount; i++)
                blocks.Add(br.ReadInt32());
            return blocks;
        }

        static void WriteRootRecordToFile(BinaryWriter bw, List<int> dataRecords, RootRecord record, FileOffsetInDirectory fileOffset)
        {
            // смещаемся на необходимый блок данных - на необходимую запись mft
            if (dataRecords.Count == 0)
            {
                bw.BaseStream.Seek(fileOffset.DirectoryRecordNumberInMft * SystemConstants.MftRecordSize,
                    SeekOrigin.Begin);
                bw.BaseStream.Seek(SystemConstants.MftHeaderLength + 1 + fileOffset.FileRecordNumberInDirectory * SystemConstants.RootRecordLength, SeekOrigin.Current);
                bw.Write(record.GetBytes());
            }
            else
            {
                int directoryBlockNumber = fileOffset.FileRecordNumberInDirectory * SystemConstants.RootRecordLength /
                                           (SystemConstants.MftRecordSize - 1);
                // смещение относительно блока
                int fileRecordOffset = fileOffset.FileRecordNumberInDirectory * SystemConstants.RootRecordLength %
                                       (SystemConstants.MftRecordSize - 1);

                int availableBytes = SystemConstants.MftRecordSize - 1 - fileRecordOffset;

                bw.BaseStream.Seek(
                    dataRecords[directoryBlockNumber] * SystemConstants.MftRecordSize + 1 + fileRecordOffset,
                    SeekOrigin.Begin);
                if (availableBytes >= SystemConstants.RootRecordLength)
                    bw.Write(record.GetBytes());
                else
                {
                    bw.Write(record.GetBytes().GetRange(0, availableBytes));
                    bw.BaseStream.Seek(dataRecords[directoryBlockNumber + 1] * SystemConstants.MftRecordSize,
                        SeekOrigin.Begin);
                    bw.Write((byte)MftRecord.Signature.IsData);
                    bw.Write(record.GetBytes().GetRange(availableBytes, SystemConstants.RootRecordLength - availableBytes));
                }
            }
        }

        private static void ShiftDataToNewMftRecord(int lastMftRecordNumber, int dataSize, int newMftRecordNumber)
        {
            byte[] dataBytes;
            using (BinaryReader br = new BinaryReader(File.Open(SystemConstants.SystemFile, FileMode.Open)))
            {
                br.BaseStream.Seek(
                    lastMftRecordNumber * SystemConstants.MftRecordSize + SystemConstants.MftHeaderLength + 1,
                    SeekOrigin.Begin);
                dataBytes = br.ReadBytes(dataSize);
            }

            using (BinaryWriter bw = new BinaryWriter(File.Open(SystemConstants.SystemFile, FileMode.Open)))
            {
                bw.BaseStream.Seek(
                    lastMftRecordNumber * SystemConstants.MftRecordSize + SystemConstants.MftHeaderLength + 1,
                    SeekOrigin.Begin);
                for (int i = 0; i < dataSize; i++)
                    bw.Write(0);

                bw.BaseStream.Seek(newMftRecordNumber * SystemConstants.MftRecordSize, SeekOrigin.Begin);
                bw.Write((byte)MftRecord.Signature.IsData);
                bw.Write(dataBytes);
            }
        }

        static int GetNewMftRecordNumber(int haveAlreadyRecieved = 0)
        {
            using (BinaryReader br = new BinaryReader(File.Open(SystemConstants.SystemFile, FileMode.Open)))
            {
                //long mftSize = br.BaseStream.Length * 10 / 100;
                br.BaseStream.Seek(SystemConstants.MftRecNumber * SystemConstants.MftRecordSize, SeekOrigin.Begin);
                MftRecord mft = ReadMftRecord(br);
                return mft.Data.Count + haveAlreadyRecieved;
            }
        }

        static int GetNewFileRecordNumberInDirectory(int directoryRecordNumberInMft)
        {
            using (BinaryReader br = new BinaryReader(File.Open(SystemConstants.SystemFile, FileMode.Open)))
            {
                br.BaseStream.Seek(directoryRecordNumberInMft * SystemConstants.MftRecordSize, SeekOrigin.Begin);
                MftRecord directory = ReadMftRecord(br);
                return directory.Size / SystemConstants.RootRecordLength;
            }
        }
    
        static int GetDirectoryMftRecordNumber(Path path)
        {
            string[] dirictoryList = path.DirectoriesList.ToArray();//Split(new[] { '\\' }, StringSplitOptions.RemoveEmptyEntries);

            using (BinaryReader br = new BinaryReader(File.Open(SystemConstants.SystemFile, FileMode.Open)))
            {
                int recordNumberInMft = SystemConstants.RootDirectoryRecNumber;
                bool directoryFound = dirictoryList.Length == 0;
                foreach (var directory in dirictoryList)
                {
                    br.BaseStream.Seek(recordNumberInMft * SystemConstants.MftRecordSize, SeekOrigin.Begin);
                    MftRecord currentDirectory = ReadMftRecord(br);

                    if (currentDirectory.Data.Count == 0)
                    { // Записи корневого каталога помещаются в поле данных Mft-записи.
                        // в поле данных максимум может быть только 22 записи
                        int fileCount = currentDirectory.Size / SystemConstants.RootRecordLength;
                        directoryFound = false;
                        for (int j = 0; j < fileCount && !directoryFound; j++)
                        {
                            RootRecord file = ReadRootRecord(br);
                            // если рассматриваемая запись в каталоге - это 
                            // папка c именем, которое есть в пути к файлу, - 
                            //перемещаемся на запись Mft для чтения её данных
                            if (directory == file.FileName && file.Attributes ==
                                (file.Attributes | (byte)MftRecord.Attribute.Directory))
                            {
                                recordNumberInMft = file.Number;
                                directoryFound = true;
                            }
                        }
                    }
                    else
                    {
                        int currentOffset = 0;
                        int virtualOffset = 0;
                        int dataSizeInRecord = SystemConstants.MftRecordSize - 1;
                        int blockIndex = 0;
                        br.BaseStream.Seek(currentDirectory.Data[blockIndex] * SystemConstants.MftRecordSize,
                            SeekOrigin.Begin);
                        if (br.ReadByte() == (byte) MftRecord.Signature.IsData)
                        {
                            while (currentOffset < currentDirectory.Size)
                            {
                                if (virtualOffset + SystemConstants.RootRecordLength > dataSizeInRecord)
                                {
                                    byte[] recordHead = br.ReadBytes(Math.Abs(virtualOffset - dataSizeInRecord));
                                    br.BaseStream.Seek(
                                        currentDirectory.Data[++blockIndex] * SystemConstants.MftRecordSize,
                                        SeekOrigin.Begin);
                                    if (br.ReadByte() == (byte) MftRecord.Signature.IsData)
                                    {
                                        byte[] recordTail =
                                            br.ReadBytes(SystemConstants.RootRecordLength - recordHead.Length);
                                        List<byte> recordBytes = new List<byte>(recordHead);
                                        recordBytes.AddRange(recordTail);
                                        RootRecord file = new RootRecord(recordBytes.ToArray());
                                        // если рассматриваемая запись в каталоге - это 
                                        // папка c именем, которое есть в пути к файлу, - 
                                        //перемещаемся на запись Mft для чтения её данных
                                        if (directory == file.FileName && file.Attributes ==
                                            (file.Attributes | (byte) MftRecord.Attribute.Directory))
                                        {
                                            recordNumberInMft = file.Number;
                                            directoryFound = true;
                                        }

                                        currentOffset += SystemConstants.RootRecordLength;
                                        virtualOffset = 1 + recordTail.Length;
                                    }

                                }
                                else
                                {
                                    RootRecord file = ReadRootRecord(br);
                                    // если рассматриваемая запись в каталоге - это 
                                    // папка c именем, которое есть в пути к файлу, - 
                                    //перемещаемся на запись Mft для чтения её данных
                                    if (directory == file.FileName && file.Attributes ==
                                        (file.Attributes | (byte) MftRecord.Attribute.Directory))
                                    {
                                        recordNumberInMft = file.Number;
                                        directoryFound = true;
                                    }

                                    currentOffset += SystemConstants.RootRecordLength;
                                    virtualOffset += SystemConstants.RootRecordLength;
                                }
                            }
                        }
                    }

                    if (!directoryFound) return -1; // неверно указан путь
                }

                //if (directoryFound)
                    return recordNumberInMft;
                //return recordNumberInMft;
            }
        }

        public static bool HasMemoryToAddRecord(int recordNumber, int recordSize, int fileSize)
        {
            return (recordNumber + 1) * recordSize <= fileSize;
        }

        public static void CreateFile(string name, string extension, Path path, MftRecord.Attribute attributes = 0)
        {
            //mft.Size += SystemConstants.MftRecordSize; // проверка попадания в mft-пространство уже есть, передаётся recordNumber, попадающий в mft
            //mft.Data.Add(recordNumber);

            // проверка на права
            int mftRecordNumber = GetNewMftRecordNumber();
            if(!HasMemoryToAddRecord(mftRecordNumber, SystemConstants.MftRecordSize, SystemData.MftAreaSize))
            // Проверяем, есть ли место на диске для создания нового файла.
            //if (!HasFreeMemory())
                MessageBox.Show("Недостаточно места на диске!", "Ошибка создания файла", MessageBoxButton.OK,
                    MessageBoxImage.Error);
            else
            {
                int directoryRecordNumberInMft = GetDirectoryMftRecordNumber(path);
                MftRecord root;
                using (BinaryReader br = new BinaryReader(File.Open(SystemConstants.SystemFile, FileMode.Open)))
                {
                    br.BaseStream.Seek(directoryRecordNumberInMft * SystemConstants.MftRecordSize, SeekOrigin.Begin);
                    root = ReadMftRecord(br);
                }

                int rootFileSize;
                if (root.Data.Count == 0) rootFileSize = SystemConstants.MftRecordSize - SystemConstants.MftHeaderLength - 1;
                else rootFileSize = (SystemConstants.MftRecordSize - 1) * root.Data.Count;
                int fileRecordNumberInDirectory = GetNewFileRecordNumberInDirectory(directoryRecordNumberInMft);
                int directoryBlockRecordNumberInMftForNewFile = directoryRecordNumberInMft;
                if (!HasMemoryToAddRecord(fileRecordNumberInDirectory, SystemConstants.RootRecordLength, rootFileSize))
                {
                    directoryBlockRecordNumberInMftForNewFile = GetNewMftRecordNumber(1);
                    if (!HasMemoryToAddRecord(directoryBlockRecordNumberInMftForNewFile, SystemConstants.MftRecordSize, SystemData.MftAreaSize))
                        MessageBox.Show("Недостаточно места на диске!", "Ошибка создания файла", MessageBoxButton.OK,
                            MessageBoxImage.Error);
                }

                // Проверяем, не существует ли уже файл с таким именем.
                if(HasFileWithSuchName(name, extension, directoryRecordNumberInMft))
                    MessageBox.Show("Файл с таким именем уже существует!", "Ошибка создания файла", MessageBoxButton.OK,
                        MessageBoxImage.Error);
                else
                {
                    FileOffsetInDirectory fileOffsetInDirectory = new FileOffsetInDirectory(directoryRecordNumberInMft, fileRecordNumberInDirectory);

                    MftRecord mft;

                    using (BinaryReader br = new BinaryReader(File.Open(SystemConstants.SystemFile, FileMode.Open)))
                    {
                        br.BaseStream.Seek(SystemConstants.MftRecNumber * SystemConstants.MftRecordSize, SeekOrigin.Begin);
                        mft = ReadMftRecord(br);
                    }

                    mft.Size += SystemConstants.MftRecordSize; // проверка попадания в mft-пространство уже есть, передаётся recordNumber, попадающий в mft
                    mft.Data.Add(mftRecordNumber);


                    if (root.Data.Count == 0)
                    {
                        if (directoryRecordNumberInMft != directoryBlockRecordNumberInMftForNewFile)
                        {
                            ShiftDataToNewMftRecord(directoryRecordNumberInMft, root.Size,
                                directoryBlockRecordNumberInMftForNewFile);
                            root.Data.Add(directoryBlockRecordNumberInMftForNewFile);
                            mft.Size += SystemConstants.MftRecordSize;
                            mft.Data.Add(directoryBlockRecordNumberInMftForNewFile);
                        }
                    }
                    else if (directoryRecordNumberInMft != directoryBlockRecordNumberInMftForNewFile &&
                             !root.Data.Contains(directoryBlockRecordNumberInMftForNewFile))
                    {
                        root.Data.Add(directoryBlockRecordNumberInMftForNewFile);
                        mft.Size += SystemConstants.MftRecordSize;
                        mft.Data.Add(directoryBlockRecordNumberInMftForNewFile);
                    }

                    root.Size += SystemConstants.RootRecordLength; // проверка попадания в mft уже есть
                    // Если данные помещались в одну запись MFT, сдвигаем данные на новую запись Mft.
                    // Добавляем номер записи MFT в атрибут Data.

                    MyDateTime now = new MyDateTime(DateTime.Now);
                    MftRecord newRecord = new MftRecord
                    {
                        Sign = MftRecord.Signature.InUse,
                        Attributes = (byte)attributes,
                        Extension = extension,
                        Size = 0,
                        CreationDate = now,
                        ModificationDate = now,
                        UserId = 0, /////
                        FileName = name,
                        SecurityDescriptor = mftRecordNumber, /////////
                        Data = new List<int>()
                    };
                    RootRecord newFile = new RootRecord
                    {
                        Attributes = (byte)attributes,
                        Extension = extension,
                        Size = 0,
                        CreadtionDate = now,
                        FileName = name,
                        Number = mftRecordNumber
                    };

                    using (BinaryWriter bw = new BinaryWriter(File.Open(SystemConstants.SystemFile, FileMode.Open)))
                    {
                        WriteMftRecordToFile(bw, mft, SystemConstants.MftRecNumber);
                        WriteMftRecordToFile(bw, root, directoryRecordNumberInMft);

                        WriteMftRecordToFile(bw, newRecord, mftRecordNumber);
                        WriteRootRecordToFile(bw, root.Data, newFile, fileOffsetInDirectory);
                    }
                }
            }
        }

    }
}
