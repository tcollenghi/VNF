using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace VNF.Portal.Models
{
    public class Login
    {
        public string Email { get; set; }

        public string Senha { get; set; }

        public string Prestador { get; set; }

        public string IDPrestador { get; set; }
    }
}