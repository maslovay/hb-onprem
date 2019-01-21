namespace HBData.Models
{
    public class WorkerType
    {
        public int WorkerTypeId { get; set; }

        //company of position
        public int CompanyId { get; set; }
        public  Company Company { get; set; }

        //position name
        public string WorkerTypeName { get; set; }
    }
}
