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
        public IEnumerable<GetClientNote> ClientNotes { get; set; }
    }

    public class PutClient
    {
        public Guid ClientId { get; set; }
        public string Name { get; set; }
        public string Phone { get; set; }
        public string Email { get; set; }
        public string Gender { get; set; }//male-female
        public int? Age { get; set; }
        public string Avatar { get; set; }
        public Int32? StatusId { get; set; }
    }

    public class GetClientNote
    {
        public GetClientNote(ClientNote note, ApplicationUser user)
        {
            ClientNoteId = note.ClientNoteId;
            Text = note.Text;
            ClientId = note.ClientId;
            CreationDate = note.CreationDate;
            AuthorName = user != null? user.FullName : "";
        }
        public Guid ClientNoteId { get; set; }
        public string Text { get; set; }
        public Guid ClientId { get; set; }
        public DateTime CreationDate { get; set; }
        public string AuthorName { get; set; }
    }

    public class PutClientNote
    {
        public Guid ClientNoteId { get; set; }
        public string Text { get; set; }
    }

    public class PostClientNote
    {
        public Guid ClientId { get; set; }
        public string Text { get; set; }
    }
}
