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
        public static int FreeClusters;

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
            InitializeFreeClusters();
        }

        public static void InitializeFreeClusters()
        {
            DataAttributes bitmap = SystemCalls.ReadDataAttributes(SystemConstants.BitmapRecNumber);
            using (BinaryReader br = new BinaryReader(File.Open(SystemConstants.SystemFile, FileMode.Open)))
            {
                int byteNumber = 0; // Номер прочитанного байта в битовой карте.

                foreach (var recordNumber in bitmap.Blocks)
                {
                    br.BaseStream.Seek(recordNumber * SystemConstants.MftRecordSize, SeekOrigin.Begin);
                    if (br.ReadByte() != (byte)MftHeader.Signature.IsData) return;

                    for (int i = 0; i < SystemConstants.MftRecordSize - 1; i++) // -1 байт для заголовка Data
                    {
                        if (byteNumber >= bitmap.Size) return;

                        byte fourClustersInfo = br.ReadByte();
                        // Кластер представляется 2 битами, значит в байте - информация о 4 кластерах.
                        for (int j = 0; j < 4; j++)
                        {
                            // Обнуляем первые шесть битов, при этом получая 2 младших бита,
                            // представляющих информацию об одном кластере.
                            if ((fourClustersInfo & 0b00000011) == (byte)SystemConstants.ClusterState.Free)
                                FreeClusters++; // Увеличиваем счётчик свободных класетров
                             
                            // Сдвигаемся на 2 бита вправо для получения информации о следующем кластере.
                            fourClustersInfo = (byte)(fourClustersInfo >> 2);
                        }
                        byteNumber++;
                    }
                }
            }
        }
    }
}
