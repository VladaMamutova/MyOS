namespace MyOS.FileSystem.SpecialDataTypes
{
    public enum ClusterState : byte
    {
        Free = 0,
        Damaged = 1,
        Service = 0b10,
        Busy = 0b11
    }
}