using System.IO;
using System.Text;

namespace MyOS
{
    static class SystemData
    {
        public static readonly char VolumeName;
        public static readonly string FileSystemVersion;
        public static readonly byte State;
        public const int VolumeSize = 419430400; // Размер раздела с операционной системой.
        public const int MftAreaSize = 41943040; // Размер MFT-пространства (10% от всего размера).
        public const int RootSize = 409600; // Размер корневого каталога (100 кластеров * 4096 байтов).

        public const int
            BitmapSize = 25600; // Размер битовой карты (102 400 кластеров * 2 бита / 8 битов в байте = 25600 байтов).

        public const ushort BytesPerCluster = 4096; // Размер кластера в байтах.
        //public const int ClusterCount = 102400;
        //public const int FreeClusters = 92160;

        //public const byte AdminUid = 1; // Идентификатор администратора.
        public const int MaxUserCount = 256;
        public const byte UserRecSize = 193; // Размер заявки пользователя в списке пользователей.
        public const byte VolumeNameSize = 1;
        public const byte FsVersionSize = 9;
        public const byte StateSize = 1;
        public static SystemBuffer Buffer;

        static SystemData()
        {
            using (BinaryReader br = new BinaryReader(File.Open(SystemConstants.SystemFile, FileMode.Open)))
            {
                br.BaseStream.Seek(SystemConstants.VolumeRecNumber * SystemConstants.MftRecordSize + SystemConstants.MftHeaderLength, SeekOrigin.Begin);
                if (br.ReadByte() == 0) // Данные записи помещаютс в поле данных одной Mft-записи.
                {
                    VolumeName = br.ReadChar();
                    FileSystemVersion = Encoding.UTF8.GetString(br.ReadBytes(FsVersionSize));
                    State = br.ReadByte();
                }
            }
        }
    }
}
