using System.Collections.Generic;

namespace MyOS.FileSystem.SpecialDataTypes
{
    /// <summary>
    /// Атрибут Data в Mft-записи.
    /// </summary>
    struct Data
    {
        public int Size;
        public List<int> Clusters;

        public Data(int size, int[] clusters)
        {
            Size = size;
            Clusters = new List<int>(clusters);
        }
    }
}
