using System;

namespace MyOS.FileSystem.SpecialDataTypes
{
    struct MemoryAllocation
    {
        [Flags]
        public enum Attribute : byte
        {
            NeedToIncreaseMftSize = 0b1,
            NeedToIncreaseDirectorySize = 0b10,
            NeedNewCluster = 0b100
        }

        public int MftEntry { get; set; }
        public Attribute Attributes { get; }

        public MemoryAllocation(int mftEntry, Attribute attributes)
        {
            MftEntry = mftEntry;
            Attributes = attributes;
        }
    }
}