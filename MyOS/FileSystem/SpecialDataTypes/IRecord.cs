namespace MyOS.FileSystem.SpecialDataTypes
{
    public interface IRecord
    {
        byte[] GetBytes();
        void SetRecord(byte[] recordBytes);
        int GetLength();

        bool Equals(byte[] recordBytes);
        bool EqualsByName(string name);
    }
}