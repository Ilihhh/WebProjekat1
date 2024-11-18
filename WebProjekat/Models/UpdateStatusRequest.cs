using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WebProjekat.Models
{
    public class UpdateStatusRequest
    {
        public Guid ReservationId { get; set; }
        public StatusRezervacije NewStatus { get; set; }
        public string Korisnik { get; set; }
        public int BrojPutnika { get; set; }
    }
}