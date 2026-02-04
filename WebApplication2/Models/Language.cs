using System.ComponentModel.DataAnnotations;

namespace WebApplication2.Models
{
    public class Language
    {
        [Key]
        public int LanguageId { get; set; }

        public string Code { get; set; }   // vi, en, jp
        public string Name { get; set; }
    }
}
