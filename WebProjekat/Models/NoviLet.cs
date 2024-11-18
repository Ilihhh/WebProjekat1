using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WebProjekat.Models
{
    public class NoviLet
    {
        public string AviokompanijaNovo { get; set; }
        public string PolaznaDestinacijaNovo { get; set; }
        public string OdredisnaDestinacijaNovo { get; set; }
        public DateTime DatumIVremePolaskaNovo { get; set; }
        public DateTime DatumIVremeDolaskaNovo { get; set; }
        public int BrojSlobodnihMestaNovo { get; set; }
        public decimal CenaNovo { get; set; }
    }
}