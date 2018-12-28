using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MyOS.FileSystem.SpecialDataTypes;
using MyOS.ViewModels;
using Path = MyOS.FileSystem.SpecialDataTypes.Path;
using Constants = MyOS.FileSystem.FileSystem.Constants;

namespace MyOS.FileSystem
{
    static class SystemCalls
    {
        public static void Formatting(FormattingOptions options)
        {
            int mftAreaSize = options.VolumeSize / 10 - options.VolumeSize / 10 % Constants.MftRecordSize;
            using (var fileStream = new FileStream(Constants.SystemFile, FileMode.Create, FileAccess.Write, FileShare.None))
                fileStream.SetLength(options.VolumeSize);

            // По умолчанию в системе создаётся администратор.
            UserRecord administrator = new UserRecord("admin", "admin", 0, true, Constants.RootMftEntry);
            MftHeader.Attribute systemReadOnlyHidden = MftHeader.Attribute.ReadOnly | MftHeader.Attribute.Hidden;

            // Создание файла $MFT, представляющего централизованный каталог
            // всех остальных файлов диска и себя самого.
            // Первые 6 записей - метафайлы (имеют фиксированное положение на диске).
            // 1 файл - запись о самом MFT.
            // 2 файл - запись о копии первых записей MFT.
            // 3 файл - $Volume.
            // 4 файл - / (корневой каталог).
            // 5 файл - $Bitmap (битовая карта).
            // 6 файл - $Users (список пользователей системы).

            MftHeader mft = new MftHeader("$MFT", "",
                size: Constants.ServiceFileCount * Constants.MftRecordSize,
                attributes: systemReadOnlyHidden, userId: administrator.Id);
            MftHeader mftMirr = new MftHeader("$MFTMirr", "",
                size: Constants.ServiceFileCount * Constants.MftRecordSize,
                attributes: systemReadOnlyHidden, userId: administrator.Id);
            MftHeader volume = new MftHeader("$Volume", "", size: 15, attributes: systemReadOnlyHidden,
                userId: administrator.Id);
            MftHeader root = new MftHeader("/", "", MftHeader.Attribute.Directory, userId: administrator.Id);
            // Размер битовой карты = (количество_кластеров * 2 бита / 8 битов в байте = 25600 байтов).
            int bitmapSize = options.VolumeSize / options.BytesPerCluster * 2 / 8;
            MftHeader bitmap = new MftHeader("$Bitmap", "", size: bitmapSize, attributes: systemReadOnlyHidden,
                userId: administrator.Id);
            MftHeader users = new MftHeader("$Users", "", size: UserRecord.Length, attributes: systemReadOnlyHidden,
                userId: administrator.Id);

            #region Записываем служебные записи в главную файловую таблицу MFT.

            UpdateMftEntry(mft, Constants.MftEntry);
            UpdateMftEntry(mftMirr, Constants.MftMirrEntry);
            UpdateMftEntry(volume, Constants.VolumeMftEntry);
            UpdateMftEntry(root, Constants.RootMftEntry);
            UpdateMftEntry(bitmap, Constants.BitmapMftEntry);
            UpdateMftEntry(users, Constants.UserListMftEntry);
            UpdateRecord(Constants.UserListMftEntry, UserRecord.Empty, administrator);

            #endregion

            #region Записываем системную информацию в файлы.

            List<int> mftMirrowClusters = new List<int>(GetClustersSequence(
                CalculateClustersNumber(Constants.ServiceFileCount * Constants.MftRecordSize, options.BytesPerCluster),
                options.VolumeSize / options.BytesPerCluster / 2));
            WriteClusterNumbers(Constants.MftMirrEntry, mftMirrowClusters.ToArray());
            WriteMftMirrowData(new[] { mft, mftMirr, volume, root, bitmap, users }, mftMirrowClusters.ToArray(),
                options.BytesPerCluster);
            WriteVolumeData(options.VolumeName, options.FsName, options.BytesPerCluster);
            List<int> bitmapClusters =
                new List<int>(GetClustersSequence(CalculateClustersNumber(bitmapSize, options.BytesPerCluster),
                    mftAreaSize / options.BytesPerCluster));
            WriteClusterNumbers(Constants.BitmapMftEntry, bitmapClusters.ToArray());
            WriteInitialBitmap(bitmapClusters.ToArray(), options.VolumeSize, options.BytesPerCluster,
                 mftMirrowClusters.ToArray());

            #endregion
        }

        static void WriteMftMirrowData(MftHeader[] mftHeaders, int[] mftMirrowClusters, int bytesPerCluster)
        {
            List<byte> bytes = new List<byte>();
            foreach (var mftHeader in mftHeaders)
            {
                bytes.AddRange(mftHeader.GetBytes());
                bytes.AddRange(new byte[Constants.MftRecordSize - MftHeader.Length]);
            }

            using (BinaryWriter bw = new BinaryWriter(File.Open(Constants.SystemFile, FileMode.Open)))
            {
                int needToWriteBytes = bytes.Count;
                foreach (var block in mftMirrowClusters)
                {
                    int byteCount = needToWriteBytes < bytesPerCluster
                        ? needToWriteBytes
                        : bytesPerCluster;
                    bw.Seek(block * bytesPerCluster, SeekOrigin.Begin);
                    bw.Write(bytes.GetRange(bytes.Count - needToWriteBytes, byteCount).ToArray());
                    needToWriteBytes -= bytesPerCluster;
                }
            }
        }

        static void WriteVolumeData(string volumeName, string fsName, int bytesPerCluster)
        {
            List<byte> volumeData = new List<byte>();
            volumeData.AddRange((volumeName + fsName).GetFormatBytes(10));
            volumeData.Add(0); // Том исправен.
            volumeData.AddRange(BitConverter.GetBytes(bytesPerCluster));

            using (BinaryWriter bw = new BinaryWriter(File.Open(Constants.SystemFile, FileMode.Open)))
            {
                bw.BaseStream.Seek(Constants.VolumeMftEntry * Constants.MftRecordSize + MftHeader.Length, SeekOrigin.Begin);
                bw.Write(volumeData.ToArray());
            }
        }

        static int[] GetClustersSequence(int number, int firstBlock)
        {
            int[] dataBlocks = new int[number];
            for (int i = 0; i < dataBlocks.Length; i++)
                dataBlocks[i] = firstBlock + i;
            return dataBlocks;
        }


