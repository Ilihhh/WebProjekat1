using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WebProjekat.Models
{
    public class SearchParameters
    {
        public string PolaznaDestinacija { get; set; }
        public string OdredisnaDestinacija { get; set; }
        public DateTime? DatumPolaska { get; set; }
        public DateTime? DatumDolaska { get; set; }
        public string Aviokompanija { get; set; }
        public StatusLeta? StatusLeta { get; set; }
    }
}