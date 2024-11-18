using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WebProjekat.Models
{
    public class Rezervacija
    {
        public Guid Id { get; set; }
        public string Korisnik { get; set; }
        public Guid Let { get; set; }
        public int BrojPutnika { get; set; }
        public decimal UkupnaCena { get; set; }
        public StatusRezervacije Status { get; set; }

        public Rezervacija()
        {
            Id = Guid.NewGuid();
        }
    }

    public enum StatusRezervacije
    {
        Kreirana,
        Odobrena,
        Otkazana,
        Zavrsena
    }
}