        public static void WriteInitialBitmap(int[] bitmapClusters, int volumeSize, int bytesPerCluster, int[] mftMirrowClusters)
        {
            int mftZoneSize = volumeSize / 10 - volumeSize / 10 % Constants.MftRecordSize;
            int mftClusterСount = mftZoneSize / bytesPerCluster;
            int clusterCount = volumeSize / bytesPerCluster;
            int currentCluster = 0;
            byte serviceCluster = 0b10;
            using (BinaryWriter bw = new BinaryWriter(File.Open(Constants.SystemFile, FileMode.Open)))
            {
                foreach (var block in bitmapClusters)
                {
                    bw.BaseStream.Seek(block * bytesPerCluster, SeekOrigin.Begin);
                    for (int i = 0; i < bytesPerCluster; i++)
                    {
                        // Кластер представляется 2 битами, значит в байте - информация о 4 кластерах.
                        byte[] clustersInfo = new byte[4]; // Массив, представляющий информацию о 4 кластерах.
                        for (int j = 0; j < 4; j++)
                        {
                            clustersInfo[j] = currentCluster < mftClusterСount ||
                                              mftMirrowClusters.Contains(currentCluster) ||
                                              bitmapClusters.Contains(currentCluster)
                                ? serviceCluster
                                : (byte)0;
                            currentCluster++;

                            if (currentCluster >= clusterCount) return;
                        }

                        byte infoByte = 0;
                        // Заполняем побитово байт с информацией о кластерах.
                        // Порядок кластеров - обратный, для удобства считывания.
                        for (int k = clustersInfo.Length - 1; k >= 0; k--)
                        {
                            // В последние два бита записываем информацию о кластере.
                            infoByte = (byte)(infoByte | clustersInfo[k]);
                            // Сдвигаем байт влево на два бита, осаобождая место под информацию о следующем кластере.
                            if (k != 0) infoByte = (byte)(infoByte << 2);
                        }

                        bw.Write(infoByte);
                    }
                }
            }
        }

        public static int CalculateClustersNumber(int dataSize, int bytesPerCluster = 0)
        {
            if (dataSize <= Constants.MftRecordSize - MftHeader.Length)
                return 0;
            return (int)Math.Ceiling((double)dataSize / (bytesPerCluster == 0 ?
                FileSystem.BytesPerCluster : bytesPerCluster));
        }

        public static List<int> GetFreeClusters(int requiredNumber)
        {
            List<int> clusterNumbers = new List<int>();
            Data data = ReadMftData(Constants.BitmapMftEntry);
            using (BinaryReader br = new BinaryReader(File.Open(Constants.SystemFile, FileMode.Open)))
            {
                int byteNumber = 0; // Номер прочитанного байта в битовой карте.
                foreach (var block in data.Clusters)
                {
                    br.BaseStream.Seek(block * FileSystem.BytesPerCluster, SeekOrigin.Begin);
                    for (int i = 0; i < FileSystem.BytesPerCluster; i++)
                    {
                        if (byteNumber >= data.Size) return clusterNumbers;

                        byte fourClustersInfo = br.ReadByte();
                        // Кластер представляется 2 битами, значит в байте - информация о 4 кластерах.
                        for (int j = 0; j < 4; j++)
                        {
                            // Обнуляем первые шесть битов, при этом получая 2 младших бита,
                            // представляющих информацию об одном кластере.
                            if ((fourClustersInfo & 0b00000011) == (byte)ClusterState.Free)
                            {
                                clusterNumbers.Add(byteNumber * 4 + j); // Добавляем номер свободного кластера.
                                if (clusterNumbers.Count == requiredNumber)
                                    return clusterNumbers;
                            }

                            // Сдвигаемся на 2 бита вправо для получения информации о следующем кластере.
                            fourClustersInfo = (byte)(fourClustersInfo >> 2);
                        }
                        byteNumber++;
                    }
                }
            }

            return clusterNumbers;
        }

        public static void SetClustersState(List<int> clusterNumbers, ClusterState state)
        {
            Data data = ReadMftData(Constants.BitmapMftEntry);
            using (BinaryReader br = new BinaryReader(File.Open(Constants.SystemFile, FileMode.Open)))
            {
                // Смещаемся на поле данных (на атрибут Data) записи $Bitmap.
                foreach (var clusterNumber in clusterNumbers)
                {
                    // Получаем номер байта, в котором записан данный кластер.
                    // В одном байте - информация о 4 кластерах.
                    int clusterByteNumber = clusterNumber / 4;
                    // Получаем порядковый номер кластера в данном байте.
                    int clusterOffset = clusterNumber % 4;

                    // Получаем номер блока данных битовой карты, в которой находится необходимый байт,
                    // и порядковый номер байта в блоке.
                    int blockNumber = clusterByteNumber / FileSystem.BytesPerCluster;
                    int byteOffset = clusterByteNumber % FileSystem.BytesPerCluster;

                    br.BaseStream.Seek(data.Clusters[blockNumber] * FileSystem.BytesPerCluster + byteOffset,
                        SeekOrigin.Begin);

                    byte modifiedByte = ModifyByte(br.ReadByte(), clusterOffset, state);

                    BinaryWriter bw = new BinaryWriter(br.BaseStream);
                    bw.BaseStream.Position -= 1; // Смещаемся на один байт назад для его перезаписи.

                    bw.Write(modifiedByte);
                    if (state == ClusterState.Free) FileSystem.FreeClusters++;
                    else FileSystem.FreeClusters--;
                }
            }
        }

        private static byte ModifyByte(byte sourceByte, int clusterNumberInByte, ClusterState state)
        {
            // Создаём байт, содержащий в двух младших битах информацию о состоянии кластера.
            byte stateByte = (byte)state;

            // Сдвигаем биты кластера на заданное смещение.
            for (int j = clusterNumberInByte; j > 0; j--)
                stateByte = (byte)(stateByte << 2);

            byte byteMask = 0;
            if (clusterNumberInByte == 0)
                byteMask = 0b11_11_11_00;
            if (clusterNumberInByte == 1)
                byteMask = 0b11_11_00_11;
            if (clusterNumberInByte == 2)
                byteMask = 0b11_00_11_11;
            if (clusterNumberInByte == 3)
                byteMask = 0b00_11_11_11;

            sourceByte &= byteMask;
            sourceByte |= stateByte;

            return sourceByte;
        }

        public static void UpdateMftEntry(MftHeader mftHeader, int mftEntry)
        {
            using (BinaryWriter bw = new BinaryWriter(File.Open(Constants.SystemFile, FileMode.Open)))
            {
                bw.BaseStream.Seek(mftEntry * Constants.MftRecordSize, SeekOrigin.Begin);

                if (mftHeader == null)
                    bw.Write((byte)MftHeader.Signature.NotUsed);
                else bw.Write(mftHeader.GetBytes());
            }
        }

        public static List<MftHeader> GetAllMftHeaders()
        {
            List<MftHeader> mftHeaders = new List<MftHeader>();
            using (BinaryReader br = new BinaryReader(File.Open(Constants.SystemFile, FileMode.Open)))
            {
                br.BaseStream.Seek(Constants.MftEntry * Constants.MftRecordSize, SeekOrigin.Begin);
                MftHeader mft = new MftHeader(br.ReadBytes(MftHeader.Length));
                mftHeaders.Add(mft);
                for (int i = Constants.MftRecordSize; i < mft.Size;)
                {
                    br.BaseStream.Seek(Constants.MftRecordSize - MftHeader.Length, SeekOrigin.Current);
                    mftHeaders.Add(new MftHeader(br.ReadBytes(MftHeader.Length)));
                    i += Constants.MftRecordSize;
                }

                return mftHeaders;
            }
        }

        public static MftHeader GetMftHeader(int mftEntry)
        {
            if (mftEntry < 0) return null;
            using (BinaryReader br = new BinaryReader(File.Open(Constants.SystemFile, FileMode.Open)))
            {
                br.BaseStream.Seek(mftEntry * Constants.MftRecordSize, SeekOrigin.Begin);
                MftHeader mftHeader = new MftHeader(br.ReadBytes(MftHeader.Length));
                if (mftHeader.Sign == MftHeader.Signature.InUse)
                    return mftHeader;
                return null;
            }
        }

