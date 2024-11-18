using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WebProjekat.Models
{
    public class ReservationRequest
    {
        public Guid FlightId { get; set; }
        public int NumPassengers { get; set; }
    }
}