using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;

namespace MyOS
{
    static class SystemCalls
    {
        #region Метод форматирования и поддерживающие его методы.
        
        public static void Formatting()
        {
            using (var fileStream = new FileStream(SystemConstants.SystemFile, FileMode.Create, FileAccess.Write, FileShare.None))
                fileStream.SetLength(SystemData.VolumeSize); // Устанавливаем размер файла в 400 Мб.

            //MyDateTime nowDateTime = new MyDateTime(DateTime.Now);
            Account.User = new User("Administrator", "admin", true); // По умолчанию в системе создаётся администратор.
            //Permission permission = new Permission(); // По умолчанию для администратора и пользователя предоставляется полный доступ к файлу.
            MftHeader.Attribute systemReadOnlyHidden =
                MftHeader.Attribute.System | MftHeader.Attribute.ReadOnly | MftHeader.Attribute.Hidden;
            #region Форматирование MFT-пространства.

            // Создание файла $MFT, представляющего централизованный каталог
            // всех остальных файлов диска и себя самого.
            // Первые 6 записей - метафайлы (имеют фиксированное положение на диске).
            // 1 файл - запись о самом MFT.
            // 2 файл - запись о копии первых записей MFT.
            // 3 файл - $Volume.
            // 4 файл - / (корневой каталог).
            // 5 файл - $Bitmap (битовая карта).
            // 6 файл - $Users (список пользователей системы).

            MftHeader mft = new MftHeader("$MFT",
                size: (SystemConstants.ServiceFileCount + 26) * SystemConstants.MftRecordSize,
                attributes: systemReadOnlyHidden);

            MftHeader mftMirr = new MftHeader("$MFTMirr",
                size: SystemConstants.ServiceFileCount * SystemConstants.MftRecordSize,
                attributes: systemReadOnlyHidden);

            int[] mftMirrDataBlocks = new List<int>(GetSequenceOfDataBlocks(
                GetDataBlockCount(SystemConstants.ServiceFileCount * SystemConstants.MftRecordSize,
                    SystemData.BytesPerCluster), SystemData.VolumeSize / SystemData.BytesPerCluster / 2)).ToArray();
            DataAttributes mftMirrDataAttributes =
                new DataAttributes(true, SystemConstants.MftMirrRecNumber, mftMirr.Size, mftMirrDataBlocks);

            MftHeader volume = new MftHeader("$Volume", size: 11, attributes: systemReadOnlyHidden);

            MftHeader root = new MftHeader("/", attributes: MftHeader.Attribute.Directory);

            // Размер битовой карты = (102 400 кластеров * 2 бита / 8 битов в байте = 25600 байтов).
            int BitmapSize = SystemData.VolumeSize / SystemData.BytesPerCluster * 2 / 8;
            MftHeader bitmap = new MftHeader("$Bitmap", size: BitmapSize, attributes: systemReadOnlyHidden);

            int[] bitmapDataBlocks =
                new List<int>(GetSequenceOfDataBlocks(GetDataBlockCount(BitmapSize, SystemConstants.MftRecordSize - 1),
                    SystemConstants.ServiceFileCount)).ToArray();
            DataAttributes bitmapDataAttribytes = new DataAttributes(true, SystemConstants.BitmapRecNumber, bitmap.Size, bitmapDataBlocks);

            MftHeader users = new MftHeader("$Users", size: User.AccountLength, attributes: systemReadOnlyHidden);

            #region Записываем служебные записи в главную файловую таблицу MFT.

            UpdateMftEntry(mft, SystemConstants.MftRecNumber);
            UpdateMftEntry(mftMirr, SystemConstants.MftMirrRecNumber);
            WriteData(mftMirrDataAttributes, null);
            UpdateMftEntry(volume, SystemConstants.VolumeRecNumber);
            WriteVolumeDataToFile(); // Записываем данные в запись $Volume.
            UpdateMftEntry(root, SystemConstants.RootDirectoryRecNumber);
            UpdateMftEntry(bitmap, SystemConstants.BitmapRecNumber);
            WriteData(bitmapDataAttribytes, null);
            WriteBitmapDataToFile(); // Записываем данные битовой карты $Bitmap.
            UpdateMftEntry(users, SystemConstants.UserListRecNumber);
            RegisterUser(Account.User); // Записываем данные (список пользователей) в запись $Users.

            #endregion

            #region Записываем копии служебных записей MFT посередине диска.

            UpdateMftEntry(mft, SystemConstants.MftRecNumber, true);
            UpdateMftEntry(mftMirr, SystemConstants.MftMirrRecNumber, true);
            UpdateMftEntry(volume, SystemConstants.VolumeRecNumber, true);
            UpdateMftEntry(root, SystemConstants.RootDirectoryRecNumber, true);
            UpdateMftEntry(bitmap, SystemConstants.BitmapRecNumber, true);
            UpdateMftEntry(users, SystemConstants.UserListRecNumber, true);

            #endregion

            #endregion
            
            MessageBox.Show("Диск отформатирован!");
        }

        static void UpdateMftEntry(MftHeader header, int entryNumber, bool mftMirrow = false)
        {
            using (BinaryWriter bw = new BinaryWriter(File.Open(SystemConstants.SystemFile, FileMode.Open)))
            {
                if (!mftMirrow) // Если данная запись является зеркальным отображением слежебной записи mft,
                    // дополнительно смещаемся на середину диска для записи копии.
                    bw.BaseStream.Seek(entryNumber * SystemConstants.MftRecordSize, SeekOrigin.Begin);
                else bw.BaseStream.Seek(SystemData.VolumeSize / 2 + entryNumber * SystemConstants.MftRecordSize, SeekOrigin.Begin);

                if (header == null)
                    bw.Write((byte)MftHeader.Signature.NotUsed);
                else bw.Write(header.GetBytes());
            }
        }

