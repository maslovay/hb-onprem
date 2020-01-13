using System;

namespace UserOperations.Models.Session
{
    public class SessionParams
    {
        public Guid? ApplicationUserId;
        public Guid DeviceId;
        public string Action;
        public bool? IsDesktop;
    }
}