        public static int FindDirectoryMftEntry(Path path)
        {
            string[] directoryList = path.DirectoriesList.ToArray();
            int mftEntry = Constants.RootMftEntry;
            bool directoryFound = directoryList.Length == 0;

            // Проходим по всем директориям указанного пути
            // для поиска конечной папки.
            foreach (var directoryName in directoryList)
            {
                directoryFound = false;
                DirectoryRecord directoryRecord = (DirectoryRecord)GetRecord(directoryName,
                    mftEntry, DirectoryRecord.Length);
                if (directoryRecord == null) return -1;
                if (directoryRecord.HasAttribute(MftHeader.Attribute.Directory))
                {
                    directoryFound = true;
                    mftEntry = directoryRecord.MftEntry;
                }
            }

            if (directoryFound) return mftEntry;
            return -1;
        }

        public static IRecord GetRecord(string name, int targetMftEntry, int recordLength)
        {
            if (targetMftEntry < 0) return null;

            Data data = ReadMftData(targetMftEntry);
            using (BinaryReader br = new BinaryReader(File.Open(Constants.SystemFile, FileMode.Open)))
            {
                int offset = 0; // Смещение относительно начала блока данных Mft-записи.
                if (data.Clusters.Count == 0) // Данные содержатся в одной Mft-записи.
                {
                    br.BaseStream.Seek(targetMftEntry * Constants.MftRecordSize + MftHeader.Length,
                        SeekOrigin.Begin);

                    while (offset < data.Size)
                    {
                        byte[] recordBytes = br.ReadBytes(recordLength);
                        // Если данная последовательность байтов не пустая, считываем 
                        // и сравниваем запись по имени записи (либо имя пользователя, либо имя папки/файла).
                        if (!recordBytes.SequenceEqual(new byte[recordLength]))
                        {
                            IRecord record;
                            if (recordBytes.Length == UserRecord.Length)
                                record = new UserRecord(recordBytes);
                            else record = new DirectoryRecord(recordBytes);
                            if (record.EqualsByName(name))
                                return record;
                        }

                        offset += recordLength;
                    }
                }
                else
                { // Данные содержатся в области файлов.
                    int blockIndex = 0;
                    br.BaseStream.Seek(data.Clusters[blockIndex] * FileSystem.BytesPerCluster,
                        SeekOrigin.Begin);
                    while (offset < data.Size)
                    {
                        int availableBytes = FileSystem.BytesPerCluster - offset % FileSystem.BytesPerCluster;

                        List<byte> recordBytes = new List<byte>();
                        if (availableBytes >= recordLength) // Следующая запись помещается в текущий блок данных.
                            recordBytes.AddRange(br.ReadBytes(recordLength));
                        else
                        { // Следующая запись по частям содержится в двух блоках.
                            recordBytes.AddRange(br.ReadBytes(availableBytes));
                            br.BaseStream.Seek(data.Clusters[++blockIndex] * FileSystem.BytesPerCluster,
                                SeekOrigin.Begin);
                            if (recordLength - recordBytes.Count > 0)
                                recordBytes.AddRange(br.ReadBytes(recordLength - recordBytes.Count));
                        }

                        if (!recordBytes.ToArray().SequenceEqual(new byte[recordLength]))
                        {
                            IRecord record;
                            if (recordBytes.Count == UserRecord.Length)
                                record = new UserRecord(recordBytes.ToArray());
                            else record = new DirectoryRecord(recordBytes.ToArray());
                            if (record.EqualsByName(name))
                                return record;
                        }

                        offset += recordLength;
                    }
                }
            }

            return null;
        }

        public static List<ExplorerFile> GetFileList(Path path)
        {
            // Получаем весь список файлов для отображения в проводнике по указанному пути.
            List<DirectoryRecord> directoryRecords = GetDirectoryRecords(path);
            List<ExplorerFile> files = new List<ExplorerFile>();
            foreach (var record in directoryRecords)
            {
                MftHeader mftHeader = GetMftHeader(record.MftEntry);
                files.Add(new ExplorerFile(mftHeader, record.MftEntry));
            }

            return files;
        }

        /// <summary>
        ///  Получает файлы директории по указанному пути.
        /// </summary>
        /// <param name="path">Заданный путь.</param>
        /// <returns></returns>
        public static List<DirectoryRecord> GetDirectoryRecords(Path path)
        {
            List<DirectoryRecord> directoryRecords = new List<DirectoryRecord>();
            int directoryMftEntry = FindDirectoryMftEntry(path);
            if (directoryMftEntry == -1) return directoryRecords;
            return GetAllRecords<DirectoryRecord>(directoryMftEntry);
        }

        /// <summary>
        /// Получает все записи в Mft-записи по указанному номеру
        /// </summary>
        /// <typeparam name="T">Тип записи: пользователь или запись директории.</typeparam>
        /// <param name="mftEntry">Номер Mft-записи, информацию которой необходимо получить.</param>
        /// <returns></returns>
        public static List<T> GetAllRecords<T>(int mftEntry) where T : IRecord
        {
            int recordLength = typeof(T) == typeof(UserRecord) ? UserRecord.Length : DirectoryRecord.Length;
            List<T> records = new List<T>();
            Data data = ReadMftData(mftEntry);
            using (BinaryReader br = new BinaryReader(File.Open(Constants.SystemFile, FileMode.Open)))
            {
                int offset = 0;
                if (data.Clusters.Count == 0)
                {
                    br.BaseStream.Seek(mftEntry * Constants.MftRecordSize + MftHeader.Length, SeekOrigin.Begin);
                    while (offset < data.Size)
                    {
                        byte[] recordBytes = br.ReadBytes(recordLength);
                        if (!recordBytes.SequenceEqual(new byte[recordLength]))
                        {
                            IRecord record;
                            if (recordLength == UserRecord.Length)
                                record = new UserRecord(recordBytes.ToArray());
                            else record = new DirectoryRecord(recordBytes.ToArray());
                            records.Add((T)record);
                        }

                        offset += recordLength;
                    }
                }
                else
                {
                    int blockIndex = 0;
                    br.BaseStream.Seek(data.Clusters[blockIndex] * FileSystem.BytesPerCluster,
                        SeekOrigin.Begin);
                    while (offset < data.Size)
                    {
                        int availableBytes = FileSystem.BytesPerCluster - offset % FileSystem.BytesPerCluster;

                        List<byte> recordBytes = new List<byte>();
                        if (availableBytes >= recordLength)
                            recordBytes.AddRange(br.ReadBytes(recordLength));
                        else
                        {
                            recordBytes.AddRange(br.ReadBytes(availableBytes));
                            br.BaseStream.Seek(data.Clusters[++blockIndex] * FileSystem.BytesPerCluster,
                                SeekOrigin.Begin);
                            recordBytes.AddRange(br.ReadBytes(recordLength - recordBytes.Count));
                        }

                        if (!recordBytes.SequenceEqual(new byte[recordLength]))
                        {
                            IRecord record;
                            if (recordLength == UserRecord.Length)
                                record = new UserRecord(recordBytes.ToArray());
                            else record = new DirectoryRecord(recordBytes.ToArray());
                            records.Add((T)record);
                        }

                        offset += recordLength;
                    }
                }
            }

            return records;
        }

