namespace MyOS
{
    struct FileOffsetInDirectory
    {
        public int DirectoryRecordNumberInMft;
        public int FileOffset;

        public FileOffsetInDirectory(int directoryNumber, int fileNumber/*, int startRecordInMft, int endRecordInMft*/)
        {
            DirectoryRecordNumberInMft = directoryNumber;
            FileOffset = fileNumber;
        }
    } 
}
