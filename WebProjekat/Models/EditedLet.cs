using System;
using WebProjekat.Models;

public class EditedLet
{
    public Guid Id { get; set; }
    public string PolaznaDestinacija { get; set; }
    public string OdredisnaDestinacija { get; set; }
    public DateTime DatumIVremePolaska { get; set; }
    public DateTime DatumIVremeDolaska { get; set; }
    public int BrojSlobodnihMesta { get; set; }
    public decimal Cena { get; set; }
    public StatusLeta Status { get; set; }
}