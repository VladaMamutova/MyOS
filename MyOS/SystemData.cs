using System.IO;
using System.Text;

namespace MyOS
{
    class SystemData
    {
        public static readonly char VolumeName;
        public static readonly char Root;
        public static readonly string FileSystemVersion;
        public static readonly byte State;
        public const int VolumeSize = 419430400; // Размер раздела с операционной системой.
        public const int MftAreaSize = 41943040; // Размер MFT-пространства (10% от всего размера).
        public const int RootSize = 409600; // Размер корневого каталога (100 кластеров * 4096 байтов).

        public const ushort BytesPerCluster = 4096; // Размер кластера в байтах.

        public static SystemBuffer Buffer;
        
        static SystemData()
        {
            using (BinaryReader br = new BinaryReader(File.Open(SystemConstants.SystemFile, FileMode.Open)))
            {
                br.BaseStream.Seek(SystemConstants.VolumeRecNumber * SystemConstants.MftRecordSize + MftHeader.Length, SeekOrigin.Begin);
                VolumeName = br.ReadChar();
                FileSystemVersion = Encoding.UTF8.GetString(br.ReadBytes(9));
                State = br.ReadByte();
                br.BaseStream.Seek(SystemConstants.RootDirectoryRecNumber * SystemConstants.MftRecordSize + 2, SeekOrigin.Begin);
                Root = br.ReadChar();
            }
            Bitmap.InitializeFreeClusters();
        }
    }
}