        static void WriteVolumeDataToFile()
        {
            using (BinaryWriter bw = new BinaryWriter(File.Open(SystemConstants.SystemFile, FileMode.Open)))
            {
                bw.BaseStream.Seek(SystemConstants.VolumeRecNumber * SystemConstants.MftRecordSize + MftHeader.Length,
                    SeekOrigin.Begin);
                bw.Write("H".GetFormatBytes(1));
                bw.Write("VMFS v1.0".GetFormatBytes(9));
                bw.Write((byte) 0);
            }
        }

        public static void RegisterUser(User user)
        {
            DataAttributes usersDataAttributes = ReadDataAttributes(SystemConstants.UserListRecNumber);
            
            using (BinaryReader br = new BinaryReader(File.Open(SystemConstants.SystemFile, FileMode.Open)))
            {
                br.BaseStream.Seek(SystemConstants.UserListRecNumber * SystemConstants.MftRecordSize + MftHeader.Length,
                    SeekOrigin.Begin);
                    int offset = 0;

                    if (usersDataAttributes.Blocks.Count == 0)
                    {
                        br.BaseStream.Seek(SystemConstants.UserListRecNumber * SystemConstants.MftRecordSize + MftHeader.Length,
                            SeekOrigin.Begin);

                        while (offset < usersDataAttributes.Size)
                        {
                            byte[] userBytes = br.ReadBytes(User.AccountLength);
                            // Если нашли пустое место в файле $Users (со списком пользователей),
                            // записываем на начало заявки данные нового пользователя.
                            if (userBytes.SequenceEqual(new byte[User.AccountLength]))
                            {
                                using (BinaryWriter bw = new BinaryWriter(br.BaseStream))
                                {
                                    bw.BaseStream.Position -= User.AccountLength;
                                    bw.Write(user.Name.GetFormatBytes(26));
                                    bw.Write(user.Id);
                                    bw.Write(user.PasswordHash);
                                }

                                return;
                            }

                            offset += DirectoryRecord.Length;

                        }
                    }
                else
                {
                    int blockIndex = 0;
                    while (offset < usersDataAttributes.Size)
                    {
                        List<byte> userBytes = new List<byte>();
                        if (SystemData.BytesPerCluster - offset % SystemData.BytesPerCluster >=
                            DirectoryRecord.Length)
                        {
                            br.BaseStream.Seek(
                                usersDataAttributes.Blocks[blockIndex] * SystemData.BytesPerCluster +
                                offset % SystemData.BytesPerCluster,
                                SeekOrigin.Begin);
                            userBytes.AddRange(br.ReadBytes(DirectoryRecord.Length));
                        }
                        else
                        {
                            userBytes.AddRange(
                                br.ReadBytes(SystemData.BytesPerCluster - offset % SystemData.BytesPerCluster));
                            br.BaseStream.Seek(usersDataAttributes.Blocks[++blockIndex] * SystemData.BytesPerCluster,
                                SeekOrigin.Begin);
                            userBytes.AddRange(br.ReadBytes(DirectoryRecord.Length - userBytes.Count));
                        }

                        if (userBytes.SequenceEqual(new byte[User.AccountLength]))
                        {
                            using (BinaryWriter bw = new BinaryWriter(br.BaseStream))
                            {
                                bw.BaseStream.Position -= User.AccountLength;
                                bw.Write(user.Name.GetFormatBytes(26));
                                bw.Write(user.Id);
                                bw.Write(user.PasswordHash);
                            }
                            return;
                        }

                        offset += DirectoryRecord.Length;
                    }
                }
            }
        }

        static void WriteBitmapDataToFile()
        {
            DataAttributes dataAttributes = ReadDataAttributes(SystemConstants.BitmapRecNumber);
            int mftClusterСount = SystemData.MftAreaSize / SystemData.BytesPerCluster;
            int clusterCount = SystemData.VolumeSize / SystemData.BytesPerCluster;
            int currentCluster = 1;
            Bitmap.FreeClusters = clusterCount;
            using (BinaryWriter bw = new BinaryWriter(File.Open(SystemConstants.SystemFile, FileMode.Open)))
            {
                foreach (var block in dataAttributes.Blocks)
                {
                    bw.BaseStream.Seek(block * SystemConstants.MftRecordSize, SeekOrigin.Begin);
                    bw.Write((byte) MftHeader.Signature.IsData); // Записываем признак записи MFT.
                    for (int i = 0; i < SystemConstants.MftRecordSize - 1; i++)
                        // -1, т.к. 1 байт уже выделен под признак записи MFT).
                    {
                        // Кластер представляется 2 битами, значит в байте - информация о 4 кластерах.
                        byte[] clustersInfo = new byte[4]; // Массив, представляющий информацию о 4 кластерах.
                        for (int j = 0; j < 4; j++)
                        {
                            // Если в битовую карту записана информация не о всех кластерах, продолжаем заполнять байты.
                            if (currentCluster > clusterCount) return;

                            clustersInfo[j] = currentCluster <= mftClusterСount ||
                                              currentCluster == clusterCount / 2 ||
                                              currentCluster == clusterCount / 2 + 1
                                ? (byte)Bitmap.ClusterState.Service
                                : (byte)Bitmap.ClusterState.Free;
                            currentCluster++;
                            Bitmap.FreeClusters--;
                        }

                        bw.Write(clustersInfo.GetClusterInfoByte());
                    }
                }
            }
        }

