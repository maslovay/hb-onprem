using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace PostgreSQL.Models
{
    public class Language
    {
        public int LanguageId { get; set; }

        //название языка
        public string LanguageName { get; set; }

        //Локальное название языка
        public string LanguageLocalName { get; set; }

        //короткое название языка
        public string LanguageShortName { get; set; }

        //links
        //компании языка
        public  ICollection<Company> Company { get; set; }

        //диалоги языка
        public  ICollection<Dialogue> Dialogue { get; set; }
    }
}
