namespace DeleteScheduler.Models
{
    public class RemoveReport
    {
        public string FolderName { get; set; }
        public int RemovedFileCount { get; set; }
        public RemoveReport(string folderName, int fileCount)
        {
            FolderName = folderName;
            RemovedFileCount = fileCount;
        }
    }
}