        public static int GetDirectorySize(int mftEntry)
        {
            int directorySize = 0;
            List<DirectoryRecord> records = GetAllRecords<DirectoryRecord>(mftEntry);
            foreach (var record in records)
            {
                if (record.HasAttribute(MftHeader.Attribute.Directory))
                    directorySize += GetDirectorySize(record.MftEntry);
                else directorySize += GetMftHeader(record.MftEntry).Size;
            }

            return directorySize;
        }

        /// <summary>
        /// Обновляет запись на диске.
        /// </summary>
        /// <typeparam name="T">Тип записи: пользователь или запись директории.</typeparam>
        /// <param name="targetMftEntry">Номер Mft-записи, которую необходимо обновить.</param>
        /// <param name="before">Текущая запись.</param>
        /// <param name="after">Обновлённая запсиь.</param>
        public static void UpdateRecord<T>(int targetMftEntry, T before, T after) where T : IRecord
        {
            if (before == null || after == null)
                throw new ArgumentException("Обновляемая или обновлённая записи не определены.");
            if (targetMftEntry < 0)
                throw new ArgumentException("Файл, в котором следует обновить запись, не найден!");
            if (before.GetType() != after.GetType())
                throw new ArgumentException("Обновляемая и обновлённая записи имеют разные типы.");

            int recordLength = before.GetBytes().Length;
            Data data = ReadMftData(targetMftEntry);
            using (BinaryReader br = new BinaryReader(File.Open(Constants.SystemFile, FileMode.Open)))
            {
                int offset = 0;
                if (data.Clusters.Count == 0)
                {
                    br.BaseStream.Seek(targetMftEntry * Constants.MftRecordSize + MftHeader.Length,
                        SeekOrigin.Begin);
                    while (offset < data.Size)
                    {
                        if (before.Equals(br.ReadBytes(recordLength)))
                        {
                            using (BinaryWriter bw = new BinaryWriter(br.BaseStream))
                            {
                                bw.BaseStream.Position -= recordLength;
                                bw.Write(after.GetBytes());
                            }
                            return;
                        }

                        offset += recordLength;
                    }
                }
                else
                {
                    int blockIndex = 0;
                    br.BaseStream.Seek(data.Clusters[blockIndex] * FileSystem.BytesPerCluster, SeekOrigin.Begin);
                    while (offset < data.Size)
                    {
                        int availableBytes = FileSystem.BytesPerCluster - offset % FileSystem.BytesPerCluster;
                        if (availableBytes >= recordLength)
                        {
                            if (before.Equals(br.ReadBytes(recordLength)))
                            {
                                using (BinaryWriter bw = new BinaryWriter(br.BaseStream))
                                {
                                    bw.BaseStream.Position -= recordLength;
                                    bw.Write(after.GetBytes());
                                }
                                return;
                            }
                        }
                        else
                        {
                            List<byte> recordBytes = new List<byte>(br.ReadBytes(availableBytes));
                            br.BaseStream.Seek(data.Clusters[++blockIndex] * FileSystem.BytesPerCluster,
                                SeekOrigin.Begin);
                            recordBytes.AddRange(br.ReadBytes(recordLength - availableBytes));

                            if (before.Equals(recordBytes.ToArray()))
                            {
                                using (BinaryWriter bw = new BinaryWriter(br.BaseStream))
                                {
                                    bw.BaseStream.Seek(data.Clusters[--blockIndex] * FileSystem.BytesPerCluster +
                                        offset % FileSystem.BytesPerCluster, SeekOrigin.Begin);
                                    bw.Write(after.GetBytes().GetRange(0, availableBytes));
                                    bw.BaseStream.Seek(data.Clusters[++blockIndex] * FileSystem.BytesPerCluster,
                                        SeekOrigin.Begin);
                                    bw.Write(after.GetBytes().GetRange(availableBytes, recordLength - availableBytes));
                                }
                                return;
                            }
                        }

                        offset += recordLength;
                    }
                }
            }
        }

        private static void WriteClusterNumbers(int mftEntry, int[] clusters)
        {
            using (BinaryWriter bw = new BinaryWriter(File.Open(Constants.SystemFile, FileMode.Open)))
            {
                bw.Seek(mftEntry * Constants.MftRecordSize + MftHeader.Length, SeekOrigin.Begin);
                foreach (var cluster in clusters)
                    bw.Write(cluster);
            }
        }

        public static void WriteFileData(int mftEntry, byte[] fileData)
        {
            if (fileData == null) return;
            Data data = ReadMftData(mftEntry);
            using (BinaryWriter bw = new BinaryWriter(File.Open(Constants.SystemFile, FileMode.Open)))
            {
                bw.Seek(mftEntry * Constants.MftRecordSize + MftHeader.Length, SeekOrigin.Begin);

                if (data.Clusters.Count == 0)
                    bw.Write(fileData);
                else
                {
                    int needToWriteBytes = fileData.Length;
                    foreach (var block in data.Clusters)
                    {
                        int byteCount = needToWriteBytes < FileSystem.BytesPerCluster
                            ? needToWriteBytes
                            : FileSystem.BytesPerCluster;
                        bw.Seek(block * FileSystem.BytesPerCluster, SeekOrigin.Begin);
                        bw.Write(fileData.GetRange(fileData.Length - needToWriteBytes, byteCount));
                        needToWriteBytes -= byteCount;
                    }
                }
            }
        }

        public static byte[] ReadFileData(int mftEntry)
        {
            List<byte> dataBytes = new List<byte>();
            Data data = ReadMftData(mftEntry);
            using (BinaryReader br = new BinaryReader(File.Open(Constants.SystemFile, FileMode.Open)))
            {
                br.BaseStream.Seek(mftEntry * Constants.MftRecordSize + MftHeader.Length,
                    SeekOrigin.Begin);

                if (data.Clusters.Count == 0)
                    dataBytes.AddRange(br.ReadBytes(data.Size));
                else
                {
                    int needToReadBytes = data.Size;
                    foreach (var block in data.Clusters)
                    {
                        int byteCount = needToReadBytes > FileSystem.BytesPerCluster
                            ? FileSystem.BytesPerCluster
                            : needToReadBytes;
                        br.BaseStream.Seek(block * FileSystem.BytesPerCluster, SeekOrigin.Begin);
                        dataBytes.AddRange(br.ReadBytes(byteCount));
                        needToReadBytes -= byteCount;
                    }
                }

                return dataBytes.ToArray();
            }
        }

        public static Data ReadMftData(int mftEntry)
        {
            using (BinaryReader br = new BinaryReader(File.Open(Constants.SystemFile, FileMode.Open)))
            {
                br.BaseStream.Seek(mftEntry * Constants.MftRecordSize + 33, SeekOrigin.Begin);
                int dataSize = br.ReadInt32();
                br.BaseStream.Seek(13, SeekOrigin.Current);
                int clustersCount = CalculateClustersNumber(dataSize);
                int[] blocks = new int[clustersCount]; // Список номеров блоков, в которых содержатся данные.
                for (int i = 0; i < clustersCount; i++)
                    blocks[i] = br.ReadInt32();

                return new Data(dataSize, blocks);
            }
        }

