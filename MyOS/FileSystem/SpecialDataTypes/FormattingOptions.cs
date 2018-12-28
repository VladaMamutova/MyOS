namespace MyOS.FileSystem.SpecialDataTypes
{
    public struct FormattingOptions
    {
        public int VolumeSize;
        public int BytesPerCluster;
        public string FsName;
        public string VolumeName;

        public FormattingOptions(int volumeSize, int bytesPerCluster, string fsName, string volumeName)
        {
            VolumeSize = volumeSize;
            BytesPerCluster = bytesPerCluster;
            FsName = fsName;
            VolumeName = volumeName;
        }
    }
}