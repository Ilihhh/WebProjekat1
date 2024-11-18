using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WebProjekat.Models
{
    public class EditProfileModel
    {
        public string KorisnickoIme { get; set; }
        public string Ime { get; set; }
        public string Prezime { get; set; }
        public string Email { get; set; }
        public DateTime DatumRodjenja { get; set; }
        public string Pol { get; set; } // Može biti string 'M' ili 'Ž' ili bilo koji drugi format koji koristiš
        public string StaraLozinka { get; set; }
        public string NovaLozinka { get; set; }
    }
}