        /// <summary>
        /// Получает незанятый номер для новой Mft-записи.
        /// </summary>
        /// <returns></returns>
        static int GetNewMftEntry()
        {
            using (BinaryReader br = new BinaryReader(File.Open(Constants.SystemFile, FileMode.Open)))
            {
                br.BaseStream.Seek(Constants.MftEntry * Constants.MftRecordSize, SeekOrigin.Begin);
                MftHeader mftItselfHeader = new MftHeader(br.ReadBytes(MftHeader.Length));

                // Просматриваем все записи MFT, начиная с первого доступного номера, т.е. пропуская метафайлы.
                int mftEntry = Constants.ServiceFileCount;
                br.BaseStream.Seek(mftEntry * Constants.MftRecordSize, SeekOrigin.Begin);
                while (mftEntry * Constants.MftRecordSize < mftItselfHeader.Size)
                {
                    if (br.ReadByte() == (byte)MftHeader.Signature.NotUsed)
                        return mftEntry;
                    br.BaseStream.Seek(Constants.MftRecordSize - 1, SeekOrigin.Current);
                    mftEntry++;
                }

                // Проверяем, будет ли главный файл MFT помещаться в MFT зону,
                // если увечилить его размер ещё на одну запись.
                if ((mftEntry + 1) * Constants.MftRecordSize <= FileSystem.MftAreaSize)
                    return mftEntry;
                return -1;
            }
        }

        /// <summary>
        /// Получает атрибуты размещения новой записи в памяти.
        /// </summary>
        /// <exception cref="FsException">Недостаточно свободного пространства в Mft-зоне.</exception>
        /// <exception cref="FsException">Превышен максимально допустимый размер директории.</exception>
        /// <exception cref="FsException">Недостаточно свободного пространства в области файлов.</exception>
        /// <param name="targetMftEntry">Номер Mft-записи, в которую следует помесить данную запись.</param>
        /// <param name="recordLength">Длина в байтах данной записи.</param>
        /// <returns></returns>
        public static MemoryAllocation TryToAllocateMemory(int targetMftEntry, int recordLength)
        {
            int mftEntry = GetNewMftEntry();
            if (mftEntry == -1)
                throw new FsException(FsException.Code.NoSystemSpace, "");

            MemoryAllocation.Attribute allocationAttributes = 0;

            // Устанавливаем флаг увеличения Mft, если новая Mft-запись не помещается в Mft-файл.
            if (GetMftHeader(Constants.MftEntry).Size == mftEntry * Constants.MftRecordSize)
                allocationAttributes |= MemoryAllocation.Attribute.NeedToIncreaseMftSize;

            Data data = ReadMftData(targetMftEntry);
            using (BinaryReader br = new BinaryReader(File.Open(Constants.SystemFile, FileMode.Open)))
            {
                int offset = 0;
                if (data.Clusters.Count == 0)
                {
                    br.BaseStream.Seek(targetMftEntry * Constants.MftRecordSize + MftHeader.Length,
                        SeekOrigin.Begin);
                    while (offset < data.Size)
                    {
                        if (br.ReadBytes(recordLength).SequenceEqual(new byte[recordLength]))
                            return new MemoryAllocation(mftEntry, allocationAttributes);
                        offset += recordLength;
                    }

                    // Проверяем, поместится ли новая запись в атрибут Data Mft-записи директории.
                    if (offset + recordLength > Constants.MftRecordSize - MftHeader.Length)
                        allocationAttributes |= MemoryAllocation.Attribute.NeedNewCluster;
                }

                else
                {
                    int blockIndex = 0;
                    br.BaseStream.Seek(data.Clusters[blockIndex] * FileSystem.BytesPerCluster, SeekOrigin.Begin);
                    while (offset < data.Size)
                    {
                        int availableBytes = FileSystem.BytesPerCluster - offset % FileSystem.BytesPerCluster;
                        if (availableBytes >= recordLength)
                        {
                            if (br.ReadBytes(recordLength).SequenceEqual(new byte[recordLength]))
                                return new MemoryAllocation(mftEntry, allocationAttributes);
                        }
                        else
                        {
                            List<byte> recordBytes = new List<byte>(br.ReadBytes(availableBytes));
                            br.BaseStream.Seek(data.Clusters[blockIndex + 1] * FileSystem.BytesPerCluster,
                                SeekOrigin.Begin);
                            recordBytes.AddRange(br.ReadBytes(recordLength - availableBytes));

                            if (recordBytes.SequenceEqual(new byte[recordLength]))
                                return new MemoryAllocation(mftEntry, allocationAttributes);
                        }

                        offset += recordLength;
                    }

                    // Проверяем, поместится ли новая запись в уже выделенные кластеры.
                    if (offset + recordLength > data.Clusters.Count * FileSystem.BytesPerCluster)
                        allocationAttributes |= MemoryAllocation.Attribute.NeedNewCluster;
                }

                // Так как прошли по всей директории и не нашли свободного места,
                // то устанавливаем флаг необходимости увеличения размера директории.
                allocationAttributes |= MemoryAllocation.Attribute.NeedToIncreaseDirectorySize;

                // Проверяем, есть ли свободные кластеры в области файлов,
                // если был установлен флаг выделения нового кластера.
                if ((allocationAttributes & MemoryAllocation.Attribute.NeedNewCluster) != 0)
                {
                    if (FileSystem.FreeClusters == 0) throw new FsException(FsException.Code.NoFreeSpace, "");
                    // Проверяем, поместится ли в атрибут Data ссылка на ещё один выделенный кластер.
                    if (data.Clusters.Count + 1 > (Constants.MftRecordSize - MftHeader.Length) / 4)
                        throw new FsException(FsException.Code.NoSystemSpace, "");
                }

                return new MemoryAllocation(mftEntry, allocationAttributes);
            }
        }

        /// <summary>
        /// Увеличивает размеры необходимых файлов с учётом атрибутов распределения нового файла.
        /// </summary>
        /// <param name="allocation">Атрибуты распределения файла в памяти.</param>
        /// <param name="targetMftEntry">Номер Mft-записи, в которую следует поместить данную запись.</param>
        /// <param name="recordLength">Номер новой записи.</param>
        public static void AllocateRecord(MemoryAllocation allocation, int targetMftEntry, int recordLength)
        {
            Data data = ReadMftData(targetMftEntry);
            byte[] dataBytes = null;

            // При необходимости выделяем новый кластер под подительскую директорию.
            if ((allocation.Attributes & MemoryAllocation.Attribute.NeedNewCluster) != 0)
            {
                int newBlock = GetFreeClusters(1)[0];
                SetClustersState(new List<int> { newBlock }, ClusterState.Busy);
                if (data.Clusters.Count == 0)
                    dataBytes = ReadFileData(targetMftEntry);

                data.Clusters.Add(newBlock);
                WriteClusterNumbers(targetMftEntry, data.Clusters.ToArray());
            }

            MftHeader directoryMftHeader = GetMftHeader(targetMftEntry);
            directoryMftHeader.ModificationDate = new MyDateTime(DateTime.Now);
            // При необходимости увеличиваем размер родительской директории.
            if ((allocation.Attributes & MemoryAllocation.Attribute.NeedToIncreaseDirectorySize) != 0)
                directoryMftHeader.Size += recordLength;
            UpdateMftEntry(directoryMftHeader, targetMftEntry);

            // Если было необходимо выделить новый кластер для директории и он был первым
            // (то есть до этого данные полностью помещались в Mft-запись),
            // то перезаписываем их в область файлов.
            if (dataBytes != null) WriteFileData(targetMftEntry, dataBytes);

            // При необходимости увеличиваем размер зоны Mft.
            if ((allocation.Attributes & MemoryAllocation.Attribute.NeedToIncreaseMftSize) != 0)
            {
                MftHeader mftHeader = GetMftHeader(Constants.MftEntry);
                mftHeader.Size += Constants.MftRecordSize;
                UpdateMftEntry(mftHeader, Constants.MftEntry);
            }
        }

