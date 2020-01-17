using HBData.Models;
using System;
using System.Collections.Generic;

namespace UserOperations.Models
{
    public class GetDevice
    {
        public Guid DeviceId { get; set; }
        public string Name { get; set; }
        public int StatusId { get; set; }
        public Guid CompanyId { get; set; }
        public Guid? DeviceTypeId { get; set; }
    }

    public class PostDevice
    {
        public string Name { get; set; }
        public String Code { get; set; }
        public Guid? CompanyId { get; set; }
        public Guid? DeviceTypeId { get; set; }
    }

    public class PutDevice
    {
        public Guid DeviceId { get; set; }
        public string Name { get; set; }
        public String Code { get; set; }
        public int StatusId { get; set; }
        public Guid? DeviceTypeId { get; set; }
    }

        public class GetUsersSessions
    {
            public Guid UserId { get; set; }
            public string FullName { get; set; }
            public string Avatar { get; set; }
            public string SessionStatus { get; set; }
            public Guid? DeviceId { get; set; }
        }
}
