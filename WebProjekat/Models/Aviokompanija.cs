using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WebProjekat.Models
{
    public class Aviokompanija
    {
        public string Naziv { get; set; }
        public string Adresa { get; set; }
        public string KontaktInformacije { get; set; }
        public List<Let> ListaLetova { get; set; }
        public List<Recenzija> ListaRecenzija { get; set; }
        public bool IsRemoved { get; set; } = false;

        public Aviokompanija()
        {
            ListaLetova = new List<Let>();
            ListaRecenzija = new List<Recenzija>();
        }
    }
}