        public static int Create(Path path, string name, string extension,
            MftHeader.Attribute attributes = MftHeader.Attribute.None, int userId = -1)
        {
            MftHeader newMftHeader = new MftHeader(name, extension, attributes, userId: userId);
            // Получаем родительскую директорию.
            int directoryMftEntry = FindDirectoryMftEntry(path);
            if (directoryMftEntry == -1)
                throw new FsException(FsException.Code.NotFound, path.GetAbsolutePath(newMftHeader.FileName));
            MftHeader parentDirectory = GetMftHeader(directoryMftEntry);

            // Проверяем права доступа для директории.
            if (!parentDirectory.HasPermissions(FileSystem.CurrentUser, Permission.Rights.Write))
                throw new FsException(FsException.Code.NoWritePermission,
                    path.CurrentPath);

            // Получаем параметры распределения памяти.
            MemoryAllocation allocation = TryToAllocateMemory(directoryMftEntry, DirectoryRecord.Length);

            // Проверяем, не существует ли уже файл с таким именем.
            DirectoryRecord record =
                (DirectoryRecord)GetRecord(newMftHeader.GetFullName(), directoryMftEntry, DirectoryRecord.Length);
            if (record != null)
                throw new FsException(record.HasAttribute(MftHeader.Attribute.Directory)
                        ? FsException.Code.FolderExists
                        : FsException.Code.FileExists, record.GetFullName());

            // Добавляем новую запись в Mft.
            UpdateMftEntry(newMftHeader, allocation.MftEntry);
            // Увеличиваем при необходимости размеры файлов.
            AllocateRecord(allocation, directoryMftEntry, DirectoryRecord.Length);
            // Добавляем запись в родительскую директорию на пустое место.
            UpdateRecord(directoryMftEntry, DirectoryRecord.Empty,
                new DirectoryRecord(newMftHeader, allocation.MftEntry));

            return allocation.MftEntry;
        }

        public static void Copy(Path path, DirectoryRecord record)
        {
            // Получаем файл для копирования.
            DirectoryRecord bufferRecord =
                (DirectoryRecord)GetRecord(record.GetFullName(), FindDirectoryMftEntry(path), DirectoryRecord.Length);
            MftHeader mftHeader = GetMftHeader(bufferRecord.MftEntry);
            // Пользователь должен обладать полным доступом к файлу для его копирования.
            if (!mftHeader.HasPermissions(FileSystem.CurrentUser, Permission.Rights.FullControl))
                throw new FsException(FsException.Code.NoFullControlPermission, path.GetAbsolutePath(mftHeader.GetFullName()));

            FileSystem.Buffer = new SystemBuffer { Path = new Path(path), Record = new DirectoryRecord(bufferRecord) };
        }

        public static List<DirectoryRecord> CopyRecursively(Path sourcePath, Path destinationPath)
        {
            List<DirectoryRecord> copies = new List<DirectoryRecord>();
            List<DirectoryRecord> records = GetDirectoryRecords(sourcePath);
            foreach (var record in records)
            {
                MftHeader source = GetMftHeader(record.MftEntry);
                try
                {
                    // Проверяем, существует ли файл в исходном расположении.
                    if (source == null)
                        throw new FsException(FsException.Code.NotFound,
                            sourcePath.GetAbsolutePath(record.GetFullName()));

                    // Проверяем доступ к файлу. Пользователь должен обладать полным доступом к файлу для его копирования.
                    if (!source.HasPermissions(FileSystem.CurrentUser, Permission.Rights.FullControl))
                        throw new FsException(FsException.Code.NoFullControlPermission,
                            sourcePath.GetAbsolutePath(source.GetFullName()));

                    // Пробуем создать копию файла в заданном располжении.
                    var mftEntry = Create(destinationPath, source.FileName, source.Extension,
                        (MftHeader.Attribute) source.Attributes);
                    byte[] data;
                    // В зависимости от типа записи (директория или файл), получаем её данные.
                    if (record.HasAttribute(MftHeader.Attribute.Directory))
                    { // Для директории копируем перечень файлов.
                        Path from = new Path(sourcePath);
                        from.Add(record.FileName);
                        Path to = new Path(destinationPath);
                        to.Add(source.FileName);

                        // Получаем список дочерних файлов.
                        List<DirectoryRecord> childCopyFileList = CopyRecursively(from, to);

                        // Создаём содержимое директории-копии.
                        List<byte> directoryBytes = new List<byte>();
                        foreach (var childFileCopy in childCopyFileList)
                            directoryBytes.AddRange(childFileCopy.GetBytes());
                        data = directoryBytes.ToArray();
                    }
                    else data = ReadFileData(record.MftEntry); // Копируем данные файла.

                    MftHeader copyMftHeader = GetMftHeader(mftEntry);
                    // Записываем данные записи.
                    Save(new DirectoryRecord(copyMftHeader, mftEntry), data);
                    // Обновляем созданную копию записи, изменяя её размер.
                    copyMftHeader.Size = data.Length;
                    UpdateMftEntry(copyMftHeader, mftEntry);

                    copies.Add(new DirectoryRecord(copyMftHeader, mftEntry));
                }
                catch (FsException fsException)
                {
                    // При возникновении исключения, выводим сообщение об ошибке,
                    // пропускаем файл и идём дальше по перечню файлов директории.
                    fsException.ShowError(FsException.Command.Copy,
                        record.HasAttribute(MftHeader.Attribute.Directory)
                            ? FsException.Element.Folder
                            : FsException.Element.File);
                }
            }

            return copies;
        }

