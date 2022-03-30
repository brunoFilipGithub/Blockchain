using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Blockchain.Models
{
    public class AuthorityLoginModel
    {
        [Required(ErrorMessage = "Molimo unesite svoj ključ za prijavu")]
        public string AuthorityUserLoginKey { get; set; }
    }
}
