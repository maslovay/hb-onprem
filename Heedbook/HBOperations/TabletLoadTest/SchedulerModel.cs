using System;

namespace TabletLoadTest
{
    public class SchedulerModel
    {
        public Guid DeviceId {get;set;}
        public Guid CompanyId {get;set;}
        public Guid ApplicationUserId {get; set;}
        public string RequestName {get; set;}
    }
}