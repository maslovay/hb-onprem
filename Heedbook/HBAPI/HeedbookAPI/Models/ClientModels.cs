using HBData.Models;
using System;
using System.Collections.Generic;

namespace UserOperations.Models
{
    public class GetClient
    {
        public Guid ClientId { get; set; }
        public string Name { get; set; }
        public string Phone { get; set; }
        public string Email { get; set; }
        public string Gender { get; set; }//male-female
        public int Age { get; set; }
        public string Avatar { get; set; }
        public Int32? StatusId { get; set; }
        public Guid CompanyId { get; set; }
        public Guid? CorporationId { get; set; }
        public IEnumerable<Guid> DialogueIds { get; set; }
        public IEnumerable<ClientNote> ClientNotes { get; set; }
    }
}
