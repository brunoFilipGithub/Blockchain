using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Blockchain.Models
{
    public class UserRegisterModel
    {
        [Required(ErrorMessage ="Unos OIB-a je obavezan!")]
        [RegularExpression("(^[0-9]{11}$)", ErrorMessage = "OIB mora sadržavati samo brojeve i imati 11 znamenaka!")]
        public string OIB { get; set; }

        [Required(ErrorMessage = "Unos e-mail adrese je obavezan!")]
        [EmailAddress(ErrorMessage = "E-mail adresa nije dobrog formata!")]
        public string Email { get; set; }
    }
}
