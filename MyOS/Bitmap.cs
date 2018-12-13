using System.Collections.Generic;
using System.IO;

namespace MyOS
{
    public static class Bitmap
    {
        public enum ClusterState : byte
        {
            Free = 0,
            Damaged = 1,
            Service = 0b10,
            Busy = 0b11
        }

        public static int FreeClusters;

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
                            if ((fourClustersInfo & 0b00000011) == (byte)ClusterState.Free)
                                FreeClusters++; // Увеличиваем счётчик свободных класетров

                            // Сдвигаемся на 2 бита вправо для получения информации о следующем кластере.
                            fourClustersInfo = (byte)(fourClustersInfo >> 2);
                        }
                        byteNumber++;
                    }
                }
            }
        }

        public static List<int> GetFreeClusters(int requiredNumber)
        {
            List<int> clusterNumbers = new List<int>();
            DataAttributes dataAttribytes = SystemCalls.ReadDataAttributes(SystemConstants.BitmapRecNumber);
            using (BinaryReader br = new BinaryReader(File.Open(SystemConstants.SystemFile, FileMode.Open)))
            {
                int byteNumber = 0; // Номер прочитанного байта в битовой карте.

                foreach (var block in dataAttribytes.Blocks)
                {
                    br.BaseStream.Seek(block * SystemConstants.MftRecordSize + 1, SeekOrigin.Begin);
                    for (int i = 0; i < SystemConstants.MftRecordSize - 1; i++) // -1 байт для признака записи Mft.
                    {
                        if (byteNumber >= dataAttribytes.Size) return clusterNumbers;

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
            DataAttributes dataAttribytes = SystemCalls.ReadDataAttributes(SystemConstants.BitmapRecNumber);
            using (BinaryReader br = new BinaryReader(File.Open(SystemConstants.SystemFile, FileMode.Open)))
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
                    int blockNumber = clusterByteNumber / (SystemConstants.MftRecordSize - 1);
                    int byteOffset = clusterByteNumber % (SystemConstants.MftRecordSize - 1);

                    br.BaseStream.Seek(
                        dataAttribytes.Blocks[blockNumber] * SystemConstants.MftRecordSize + 1 + byteOffset,
                        SeekOrigin.Begin);

                    byte modifiedByte = ModifyByte(br.ReadByte(), clusterOffset, state);
                    
                    BinaryWriter bw = new BinaryWriter(br.BaseStream);
                    bw.BaseStream.Position -= 1; // Смещаемся на один байт назад для его перезаписи.

                    bw.Write(modifiedByte);
                    if (state == ClusterState.Free) FreeClusters++;
                    else FreeClusters--;
                }
            }
        }

        public static byte ModifyByte(byte sourceByte, int clusterNumberInByte, ClusterState state)
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
    }
}
