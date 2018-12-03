namespace MyOS
{
    struct FileOffsetInDirectory
    {
        public int DirectoryRecordNumberInMft;
        public int FileRecordNumberInDirectory;
        //public int FileStartRecordInMft;
        //public int FileEndRecordInMft;

        public FileOffsetInDirectory(int directoryNumber, int fileNumber/*, int startRecordInMft, int endRecordInMft*/)
        {
            DirectoryRecordNumberInMft = directoryNumber;
            FileRecordNumberInDirectory = fileNumber;
            //FileStartRecordInMft = startRecordInMft;
            //FileEndRecordInMft = endRecordInMft;
        }

        //public FileOffsetInDirectory(int directoryNumber, int fileNumber)
        //{
        //    DirectoryRecordNumberInMft = directoryNumber;
        //    DirectoryBlockInMft = directoryNumber;
        //    FileRecordNumberInDirectory = fileNumber;
        //}
    } 
}
