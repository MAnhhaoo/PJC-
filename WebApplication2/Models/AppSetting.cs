using System.ComponentModel.DataAnnotations;

namespace WebApplication2.Models
{
    public class AppSetting
    {
        [Key]
        public string Key { get; set; } = "";

        public string Value { get; set; } = "";
    }
}
