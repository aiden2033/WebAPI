using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace WebAPI.Models
{
    public class User
    {
        [Key]
        public int Id { get; set; }
        public string Login { get; set; }
        [MinLength(8)]
        public string Password { get; set; }
        public string Role { get; set; }
        public string Token { get; set; }
    }
}