        public static void Paste(Path path)
        {
            if (FileSystem.Buffer.Path.GetAbsolutePath(FileSystem.Buffer.Record.GetFullName()) == path.CurrentPath)
                throw new FsException(FsException.Code.EndlessCopy,
                    FileSystem.Buffer.Record.GetFullName());

            MftHeader source = GetMftHeader(FileSystem.Buffer.Record.MftEntry);
            // Проверяем, существует ли файл в исходном расположении.
            if (source == null)
                throw new FsException(FsException.Code.NotFound,
                    path.GetAbsolutePath(FileSystem.Buffer.Record.GetFullName()));

            int directoryMftEntry = FindDirectoryMftEntry(path);
            // Проверяем доступ к файлу. Пользователь должен обладать полным доступом к файлу для его копирования.
            if (!source.HasPermissions(FileSystem.CurrentUser, Permission.Rights.FullControl))
                throw new FsException(FsException.Code.NoFullControlPermission,
                    path.GetAbsolutePath(source.GetFullName()));

            // Проверяем, существует ли в заданном расположении файл или директория с указанным именем.
            // Пока находим такие файлы и длина имени не превосходит максимальную в системе, изменяем имя.
            string sourceFullName = source.GetFullName();
            string name = source.FileName;
            int attempt = 1;
            while (GetRecord(source.GetFullName(), directoryMftEntry, DirectoryRecord.Length) != null)
            {
                source.FileName = name + " (" + attempt++ + ")";
                if (source.FileName.Length > 26)
                    throw new FsException(FsException.Code.NameTooLong, source.FileName);
            }

            // Пробуем создать файл по указанному пути.
            var mftEntry = Create(path, source.FileName, source.Extension,
                (MftHeader.Attribute)source.Attributes);

            byte[] data;
            if (source.HasAttribute(MftHeader.Attribute.Directory))
            {
                // Если данный файл - директория, то копируем всё её содержимое.
                Path from = new Path(FileSystem.Buffer.Path);
                from.Add(FileSystem.Buffer.Record.FileName);
                Path to = new Path(path);
                to.Add(source.FileName);

                // Получаем список дочерних файлов.
                List<DirectoryRecord> childCopyFileList = CopyRecursively(from, to);

                // Записываем перечень файлов в директрию.
                List<byte> directoryBytes = new List<byte>();
                foreach (var childFileCopy in childCopyFileList)
                    directoryBytes.AddRange(childFileCopy.GetBytes());

                data = directoryBytes.ToArray();
                if (data.Length != GetAllRecords<DirectoryRecord>(FileSystem.Buffer.Record.MftEntry).Count *
                    DirectoryRecord.Length)
                    throw new FsException(FsException.Code.IncompleteCopying,
                        FileSystem.Buffer.Path.GetAbsolutePath(sourceFullName));
            }
            else data = ReadFileData(FileSystem.Buffer.Record.MftEntry);

            MftHeader copyMftHeader = GetMftHeader(mftEntry);
            Save(new DirectoryRecord(source, mftEntry), data);
            copyMftHeader.Size = data.Length;

            UpdateMftEntry(copyMftHeader, mftEntry);
        }

        public static void Rename(Path path, DirectoryRecord record, string newFullName)
        {
            // Проверяем права доступа для директории.
            int directoryMftEntry = FindDirectoryMftEntry(path);
            MftHeader parentDirectory = GetMftHeader(directoryMftEntry);
            if (!parentDirectory.HasPermissions(FileSystem.CurrentUser, Permission.Rights.Write))
                throw new FsException(FsException.Code.NoWritePermission, path.CurrentPath);

            // Проверяем права доступа для заданной записи.
            MftHeader file = GetMftHeader(record.MftEntry);
            if (!file.HasPermissions(FileSystem.CurrentUser, Permission.Rights.Write))
                throw new FsException(FsException.Code.NoWritePermission, path.GetAbsolutePath(file.GetFullName()));


            DirectoryRecord newRecord = (DirectoryRecord)GetRecord(newFullName, directoryMftEntry, DirectoryRecord.Length);
            if (newRecord != null)
            {
                throw new FsException(newRecord.HasAttribute(MftHeader.Attribute.Directory)
                    ? FsException.Code.FolderExists
                    : FsException.Code.FileExists, newRecord.GetFullName());
            }

            newFullName.ParseFullName(record.HasAttribute(MftHeader.Attribute.Directory), out var name, out var extension);
            file.FileName = name;
            file.Extension = extension;
            UpdateMftEntry(file, record.MftEntry);

            UpdateRecord(directoryMftEntry, record, new DirectoryRecord(file, record.MftEntry));
        }

        public static void Save(DirectoryRecord record, byte[] fileData)
        {
            Data data = ReadMftData(record.MftEntry);
            int requiredClusterCount = CalculateClustersNumber(fileData.Length) - data.Clusters.Count;
            if (requiredClusterCount > 0)
            {
                List<int> clusterNumbers = GetFreeClusters(requiredClusterCount);

                if (clusterNumbers.Count < requiredClusterCount)
                    throw new FsException(FsException.Code.NoFreeSpace, "");

                if (data.Clusters.Count + requiredClusterCount > (Constants.MftRecordSize - MftHeader.Length) / 4)
                    throw new FsException(FsException.Code.NoSystemSpace, "");

                data.Clusters.AddRange(clusterNumbers);
                SetClustersState(clusterNumbers, ClusterState.Busy);
            }
            else if (requiredClusterCount < 0)
            {
                SetClustersState(data.Clusters.GetRange(data.Clusters.Count + requiredClusterCount,
                    Math.Abs(requiredClusterCount)), ClusterState.Free);
                data.Clusters.RemoveRange(data.Clusters.Count + requiredClusterCount,
                    Math.Abs(requiredClusterCount));
            }

            MftHeader file = GetMftHeader(record.MftEntry);
            if (file.HasAttribute(MftHeader.Attribute.ReadOnly))
                throw new FsException(FsException.Code.ReadOnly, file.GetFullName());
            if (!file.HasPermissions(FileSystem.CurrentUser, Permission.Rights.Write))
                throw new FsException(FsException.Code.NoWritePermission, file.GetFullName());

            file.ModificationDate = new MyDateTime(DateTime.Now);
            file.Size = fileData.Length;
            UpdateMftEntry(file, record.MftEntry);
            WriteClusterNumbers(record.MftEntry, data.Clusters.ToArray());
            WriteFileData(record.MftEntry, fileData);
        }

        public static void Delete(Path path, DirectoryRecord record)
        {
            // Проверяем, есть ли право на запись родительской директории.
            int directoryMftEntry = FindDirectoryMftEntry(path);
            MftHeader parentDirectory = GetMftHeader(directoryMftEntry);
            if (directoryMftEntry != Constants.RootMftEntry &&
                !parentDirectory.HasPermissions(FileSystem.CurrentUser, Permission.Rights.Write))
                throw new FsException(FsException.Code.NoWritePermission, path.CurrentPath);

            // Проверяем, есть ли право на изменение файла.
            MftHeader mftHeader = GetMftHeader(record.MftEntry);
            if (!mftHeader.HasPermissions(FileSystem.CurrentUser, Permission.Rights.Modify))
                throw new FsException(FsException.Code.NoModifyPermission,
                    path.GetAbsolutePath(mftHeader.GetFullName()));

            if (record.HasAttribute(MftHeader.Attribute.Directory))
            { // Если удаляемая запись является директорией, удаляем её содержимое.
                Path from = new Path(path);
                from.Add(record.FileName);
                int deletedFileCount = DeleteRecursively(from);
                // Если не все файлы директории были удалены, прекращаем операцию удаления,
                // поднимаясь на верхний уровень.
                if (deletedFileCount < GetAllRecords<DirectoryRecord>(record.MftEntry).Count)
                    throw new FsException(FsException.Code.IncompleteDeleting, record.FileName);

                // Если была удалена домашняя директория пользователя,
                // то изменяем номер Mft-записи в его заявке.
                List<UserRecord> users = GetAllRecords<UserRecord>(Constants.UserListMftEntry);
                foreach (var user in users)
                {
                    if (user.HomeDirectoryMftEntry == record.MftEntry)
                    {
                        UserRecord updatedUserRecord =
                            new UserRecord(user) {HomeDirectoryMftEntry = Constants.RootMftEntry};
                        UpdateRecord(Constants.UserListMftEntry, user, updatedUserRecord);
                    }
                }
            }

            // Очищаем кластеры.
            Data data = ReadMftData(record.MftEntry);
            SetClustersState(data.Clusters, ClusterState.Free);

            // Удаляем запись в родительской директории.
            UpdateRecord(directoryMftEntry, GetRecord(record.GetFullName(), directoryMftEntry,
                    DirectoryRecord.Length), DirectoryRecord.Empty);

            // Ставим пометку о неиспользуемой записи MFT.
            UpdateMftEntry(null, record.MftEntry);

            // Изменяем время последней модификации родительской директории
            MftHeader directoryHeader = GetMftHeader(directoryMftEntry);
            directoryHeader.ModificationDate = new MyDateTime(DateTime.Now);
            UpdateMftEntry(directoryHeader, directoryMftEntry);
        }

