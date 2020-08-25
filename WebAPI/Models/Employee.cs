using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace WebAPI.Models
{
    public class Employee
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public string Surname { get; set; }
        [Required]
        public string Name { get; set; }
        public string MiddleName { get; set; }
        public DateTime Birthday { get; set; }
        [Required]
        public SubDivision SubDivision { get; set; } //подразделение 
        [Required]
        public Position Position { get; set; } //должность
    }
}
