using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WebProjekat.Models
{
    public class Recenzija
    {
        public string Recenzent { get; set; }
        public string Aviokompanija { get; set; }
        public string Naslov { get; set; }
        public string Sadrzaj { get; set; }
        public string Slika { get; set; } // Opcioni parametar
        public StatusRecenzije Status { get; set; }
    }

    public enum StatusRecenzije
    {
        Kreirana,
        Odobrena,
        Odbijena
    }
}