        #endregion

        public static int GetDataBlockCount(int dataSize, int blockSize)
        {
            if (dataSize <= SystemConstants.MftRecordSize - MftHeader.Length)
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

        public static int FindDirectoryMftEntry(Path path)
        {
            string[] directoryList = path.DirectoriesList.ToArray();
            int mftEntry = SystemConstants.RootDirectoryRecNumber;
            bool directoryFound = directoryList.Length == 0;

            foreach (var directoryName in directoryList)
            {
                directoryFound = false;
                DataAttributes directoryData = ReadDataAttributes(mftEntry);
                
                int filePosition = GetFilePosition(directoryName, "", directoryData.MftEntry);
                DirectoryRecord directoryRecord = GetFileInDirectory(directoryData.MftEntry, filePosition);
                if (directoryName == directoryRecord.FileName && directoryRecord.Attributes ==
                    (directoryRecord.Attributes | (byte)MftHeader.Attribute.Directory))
                {
                    directoryFound = true;
                    mftEntry = directoryRecord.Number;
                }
            }

            if (directoryFound) return mftEntry;
            return -1;
        }

        public static int GetFilePosition(string fileName, string extension, int directorMftEntry)
        {
            if (directorMftEntry < 0) return -1;
            DataAttributes directoryData = ReadDataAttributes(directorMftEntry);

            using (BinaryReader br = new BinaryReader(File.Open(SystemConstants.SystemFile, FileMode.Open)))
            {
                int offset = 0;

                if (directoryData.Blocks.Count == 0)
                {
                    br.BaseStream.Seek(directorMftEntry * SystemConstants.MftRecordSize + MftHeader.Length,
                        SeekOrigin.Begin);

                    while (offset < directoryData.Size)
                    {
                        byte[] recordBytes = br.ReadBytes(DirectoryRecord.Length);
                        if (!recordBytes.SequenceEqual(new byte[DirectoryRecord.Length]))
                        {
                            DirectoryRecord file = new DirectoryRecord(recordBytes);
                            if (fileName + (extension == "" ? "" : '.' + extension) == file.GetFullName())
                                return offset;
                        }

                        offset += DirectoryRecord.Length;
                    }
                }
                else
                {
                    int blockIndex = 0;
                    while (offset < directoryData.Size)
                    {
                        List<byte> recordBytes = new List<byte>();
                        if (SystemData.BytesPerCluster - offset % SystemData.BytesPerCluster >=
                            DirectoryRecord.Length)
                        {
                            br.BaseStream.Seek(
                                directoryData.Blocks[blockIndex] * SystemData.BytesPerCluster +
                                offset % SystemData.BytesPerCluster,
                                SeekOrigin.Begin);
                            recordBytes.AddRange(br.ReadBytes(DirectoryRecord.Length));
                        }
                        else
                        {
                            recordBytes.AddRange(
                                br.ReadBytes(SystemData.BytesPerCluster - offset % SystemData.BytesPerCluster));
                            br.BaseStream.Seek(directoryData.Blocks[++blockIndex] * SystemData.BytesPerCluster,
                                SeekOrigin.Begin);
                            recordBytes.AddRange(br.ReadBytes(DirectoryRecord.Length - recordBytes.Count));
                        }

                        if (!recordBytes.ToArray().SequenceEqual(new byte[DirectoryRecord.Length]))
                        {
                            DirectoryRecord file = new DirectoryRecord(recordBytes.ToArray());
                            if (fileName + (extension == "" ? "" : '.' + extension) == file.GetFullName())
                                return offset;
                        }

                        offset += DirectoryRecord.Length;
                    }
                }
            }

            return -1;
        }

        public static DirectoryRecord GetFileInDirectory(int directoryMftEntry, int filePosition)
        {
            if (directoryMftEntry < 0 || filePosition < 0) return null;
            DataAttributes directoryData = ReadDataAttributes(directoryMftEntry);
            DirectoryRecord directoryRecord;
            using (BinaryReader br = new BinaryReader(File.Open(SystemConstants.SystemFile, FileMode.Open)))
            {
                if (directoryData.Blocks.Count == 0)
                {
                    br.BaseStream.Seek(
                        directoryMftEntry * SystemConstants.MftRecordSize + MftHeader.Length + filePosition,
                        SeekOrigin.Begin);

                    directoryRecord = new DirectoryRecord(br.ReadBytes(DirectoryRecord.Length));
                }
                else
                {
                    int blockNumber = filePosition / SystemData.BytesPerCluster;
                    // смещение относительно блока
                    int recordOffset = filePosition % SystemData.BytesPerCluster;

                    int availableBytes = SystemData.BytesPerCluster - recordOffset;
                    br.BaseStream.Seek(
                        directoryData.Blocks[blockNumber] * SystemData.BytesPerCluster + recordOffset,
                        SeekOrigin.Begin);
                    if (availableBytes >= DirectoryRecord.Length)
                        directoryRecord = new DirectoryRecord(br.ReadBytes(DirectoryRecord.Length));
                    else
                    {
                        List<byte> recordBytes = new List<byte>();
                        recordBytes.AddRange(br.ReadBytes(availableBytes));
                        br.BaseStream.Seek(directoryData.Blocks[blockNumber + 1] * SystemData.BytesPerCluster,
                            SeekOrigin.Begin);
                        recordBytes.AddRange(br.ReadBytes(DirectoryRecord.Length - availableBytes));
                        directoryRecord = new DirectoryRecord(recordBytes.ToArray());
                    }
                }
            }

            return directoryRecord;
        }

        public static List<ExplorerFile> GetFileList(Path path)
        {
            List<DirectoryRecord> directoryRecords = GetDirectoryRecords(path);
            List<ExplorerFile> files = new List<ExplorerFile>();
            foreach (var record in directoryRecords)
            {
                MftHeader mftHeader = GetMftHeader(record.Number);
                files.Add(new ExplorerFile
                {
                    Attributes = mftHeader.Attributes,
                    FullName = mftHeader.GetFullName(),
                    CreationDate = mftHeader.CreationDate,
                    ModificationDate = mftHeader.ModificationDate,
                    Size = mftHeader.Size,
                    MftEntry = record.Number
                });
            }

            return files;
        }

        public static List<DirectoryRecord> GetDirectoryRecords(Path path)
        {
            List<DirectoryRecord> directoryRecords = new List<DirectoryRecord>();
            int directoryMftEntry = FindDirectoryMftEntry(path);
            if(directoryMftEntry == -1) return directoryRecords;
            DataAttributes directoryData = ReadDataAttributes(directoryMftEntry);
            using (BinaryReader br = new BinaryReader(File.Open(SystemConstants.SystemFile, FileMode.Open)))
            {
                int offset = 0;
                if (directoryData.Blocks.Count == 0)
                {
                    br.BaseStream.Seek(directoryMftEntry * SystemConstants.MftRecordSize + MftHeader.Length, SeekOrigin.Begin);
                    while (offset < directoryData.Size)
                    {
                        byte[] recordBytes = br.ReadBytes(DirectoryRecord.Length);
                        if (!recordBytes.SequenceEqual(new byte[DirectoryRecord.Length]))
                        {
                            DirectoryRecord file = new DirectoryRecord(recordBytes);
                            directoryRecords.Add(file);
                        }

                        offset += DirectoryRecord.Length;
                    }
                }
                else
                {
                    int blockIndex = 0;
                    while (offset < directoryData.Size)
                    {
                        List<byte> recordBytes = new List<byte>();
                        if (SystemData.BytesPerCluster - offset % SystemData.BytesPerCluster >=
                            DirectoryRecord.Length)
                        {
                            br.BaseStream.Seek(
                                directoryData.Blocks[blockIndex] * SystemData.BytesPerCluster +
                                offset % SystemData.BytesPerCluster,
                                SeekOrigin.Begin);
                            recordBytes.AddRange(br.ReadBytes(DirectoryRecord.Length));
                        }
                        else
                        {
                            recordBytes.AddRange(
                                br.ReadBytes(SystemData.BytesPerCluster - offset % SystemData.BytesPerCluster));
                            br.BaseStream.Seek(directoryData.Blocks[++blockIndex] * SystemData.BytesPerCluster,
                                SeekOrigin.Begin);
                            recordBytes.AddRange(br.ReadBytes(DirectoryRecord.Length - recordBytes.Count));
                        }

                        if (!recordBytes.ToArray().SequenceEqual(new byte[DirectoryRecord.Length]))
                        {
                            DirectoryRecord file = new DirectoryRecord(recordBytes.ToArray());
                            directoryRecords.Add(file);
                        }

                        offset += DirectoryRecord.Length;
                    }
                }
            }

            return directoryRecords;
        }
       
        static void UpdateDirectoryRecord(DataAttributes directoryData, int filePosition, DirectoryRecord record)
        {
            byte[] recordBytes = record == null ? new byte[DirectoryRecord.Length] : record.GetBytes();
            using (BinaryWriter bw = new BinaryWriter(File.Open(SystemConstants.SystemFile, FileMode.Open)))
            {
                // смещаемся на необходимый блок данных - на необходимую запись mft
                if (directoryData.Blocks.Count == 0)
                {
                    bw.BaseStream.Seek(directoryData.MftEntry * SystemConstants.MftRecordSize, SeekOrigin.Begin);
                    bw.BaseStream.Seek(MftHeader.Length + filePosition, SeekOrigin.Current);
                    bw.Write(recordBytes);
                }
                else
                {
                    int blockNumber = filePosition / SystemData.BytesPerCluster;
                    // смещение относительно блока
                    int recordOffset = filePosition % SystemData.BytesPerCluster;

                    int availableBytes = SystemData.BytesPerCluster - recordOffset;

                    bw.BaseStream.Seek(
                        directoryData.Blocks[blockNumber] * SystemData.BytesPerCluster + recordOffset,
                        SeekOrigin.Begin);
                    if (availableBytes >= DirectoryRecord.Length)
                        bw.Write(recordBytes);
                    else
                    {
                        bw.Write(recordBytes.GetRange(0, availableBytes));
                        bw.BaseStream.Seek(directoryData.Blocks[blockNumber + 1] * SystemData.BytesPerCluster,
                            SeekOrigin.Begin);
                        bw.Write(recordBytes.GetRange(availableBytes, DirectoryRecord.Length - availableBytes));
                    }
                }
            }
        }

        static int GetNewMftEntry()
        {
            using (BinaryReader br= new BinaryReader(File.Open(SystemConstants.SystemFile, FileMode.Open)))
            {
                br.BaseStream.Seek(SystemConstants.MftRecNumber * SystemConstants.MftRecordSize, SeekOrigin.Begin);
                MftHeader mftItselfHeader = new MftHeader(br.ReadBytes(MftHeader.Length));
                
                // Просматриваем все записи MFT, начиная с первого доступного номера, т.е. пропуская метафайлы.
                int mftEntry = SystemConstants.ServiceFileCount;
                br.BaseStream.Seek(mftEntry * SystemConstants.MftRecordSize, SeekOrigin.Begin);
                while (mftEntry * SystemConstants.MftRecordSize < mftItselfHeader.Size)
                {
                    if (br.ReadByte() == (byte)MftHeader.Signature.NotUsed)
                        return mftEntry;
                    br.BaseStream.Seek(SystemConstants.MftRecordSize - 1, SeekOrigin.Current);
                    mftEntry++;
                }

                // Проверяем, будет ли главный файл MFT помещаться в MFT зону,
                // если увечилить его размер ещё на одну запись.
                if ((mftEntry + 1) * SystemConstants.MftRecordSize <= SystemData.MftAreaSize)
                    return mftEntry;
                return -1;
            }
        }

        static int GetNewRecordPositionInDirectory(DataAttributes directoryData)
        {
            using (BinaryReader br = new BinaryReader(File.Open(SystemConstants.SystemFile, FileMode.Open)))
            {
                int offset = 0;
                if (directoryData.Blocks.Count == 0)
                {
                    br.BaseStream.Seek(directoryData.MftEntry * SystemConstants.MftRecordSize + MftHeader.Length, SeekOrigin.Begin);
                    while (offset < directoryData.Size)
                    {
                        if (br.ReadBytes(DirectoryRecord.Length).SequenceEqual(new byte[DirectoryRecord.Length]))
                            return offset;
                        offset += DirectoryRecord.Length;
                    }

                    if (offset + DirectoryRecord.Length <= SystemConstants.MftRecordSize - MftHeader.Length)
                        return offset;
                }
                else
                {
                    int blockIndex = 0;
                    while (offset < directoryData.Size)
                    {
                        List<byte> recordBytes = new List<byte>();
                        if (SystemData.BytesPerCluster - offset % SystemData.BytesPerCluster >=
                            DirectoryRecord.Length)
                        {
                            br.BaseStream.Seek(
                                directoryData.Blocks[blockIndex] * SystemData.BytesPerCluster +
                                offset % SystemData.BytesPerCluster,
                                SeekOrigin.Begin);
                            recordBytes.AddRange(br.ReadBytes(DirectoryRecord.Length));
                        }
                        else
                        {
                            recordBytes.AddRange(
                                br.ReadBytes(SystemData.BytesPerCluster - offset % SystemData.BytesPerCluster));
                            br.BaseStream.Seek(directoryData.Blocks[++blockIndex] * SystemData.BytesPerCluster,
                                SeekOrigin.Begin);
                            recordBytes.AddRange(br.ReadBytes(DirectoryRecord.Length - recordBytes.Count));
                        }

                        if (recordBytes.ToArray().SequenceEqual(new byte[DirectoryRecord.Length]))
                            return offset;
                        offset += DirectoryRecord.Length;
                    }

                    if (offset + DirectoryRecord.Length <= directoryData.Blocks.Count * SystemData.BytesPerCluster)
                        return offset;
                }

                return -1;
            }
        }

        private static void ShiftMftDataToFileArea(int mftEntry, int dataSize, int block)
        {
            using (BinaryReader br = new BinaryReader(File.Open(SystemConstants.SystemFile, FileMode.Open)))
            {
                br.BaseStream.Seek(
                    mftEntry * SystemConstants.MftRecordSize + MftHeader.Length, SeekOrigin.Begin);
                var dataBytes = br.ReadBytes(dataSize);

                using (BinaryWriter bw = new BinaryWriter(br.BaseStream))
                {
                    bw.BaseStream.Seek(block * SystemData.BytesPerCluster, SeekOrigin.Begin);
                    bw.Write(dataBytes);
                }
            }
        }

        public static int CreateFile(MftHeader file, Path path)
        {
            int directoryMftEntry = FindDirectoryMftEntry(path);
            MftHeader parentDirectory = GetMftHeader(directoryMftEntry);
            if (!parentDirectory.HasPermissions(Account.User.Id, Permission.Rights.W)) return -4;

            int mftRecordNumber = GetNewMftEntry();
            if (mftRecordNumber == -1) return -1;

           
            DataAttributes directoryData = ReadDataAttributes(directoryMftEntry);

            bool needNewDataBlock = false;
            int filePosition = GetNewRecordPositionInDirectory(directoryData);
            if (filePosition == -1)
            {
                if (Bitmap.FreeClusters == 0) return -1;
                needNewDataBlock = true;
                filePosition = directoryData.Size;
            }

            // Проверяем, не существует ли уже файл с таким именем.
            DirectoryRecord record = GetFileInDirectory(directoryMftEntry, GetFilePosition(file.FileName, file.Extension, directoryMftEntry));
            if (record != null)
                return record.Attributes == (record.Attributes | (byte)MftHeader.Attribute.Directory) ? -2 : -3;

            
            if (needNewDataBlock)
            {
                int newBlock = Bitmap.GetFreeClusters(1)[0];
                Bitmap.SetClustersState(new List<int> { newBlock }, Bitmap.ClusterState.Busy);
                if (directoryData.Blocks.Count == 0)
                    ShiftMftDataToFileArea(directoryMftEntry, directoryData.Size, newBlock);
                directoryData.Blocks.Add(newBlock);
                if(filePosition == directoryData.Size)
                    directoryData.Size += DirectoryRecord.Length;
                WriteData(directoryData, null);
            }
            
            UpdateMftEntry(file, mftRecordNumber);

            MftHeader mft = GetMftHeader(SystemConstants.MftRecNumber);
            if (mftRecordNumber * SystemConstants.MftRecordSize == mft.Size)
            {
                mft.Size += SystemConstants.MftRecordSize;
                UpdateMftEntry(mft, SystemConstants.MftRecNumber);
            }

            MftHeader directoryMftHeader = GetMftHeader(directoryMftEntry);
            directoryMftHeader.ModificationDate = new MyDateTime(DateTime.Now);
            if (filePosition == directoryData.Size)
                directoryMftHeader.Size += DirectoryRecord.Length;
            UpdateMftEntry(directoryMftHeader, directoryMftEntry);

            UpdateDirectoryRecord(directoryData, filePosition, new DirectoryRecord(file, mftRecordNumber));

            return mftRecordNumber;
        }

    public static void Copy(Path path, ExplorerFile record)
        {
            int dotIndex = record.FullName.LastIndexOf('.');
            string extension = dotIndex == -1 || record.Attributes ==
                               (record.Attributes | (byte)MftHeader.Attribute.Directory)
                ? ""
                : record.FullName.Substring(dotIndex + 1, record.FullName.Length - dotIndex - 1);
            string fileName =
                record.FullName.Substring(0, extension.Length > 0 ? record.FullName.Length - extension.Length - 1 : record.FullName.Length);
            int directoryMtfEntry = FindDirectoryMftEntry(path);
            DirectoryRecord bufferRecord = new DirectoryRecord
            {
                Attributes = record.Attributes,
                CreationDate = record.CreationDate,
                Extension = extension,
                FileName = fileName,
                Number = GetFileInDirectory(directoryMtfEntry, GetFilePosition(fileName, extension, directoryMtfEntry)).Number
            };

            SystemData.Buffer = new SystemBuffer { Path = new Path(path), Record = bufferRecord };
        }

        public static List<DirectoryRecord> CopyRecursively(DirectoryRecord sourceDirectory, Path sourcePath, DirectoryRecord directoryCopy, Path destinationPath)
        {
            List<DirectoryRecord> copyList = new List<DirectoryRecord>();
            int filePosition = 0;
            MftHeader sourceDirectoryHeader = GetMftHeader(sourceDirectory.Number);
            while (filePosition < sourceDirectoryHeader.Size)
            {
                DirectoryRecord sourceFile = GetFileInDirectory(sourceDirectory.Number, filePosition);
                MftHeader fileCopy = GetMftHeader(sourceFile.Number);
                fileCopy.CreationDate = fileCopy.ModificationDate = new MyDateTime(DateTime.Now);
                fileCopy.Size = 0;

                int mftEntry = CreateFile(fileCopy, destinationPath);
                //if(-1)
                if (sourceFile.Attributes == (sourceFile.Attributes | (byte)MftHeader.Attribute.Directory))
                {
                    
                    Path from = new Path(sourcePath);
                    from.Add(sourceFile.FileName);
                    Path to = new Path(destinationPath);
                    to.Add(fileCopy.FileName);

                    List<DirectoryRecord> childCopyFileList = CopyRecursively(new DirectoryRecord(GetMftHeader(sourceFile.Number), sourceFile.Number), from, new DirectoryRecord(fileCopy, mftEntry), to);
                
                    int position = 0;
                    fileCopy.Size = childCopyFileList.Count * DirectoryRecord.Length; // Если не хватало памяти, то не все записи были скопированы и размер директории будет меньше.
                    foreach (var childFileCopy in childCopyFileList)
                    {
                        UpdateDirectoryRecord(ReadDataAttributes(mftEntry), position, childFileCopy);
                        position += DirectoryRecord.Length;
                    }
                    copyList.Add(new DirectoryRecord(fileCopy, mftEntry));
                }
                else
                {
                    SaveFile(new SystemBuffer { Record = new DirectoryRecord(fileCopy, mftEntry), Path = new Path(destinationPath) },
                        Encoding.UTF8.GetString(ReadFileData(ReadDataAttributes(sourceFile.Number))));
                    copyList.Add(new DirectoryRecord(GetMftHeader(mftEntry), mftEntry));
                }

                filePosition += DirectoryRecord.Length;
            }

            return copyList;
        }

        public static void Paste(Path path)
        {
            if (SystemData.Buffer != null)
            {
                if (path.CurrentPath!= (SystemData.Buffer.Path.CurrentPath) && path.CurrentPath.Contains(SystemData.Buffer.Path.CurrentPath))
                {
                    MessageBox.Show(
                        "Конечная папка, в которую следует поместить файлы, является дочерней для папки, в которой они находятся",
                        "Ошибка вставки файла", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                // проверка на права
                MftHeader copy = GetMftHeader(SystemData.Buffer.Record.Number);
                copy.CreationDate = copy.ModificationDate = new MyDateTime(DateTime.Now);
                copy.Size = 0;

                int mftEntry, attempt = 1;
                string name = copy.FileName;
                while ((mftEntry = CreateFile(copy, path)) < -1)
                    copy.FileName = name + " (" + attempt++ + ")";

                if (copy.Attributes == (copy.Attributes | (byte) MftHeader.Attribute.Directory))
                {
                    Path from = new Path(SystemData.Buffer.Path);
                    from.Add(SystemData.Buffer.Record.FileName);
                    Path to = new Path(path);
                    to.Add(copy.FileName);
                    List<DirectoryRecord> childCopyFileList =
                        CopyRecursively(new DirectoryRecord(GetMftHeader(SystemData.Buffer.Record.Number), SystemData.Buffer.Record.Number), from, new DirectoryRecord(copy, mftEntry), to);
                    // Если не хватало памяти, то не все записи были скопированы и размер директории будет меньше.
                    copy.Size = childCopyFileList.Count * DirectoryRecord.Length;
                }
                else
                {
                    SaveFile(new SystemBuffer {Record = new DirectoryRecord(copy, mftEntry), Path = new Path(path)},
                        Encoding.UTF8.GetString(ReadFileData(ReadDataAttributes(SystemData.Buffer.Record.Number))));
                }
            }
        }

        public static MftHeader GetMftHeader(int recordNumber)
        {
            if (recordNumber < 0) return null;
            using (BinaryReader br = new BinaryReader(File.Open(SystemConstants.SystemFile, FileMode.Open)))
            {
                br.BaseStream.Seek(recordNumber * SystemConstants.MftRecordSize, SeekOrigin.Begin);
                return new MftHeader(br.ReadBytes(MftHeader.Length));
            }
        }

        public static User GetUserById(int uid)
        {
            DataAttributes usersData = ReadDataAttributes(SystemConstants.UserListRecNumber);
            using (BinaryReader br = new BinaryReader(File.Open(SystemConstants.SystemFile, FileMode.Open)))
            {
                int offset = 0;
                if (usersData.Blocks.Count == 0)
                {
                    br.BaseStream.Seek(SystemConstants.UserListRecNumber * SystemConstants.MftRecordSize + MftHeader.Length, SeekOrigin.Begin);
                    while (offset < usersData.Size)
                    {
                        byte[] userBytes = br.ReadBytes(User.AccountLength);
                        if (!userBytes.SequenceEqual(new byte[User.AccountLength]))
                        {
                            User user = new User(userBytes);
                            if (user.Id == uid) return user;
                        }

                        offset += User.AccountLength;
                    }
                }
                else
                {
                    int blockIndex = 0;
                    while (offset < usersData.Size)
                    {
                        List<byte> userBytes = new List<byte>();
                        if (SystemConstants.MftRecordSize - 1 - offset % (SystemConstants.MftRecordSize - 1) >=
                            User.AccountLength)
                        {
                            br.BaseStream.Seek(
                                usersData.Blocks[blockIndex] * (SystemConstants.MftRecordSize - 1) +
                                offset % SystemConstants.MftRecordSize,
                                SeekOrigin.Begin);
                            userBytes.AddRange(br.ReadBytes(User.AccountLength));
                        }
                        else
                        {
                            userBytes.AddRange(
                                br.ReadBytes(SystemConstants.MftRecordSize - 1 - offset % (SystemConstants.MftRecordSize - 1)));
                            br.BaseStream.Seek(usersData.Blocks[++blockIndex] * SystemConstants.MftRecordSize - 1,
                                SeekOrigin.Begin);
                            userBytes.AddRange(br.ReadBytes(User.AccountLength - userBytes.Count));
                        }

                        if (!userBytes.SequenceEqual(new byte[User.AccountLength]))
                        {
                            User user = new User(userBytes.ToArray());
                            if (user.Id == uid) return user;
                        }

                        offset += User.AccountLength;
                    }
                }
            }

            return null;
        }

        public static DataAttributes ReadDataAttributes(int mftEntry)
        {
            using (BinaryReader br = new BinaryReader(File.Open(SystemConstants.SystemFile, FileMode.Open)))
            {
                br.BaseStream.Seek(mftEntry * SystemConstants.MftRecordSize + 1, SeekOrigin.Begin);
                byte attributes = br.ReadByte();
                bool isSystem = attributes == (attributes | (byte)MftHeader.Attribute.System);
                int blockSize =
                    isSystem ? SystemConstants.MftRecordSize - 1 : SystemData.BytesPerCluster;
                br.BaseStream.Seek(31, SeekOrigin.Current);
                int dataSize = br.ReadInt32();

                br.BaseStream.Seek(13, SeekOrigin.Current);
                int blockCount = GetDataBlockCount(dataSize, blockSize);
                int[] blocks = new int[blockCount]; // Список номеров блоков, в которых содержатся данные.
                for (int i = 0; i < blockCount; i++)
                    blocks[i] = br.ReadInt32();

                return new DataAttributes(isSystem, mftEntry, dataSize, blocks);
            }
        }

        public static void WriteData(DataAttributes dataAttributes, byte[] data)
        {
            using (BinaryWriter bw = new BinaryWriter(File.Open(SystemConstants.SystemFile, FileMode.Open)))
            {
                bw.Seek(dataAttributes.MftEntry * SystemConstants.MftRecordSize + 33,
                    SeekOrigin.Begin);
                bw.Write(dataAttributes.Size);

                bw.Seek(13, SeekOrigin.Current);
                
                foreach (var block in dataAttributes.Blocks)
                    bw.Write(block);

                if(data == null) return;

                if (dataAttributes.Blocks.Count == 0)
                    bw.Write(data);
                else
                {
                    int needToWriteBytes = data.Length;
                    foreach (var block in dataAttributes.Blocks)
                    {
                        int byteCount = needToWriteBytes < SystemData.BytesPerCluster
                            ? needToWriteBytes
                            : SystemData.BytesPerCluster;
                        bw.Seek(block * SystemData.BytesPerCluster, SeekOrigin.Begin);
                        bw.Write(data.GetRange(data.Length - needToWriteBytes, byteCount));
                        needToWriteBytes -= SystemData.BytesPerCluster;
                    }
                }
            }
        }

        public static byte[] ReadFileData(DataAttributes dataAttributes)
        {
            List<byte> dataBytes = new List<byte>();
            using (BinaryReader br = new BinaryReader(File.Open(SystemConstants.SystemFile, FileMode.Open)))
            {
                br.BaseStream.Seek(dataAttributes.MftEntry * SystemConstants.MftRecordSize + MftHeader.Length,
                    SeekOrigin.Begin);
                
                if (dataAttributes.Blocks.Count == 0)
                    dataBytes.AddRange(br.ReadBytes(dataAttributes.Size));
                else
                {
                    int needToReadBytes = dataAttributes.Size;
                    foreach (var block in dataAttributes.Blocks)
                    {
                        int byteCount = needToReadBytes > SystemData.BytesPerCluster
                            ? SystemData.BytesPerCluster
                            : needToReadBytes;
                        br.BaseStream.Seek(block * SystemData.BytesPerCluster, SeekOrigin.Begin);
                        dataBytes.AddRange(br.ReadBytes(byteCount));
                        needToReadBytes -= SystemData.BytesPerCluster;
                    }
                }

                return dataBytes.ToArray();
            }
        }

        public static void Rename(DirectoryRecord record, string fileName, string extension, Path path)
        {
            MftHeader file = GetMftHeader(record.Number);
            file.FileName = fileName;
            file.Extension = extension;

            UpdateMftEntry(file, record.Number);
            int directoryMftEntry = FindDirectoryMftEntry(path);
            int filePosition = GetFilePosition(record.FileName, record.Extension, directoryMftEntry);
            UpdateDirectoryRecord(ReadDataAttributes(directoryMftEntry), filePosition, new DirectoryRecord(file, record.Number));
        }

        public static void SaveFile(SystemBuffer buffer, string data)
        {
            DataAttributes fileDataAttributes = ReadDataAttributes(buffer.Record.Number);
            int requiredClusterCount = GetDataBlockCount(data.Length, SystemData.BytesPerCluster) - fileDataAttributes.Blocks.Count;
            if (requiredClusterCount > 0)
            {
                List<int> clusterNumbers = Bitmap.GetFreeClusters(requiredClusterCount);
                if (clusterNumbers.Count < requiredClusterCount)
                    MessageBox.Show("Недостаточно места на диске!", "Ошибка сохранения файла", MessageBoxButton.OK,
                        MessageBoxImage.Error);
                else
                {
                    fileDataAttributes.Blocks.AddRange(clusterNumbers);
                    Bitmap.SetClustersState(clusterNumbers, Bitmap.ClusterState.Busy);
                }
            }
            else if (requiredClusterCount < 0)
            {
                Bitmap.SetClustersState(fileDataAttributes.Blocks.GetRange(fileDataAttributes.Blocks.Count + requiredClusterCount,
                    Math.Abs(requiredClusterCount)), Bitmap.ClusterState.Free);
                fileDataAttributes.Blocks.RemoveRange(fileDataAttributes.Blocks.Count + requiredClusterCount,
                    Math.Abs(requiredClusterCount));
            }

            MftHeader file = GetMftHeader(buffer.Record.Number);
            file.ModificationDate = new MyDateTime(DateTime.Now);
            file.Size = fileDataAttributes.Size = data.Length;

            UpdateMftEntry(file, buffer.Record.Number);
            WriteData(fileDataAttributes, data.GetFormatBytes(data.Length));
            int directoryMftEntry = FindDirectoryMftEntry(buffer.Path);
            int filePosition = GetFilePosition(buffer.Record.FileName, buffer.Record.Extension, directoryMftEntry); 
            UpdateDirectoryRecord(ReadDataAttributes(directoryMftEntry), filePosition, buffer.Record);
        }

        public static void Delete(Path path, DirectoryRecord record)
        {
            if (record.Attributes == (record.Attributes | (byte) MftHeader.Attribute.Directory))
                DeleteRecursively(record);

            // удаляем запись в родительской директории
            int directoryMftEntry = FindDirectoryMftEntry(new Path(path));
            int filePosition = GetFilePosition(record.FileName, record.Extension, directoryMftEntry);
            UpdateDirectoryRecord(ReadDataAttributes(directoryMftEntry), filePosition, null);
            
            // Очищаем кластеры
            DataAttributes recordData = ReadDataAttributes(record.Number);
            Bitmap.SetClustersState(recordData.Blocks, Bitmap.ClusterState.Free);

            // Ставим пометку о неиспользуемой записи mft
            UpdateMftEntry(null, record.Number);

            //изменяем размер и время последней модификации родительской директории
            MftHeader directoryHeader = GetMftHeader(directoryMftEntry);
            directoryHeader.ModificationDate = new MyDateTime(DateTime.Now);
            UpdateMftEntry(directoryHeader, directoryMftEntry);
        }

        static int DeleteRecursively(DirectoryRecord directory)
        {
            int totalFileCount = 0;
            int filePosition = 0;
            MftHeader sourceDirectoryHeader = GetMftHeader(directory.Number);
            while (filePosition < sourceDirectoryHeader.Size)
            {
                DirectoryRecord file = GetFileInDirectory(directory.Number, filePosition);
                
                if (file.Attributes == (file.Attributes | (byte)MftHeader.Attribute.Directory))
                    totalFileCount += DeleteRecursively(file);
                else
                {
                    UpdateDirectoryRecord(ReadDataAttributes(file.Number), filePosition, null);
                    
                    // Очищаем кластеры
                    DataAttributes recordData = ReadDataAttributes(file.Number);
                    Bitmap.SetClustersState(recordData.Blocks, Bitmap.ClusterState.Free);

                    // Ставим пометку о неиспользуемой записи mft
                    UpdateMftEntry(null, file.Number);
                }
                
                filePosition += DirectoryRecord.Length;
                totalFileCount++;
            }

            return totalFileCount;
        }
    }
}