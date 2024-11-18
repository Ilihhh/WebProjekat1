using System;
using Newtonsoft.Json;

namespace WebProjekat.Models
{
    public class Let
    {
        //[JsonProperty("id")]
        public Guid Id { get; set; }
        public string Aviokompanija { get; set; }
        public string PolaznaDestinacija { get; set; }
        public string OdredisnaDestinacija { get; set; }
        public DateTime DatumIVremePolaska { get; set; }
        public DateTime DatumIVremeDolaska { get; set; }
        public int BrojSlobodnihMesta { get; set; }
        public int BrojZauzetihMesta { get; set; }
        public decimal Cena { get; set; }
        public StatusLeta Status { get; set; }
        public bool IsRemoved { get; set; } = false;

        // Konstruktor koji generiše novi Guid za svaki novi let
        public Let()
        {
            Id = Guid.NewGuid();
        }

        // Implementacija Equals metode
        public override bool Equals(object obj)
        {
            if (!(obj is Let))
                return false;

            var other = obj as Let;

            return this.Id == other.Id &&
                   this.Aviokompanija == other.Aviokompanija &&
                   this.PolaznaDestinacija == other.PolaznaDestinacija &&
                   this.OdredisnaDestinacija == other.OdredisnaDestinacija &&
                   this.DatumIVremePolaska == other.DatumIVremePolaska &&
                   this.DatumIVremeDolaska == other.DatumIVremeDolaska &&
                   this.BrojSlobodnihMesta == other.BrojSlobodnihMesta &&
                   this.BrojZauzetihMesta == other.BrojZauzetihMesta &&
                   this.Cena == other.Cena &&
                   this.Status == other.Status;
        }

        // Implementacija GetHashCode metode
        public override int GetHashCode()
        {
            return Id.GetHashCode() ^
                   Aviokompanija.GetHashCode() ^
                   PolaznaDestinacija.GetHashCode() ^
                   OdredisnaDestinacija.GetHashCode() ^
                   DatumIVremePolaska.GetHashCode() ^
                   DatumIVremeDolaska.GetHashCode() ^
                   BrojSlobodnihMesta.GetHashCode() ^
                   BrojZauzetihMesta.GetHashCode() ^
                   Cena.GetHashCode() ^
                   Status.GetHashCode();
        }
    }

    public enum StatusLeta
    {
        Aktivan,
        Otkazan,
        Zavrsen
    }
}
