using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Blockchain.Models
{
    public class UserLoginModel
    {
        [Required(ErrorMessage = "Molimo unesite ispravan ključ za prijavu!")]
        public string UserLoginKey { get; set; }
    }
}
