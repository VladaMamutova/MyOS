using System;
using System.IO;
using System.Linq;
using System.Text;
using MyOS.FileSystem.SpecialDataTypes;
using Path = MyOS.FileSystem.SpecialDataTypes.Path;

namespace MyOS.FileSystem
{
    static class FileSystem
    {
        /// <summary>
        /// Системные константы.
        /// </summary>
        public struct Constants
        {
            public const string SystemFile = "VMFS.bin";

            public const ushort MftRecordSize = 1024; // Размер записи в MFT.
            public const byte ServiceFileCount = 6; // Всего служебных метафайлов.
            public const byte MaxUserId = Byte.MaxValue; // Максимальное число пользователей в системе.

            // Фиксированные номера метафайлов в MFT.
            public const byte MftEntry = 0;
            public const byte MftMirrEntry = 1;
            public const byte VolumeMftEntry = 2;
            public const byte RootMftEntry = 3;
            public const byte BitmapMftEntry = 4;
            public const byte UserListMftEntry = 5;
        }

        public static readonly char VolumeName; // Метка тома.
        public static readonly char Root; // Имя корневого каталога.
        public static readonly string FileSystemVersion; // Версия файловой системы
        public static readonly byte State; // Состояние тома.
        public static readonly int VolumeSize; // Размер тома.
        public static readonly int MftAreaSize; // Размер MFT-пространства (10% от всего размера).
        public static readonly int BytesPerCluster; // Размер кластера в байтах.

        public static int FreeClusters;
        public static int ServiceClusters;
        public static UserRecord CurrentUser; // Текущий пользователь системы.
        public static SystemBuffer Buffer; // Системный буфер.
        public static Path CurrentPath;
        
        static FileSystem()
        {
            using (BinaryReader br = new BinaryReader(File.Open(Constants.SystemFile, FileMode.Open)))
            {
                VolumeSize = (int)br.BaseStream.Length;
                // Вычисляем размер Mft-пространства, делая его кратным размеру Mft-записи.
                MftAreaSize = VolumeSize / 10 - VolumeSize / 10 % Constants.MftRecordSize;
                br.BaseStream.Seek(Constants.VolumeMftEntry * Constants.MftRecordSize + MftHeader.Length, SeekOrigin.Begin);
                VolumeName = br.ReadChar();
                FileSystemVersion = Encoding.UTF8.GetString(br.ReadBytes(9));
                State = br.ReadByte();
                BytesPerCluster = BitConverter.ToInt32(br.ReadBytes(4), 0);
                br.BaseStream.Seek(Constants.RootMftEntry * Constants.MftRecordSize + 1, SeekOrigin.Begin);
                Root = br.ReadChar();
            }
            InitializeFreeClusters();
        }

        public static void InitializeFreeClusters()
        {
            Data bitmap = SystemCalls.ReadMftData(Constants.BitmapMftEntry);
            using (BinaryReader br = new BinaryReader(File.Open(Constants.SystemFile, FileMode.Open)))
            {
                int byteNumber = 0; // Номер прочитанного байта в битовой карте.
                foreach (var cluster in bitmap.Clusters)
                {
                    br.BaseStream.Seek(cluster * BytesPerCluster, SeekOrigin.Begin);
                    for (int i = 0; i < BytesPerCluster; i++)
                    {
                        if (byteNumber >= bitmap.Size) return;

                        byte fourClustersInfo = br.ReadByte();
                        // Кластер представляется 2 битами, значит в байте - информация о 4 кластерах.
                        for (int j = 0; j < 4; j++)
                        {
                            // Обнуляем первые шесть битов, при этом получая 2 младших бита,
                            // представляющих информацию об одном кластере.
                            if ((fourClustersInfo & 0b00000011) == (byte)ClusterState.Free)
                                FreeClusters++; // Увеличиваем счётчик свободных класетров
                            else if ((fourClustersInfo & 0b00000011) == (byte)ClusterState.Service)
                                ServiceClusters++;

                            // Сдвигаемся на 2 бита вправо для получения информации о следующем кластере.
                            fourClustersInfo = (byte)(fourClustersInfo >> 2);
                        }
                        byteNumber++;
                    }
                }
            }
        }

        public static int GetBusySpace()
        {
            return SystemCalls.GetAllMftHeaders().Count(mftHeader => mftHeader.Sign == MftHeader.Signature.InUse) *
                   Constants.MftRecordSize +
                   SystemCalls.GetDirectorySize(Constants.RootMftEntry);
        }
        public static int GetClusterCount() => VolumeSize / BytesPerCluster;
        public static int GetBusyClusterCount() => GetClusterCount() - ServiceClusters - FreeClusters;
    }
}