        static int DeleteRecursively(Path directoryPath)
        {
            int deletedRecordCount = 0;
            bool canDelete = true;
            List<DirectoryRecord> records = GetDirectoryRecords(directoryPath);
            foreach (var record in records)
            {
                MftHeader mftHeader = GetMftHeader(record.MftEntry);
                if (!mftHeader.HasPermissions(FileSystem.CurrentUser, Permission.Rights.Modify))
                { 
                    // Если не хватает прав на изменения данного элемента,
                    // выводим сообщений об ошибке и отменяем удаление этой записи.
                    new FsException(FsException.Code.NoModifyPermission,
                        directoryPath.GetAbsolutePath(mftHeader.GetFullName())).ShowError(FsException.Command.Delete,
                        record.HasAttribute(MftHeader.Attribute.Directory)
                            ? FsException.Element.Folder
                            : FsException.Element.File);

                    canDelete = false;
                }

                if (record.HasAttribute(MftHeader.Attribute.Directory))
                {
                    // Если удаляемая запись является директорией, удаляем её содержимое.
                    Path from = new Path(directoryPath);
                    from.Add(record.FileName);
                    int childRecordCount = DeleteRecursively(from);
                    if (childRecordCount * DirectoryRecord.Length < mftHeader.Size)
                        // Если не все файлы папки были удалены, отменяем удаление только этой записи.
                        canDelete = false;
                }

                if (canDelete)
                {
                    // Очищаем кластеры.
                    Data data = ReadMftData(record.MftEntry);
                    SetClustersState(data.Clusters, ClusterState.Free);

                    // Удаляем запись в родительской директории.
                    int directoryMftEntry = FindDirectoryMftEntry(new Path(directoryPath));
                    UpdateRecord(directoryMftEntry,
                        GetRecord(record.GetFullName(), directoryMftEntry, DirectoryRecord.Length),
                        DirectoryRecord.Empty);

                    // Ставим пометку о неиспользуемой записи MFT.
                    UpdateMftEntry(null, record.MftEntry);

                    // Изменяем время последней модификации родительской директории
                    MftHeader directoryHeader = GetMftHeader(directoryMftEntry);
                    directoryHeader.ModificationDate = new MyDateTime(DateTime.Now);
                    UpdateMftEntry(directoryHeader, directoryMftEntry);

                    deletedRecordCount++;
                }
            }

            return deletedRecordCount;
        }

        public static void SignIn(string name, string password)
        {
            UserRecord user = (UserRecord)GetRecord(name, Constants.UserListMftEntry, UserRecord.Length);
            if (user == null || !user.PasswordHash.SequenceEqual(password.ComputeHash())) throw new FsException(FsException.Code.UserNull, "");
            FileSystem.CurrentUser = user;
        }

        public static void SignUp(string name, string password, bool isAdministrator)
        {
            if (GetRecord(name, Constants.UserListMftEntry, UserRecord.Length) != null)
                throw new FsException(FsException.Code.UserExists, name);
            int id = GetAvailableId();
            if (id > Constants.MaxUserId) throw new FsException(FsException.Code.MaxUserCount, "");

            MemoryAllocation fileAllocation;
            try
            {
                fileAllocation = TryToAllocateMemory(Constants.UserListMftEntry, UserRecord.Length);
            }
            catch (FsException fsException)
            {
                fsException.ShowError(FsException.Command.Create, FsException.Element.User);
                return;
            }

            int homeDirectoryMftEntry;
            try
            {
                homeDirectoryMftEntry = Create(new Path(), name, "", MftHeader.Attribute.Directory, id);
            }
            catch (FsException fsException)
            {
                fsException.ShowError(FsException.Command.Create, FsException.Element.HomeDirectory);
                return;
            }

            AllocateRecord(fileAllocation, Constants.UserListMftEntry, UserRecord.Length);
            UpdateRecord(Constants.UserListMftEntry, UserRecord.Empty, new UserRecord(name, password, id, isAdministrator, homeDirectoryMftEntry));
        }

        public static UserRecord GetUserById(int uid)
        {
            Data data = ReadMftData(Constants.UserListMftEntry);
            using (BinaryReader br = new BinaryReader(File.Open(Constants.SystemFile, FileMode.Open)))
            {
                int offset = 0;
                if (data.Clusters.Count == 0)
                {
                    br.BaseStream.Seek(Constants.UserListMftEntry * Constants.MftRecordSize + MftHeader.Length, SeekOrigin.Begin);
                    while (offset < data.Size)
                    {
                        UserRecord user = new UserRecord(br.ReadBytes(UserRecord.Length));
                        if (!user.Equals(UserRecord.Empty) && user.Id == uid) return user;

                        offset += UserRecord.Length;
                    }
                }
                else
                {
                    int blockIndex = 0;
                    br.BaseStream.Seek(data.Clusters[blockIndex] * FileSystem.BytesPerCluster,
                        SeekOrigin.Begin);
                    while (offset < data.Size)
                    {
                        int availableBytes = FileSystem.BytesPerCluster - offset % FileSystem.BytesPerCluster;

                        List<byte> userBytes = new List<byte>();
                        if (availableBytes >= UserRecord.Length)
                            userBytes.AddRange(br.ReadBytes(UserRecord.Length));
                        else
                        {
                            userBytes.AddRange(br.ReadBytes(availableBytes));
                            br.BaseStream.Seek(data.Clusters[++blockIndex] * FileSystem.BytesPerCluster,
                                SeekOrigin.Begin);
                            userBytes.AddRange(br.ReadBytes(UserRecord.Length - userBytes.Count));
                        }

                        UserRecord user = new UserRecord(userBytes.ToArray());
                        if (!user.Equals(UserRecord.Empty) && user.Id == uid) return user;

                        offset += UserRecord.Length;
                    }
                }
            }

            return null;
        }

        static int GetAvailableId()
        {
            List<UserRecord> users = GetAllRecords<UserRecord>(Constants.UserListMftEntry);
            List<byte> idList = users.Select(user => user.Id).ToList();
            for (byte i = 0; i < Constants.MaxUserId; i++)
                if (!idList.Contains(i))
                    return i;
            return Constants.MaxUserId + 1;
        }

        public static void ChangeOwnerToAdministrator(byte id)
        {
            List<MftHeader> mftHeaders = GetAllMftHeaders();
            for (int i = 0; i < mftHeaders.Count; i++)
            {
                if (mftHeaders[i].UserId == id)
                {
                    mftHeaders[i].ChangeUser(0);
                    UpdateMftEntry(mftHeaders[i], i);
                }
            }
        }
    }
}