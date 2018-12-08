using System.Collections.Generic;

namespace MyOS
{
    struct DataAttributes
    {
        public int MftEntry; 
        public int Size;
        public int BlockSize;
        public List<int> Blocks;

        public DataAttributes(bool isSystem, int mftEntry, int size, int[] blocks)
        {
            MftEntry = mftEntry;
            Size = size;
            BlockSize = isSystem ? SystemConstants.MftRecordSize - 1 : SystemData.BytesPerCluster;
            Blocks = new List<int>(blocks);
        }
    }
}
