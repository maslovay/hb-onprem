using System;

namespace ApiPerformance.Models
{
    public class PostUser
    {
        public string FullName;
        public string Email;
        public string EmployeeId;
        public Guid? RoleId;
        public string Password;
        public Guid? CompanyId;
    }
}