using HBData.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace UserOperations.Models
{
    public class PostUser
    {
        public string FullName;
        [Required]
        public string Email;
        public string EmployeeId;
        public Guid? RoleId;
        [Required]
        public string Password;
        public Guid? CompanyId;
    }
    public class CompanyModel
    {
        public Guid CompanyIndustryId;
        public string CompanyName;
        public int LanguageId;
        public Guid CountryId;
        public int StatusId;
        public Guid? CorporationId;
    }
    public class UserModel
    {
        public Guid Id { get; set; }
        public string Email { get; set; }
        public string FullName { get; set; }
        public string Avatar { get; set; }
        public string EmployeeId { get; set; }
        public DateTime CreationDate { get; set; }
        public Guid? CompanyId { get; set; }
        public Int32? StatusId { get; set; }
        public string OneSignalId { get; set; }
        public Guid? WorkerTypeId { get; set; }
        public ApplicationRole Role { get; set; }
        public Guid? RoleId { get; set; }
        public UserModel()
        {

        }
        public UserModel(ApplicationUser user, string avatar = null, ApplicationRole role = null)
        {
            Id = user.Id;
            FullName = user.FullName;
            Email = user.Email;
            Avatar = avatar;
            EmployeeId = user.EmpoyeeId;
            CreationDate = user.CreationDate;
            CompanyId = user.CompanyId;
            StatusId = user.StatusId;
            OneSignalId = user.OneSignalId;
            Role = user.UserRoles.FirstOrDefault()?.Role ?? role;
            RoleId = user.UserRoles.FirstOrDefault()?.RoleId;
        }
    }

    public class PhrasePost
    {
        public string PhraseText;
        public Guid PhraseTypeId;
        public Int32? LanguageId;
        public bool IsClient;
        public Int32? WordsSpace;
        public double? Accurancy;
        public Boolean IsTemplate;
    }

    public class DialoguePut
    {
        public Guid DialogueId;
        public List<Guid> DialogueIds;//--this done for versions
        public bool InStatistic;
    }

    public class DialogueSatisfactionPut
    {
        public Guid DialogueId;
        public double Satisfaction;
        public double BegMoodTotal;
        public double EndMoodTotal;
        public int Age;
        public string Gender;
    }
    public class VideoMessage
    {
        public string Subject;
        public string Body;
    }
}
