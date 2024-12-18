﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WebProjekat.Models
{
    public class Korisnik
    {
        public string KorisnickoIme { get; set; }
        public string Lozinka { get; set; }
        public string Ime { get; set; }
        public string Prezime { get; set; }
        public string Email { get; set; }
        public DateTime DatumRodjenja { get; set; }
        public string Pol { get; set; }
        public TipKorisnika Tip { get; set; }
        public List<Rezervacija> ListaRezervacija { get; set; }

        public Korisnik()
        {
            ListaRezervacija = new List<Rezervacija>();
        }
    }

    public enum TipKorisnika
    {
        Putnik,
        Administrator
    }
}