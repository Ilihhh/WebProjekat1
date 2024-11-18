using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Hosting;
using System.Web.Http;
using System.Web.Http.Results;
using WebProjekat.Models;

namespace WebProjekat.Controllers
{
    public class LetoviController : ApiController
    {
        private KorisniciController korisniciController = new KorisniciController();

        private static readonly string FilePath = HostingEnvironment.MapPath("~/App_Data/aviokompanije.json");

        // GET: api/Letovi
        public IEnumerable<Let> Get()
        {
            return LoadLetovi();
        }

        // GET: api/Letovi/{id}
        [HttpGet]
        [Route("api/Flights/{id}")]
        public IHttpActionResult GetLetById(Guid id)
        {
            var letovi = LoadLetovi();
            var let = letovi.FirstOrDefault(l => l.Id == id);

            if (let == null)
            {
                return NotFound();
            }

            return Ok(let);
        }

        // POST: api/Flights/Search
        [HttpPost]
        [Route("api/Flights/Search")]
        public IHttpActionResult SearchFlights(SearchParameters searchParams)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var letovi = LoadLetovi();

            var filteredLetovi = letovi.Where(l =>
                (searchParams.PolaznaDestinacija == null || l.PolaznaDestinacija.ToLower().Contains(searchParams.PolaznaDestinacija.ToLower())) &&
                (searchParams.OdredisnaDestinacija == null || l.OdredisnaDestinacija.ToLower().Contains(searchParams.OdredisnaDestinacija.ToLower())) &&
                (searchParams.DatumPolaska == null || l.DatumIVremePolaska.Date == searchParams.DatumPolaska.Value.Date) &&
                (searchParams.DatumDolaska == null || l.DatumIVremeDolaska.Date == searchParams.DatumDolaska.Value.Date) &&
                (searchParams.Aviokompanija == null || l.Aviokompanija.ToLower().Contains(searchParams.Aviokompanija.ToLower())) &&
                (searchParams.StatusLeta == null || l.Status == searchParams.StatusLeta)
            ).ToList();

            return Ok(filteredLetovi);
        }

        // POST: api/Flights/Add
        [HttpPost]
        [Route("api/Flights/Add")]
        public IHttpActionResult Add(NoviLet noviLet)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            //datumske provere
            var currentDateTime = DateTime.Now;
            if(noviLet.DatumIVremePolaskaNovo < currentDateTime)
            {
                return BadRequest("Datum i vreme polaska ne mogu biti manji od trenutnog datuma");
            }
            if(noviLet.DatumIVremeDolaskaNovo < currentDateTime)
            {
                return BadRequest("Datum i vreme dolaska ne mogu biti manji od trenutnog datuma");
            }
            if(noviLet.DatumIVremeDolaskaNovo < noviLet.DatumIVremePolaskaNovo)
            {
                return BadRequest("Dolazak ne moze biti pre polaska leta");
            }

            var aviokompanije = LoadAviokompanije();
            var aviokompanija = aviokompanije.FirstOrDefault(a => a.Naziv == noviLet.AviokompanijaNovo);

            if (aviokompanija == null)
            {
                return NotFound();
            }

            var let = new Let
            {
                Id = Guid.NewGuid(),
                Aviokompanija = noviLet.AviokompanijaNovo,
                PolaznaDestinacija = noviLet.PolaznaDestinacijaNovo,
                OdredisnaDestinacija = noviLet.OdredisnaDestinacijaNovo,
                DatumIVremePolaska = noviLet.DatumIVremePolaskaNovo,
                DatumIVremeDolaska = noviLet.DatumIVremeDolaskaNovo,
                BrojSlobodnihMesta = noviLet.BrojSlobodnihMestaNovo,
                BrojZauzetihMesta = 0,
                Cena = noviLet.CenaNovo,
                Status = StatusLeta.Aktivan
            };

            aviokompanija.ListaLetova.Add(let);
            SaveAviokompanije(aviokompanije);

            return Ok(let);
        }

        // POST: api/Flights/Delete/{id}
        [HttpPost]
        [Route("api/Flights/Delete/{id}")]
        public IHttpActionResult Delete(Guid id)
        {
            var aviokompanije = LoadAviokompanije();
            var letToDelete = aviokompanije
                                .SelectMany(a => a.ListaLetova)
                                .FirstOrDefault(l => l.Id == id);

            if (letToDelete == null)
            {
                return NotFound();
            }

            // Provera da li postoje rezervacije sa statusom 'kreirana' ili 'odobrena'
            bool reservationsExist = false;

            // Učitavanje korisnika sa rezervacijama
            IHttpActionResult result = korisniciController.Get();
            if (result is OkNegotiatedContentResult<List<Korisnik>>)
            {
                var okResult = (OkNegotiatedContentResult<List<Korisnik>>)result;
                var korisnici = okResult.Content;

                // Provera svake rezervacije
                foreach (var korisnik in korisnici)
                {
                    foreach (var rezervacija in korisnik.ListaRezervacija)
                    {
                        if (rezervacija.Let == letToDelete.Id && (rezervacija.Status == StatusRezervacije.Kreirana || rezervacija.Status == StatusRezervacije.Odobrena))
                        {
                            reservationsExist = true;
                            break;
                        }
                    }
                    if (reservationsExist)
                        break;
                }
            }

            // Ako postoje rezervacije, let se ne može obrisati
            if (reservationsExist)
            {
                return BadRequest("Nije moguće obrisati let jer postoje rezervacije sa statusom 'kreirana' ili 'odobrena'.");
            }

            // Postavi IsRemoved na true
            letToDelete.IsRemoved = true;

            SaveAviokompanije(aviokompanije); // Čuvanje promena u fajlu

            return Ok();
        }


        [HttpPost]
        [Route("api/Flights/Update")]
        public IHttpActionResult Update(EditedLet updatedLet)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var aviokompanije = LoadAviokompanije();
            var letToUpdate = aviokompanije
                                .SelectMany(a => a.ListaLetova)
                                .FirstOrDefault(l => l.Id == updatedLet.Id);

            if (letToUpdate == null)
            {
                return NotFound();
            }

            // Provera da li je novi broj slobodnih mesta manji od broja zauzetih mesta
            if (updatedLet.BrojSlobodnihMesta < letToUpdate.BrojZauzetihMesta)
            {
                ModelState.AddModelError("BrojSlobodnihMesta", "Broj slobodnih mesta ne može biti manji od broja zauzetih mesta.");
                return BadRequest(ModelState);
            }

            if(updatedLet.DatumIVremeDolaska < updatedLet.DatumIVremePolaska)
            {
                ModelState.AddModelError("DatumIVremePolaska", "Dolazak leta ne moze biti pre polaska");
                return BadRequest(ModelState);
            }

            // Provera da li postoji rezervacija za dati let sa statusom Kreirana ili Odobrena
            IHttpActionResult result = korisniciController.Get(); // Učitavanje korisnika sa rezervacijama
            if (result is OkNegotiatedContentResult<List<Korisnik>>)
            {
                // Kastujete result u odgovarajući tip
                var okResult = (OkNegotiatedContentResult<List<Korisnik>>)result;

                // Dobijete listu korisnika
                var korisnici = okResult.Content;

                var rezervacijaPostoji = korisnici
                                        .Where(k => k.ListaRezervacija.Any(r => r.Let == updatedLet.Id && (r.Status == StatusRezervacije.Kreirana || r.Status == StatusRezervacije.Odobrena)))
                                        .Any();
                //cena brt
                if (rezervacijaPostoji && letToUpdate.Cena != updatedLet.Cena)
                {
                    ModelState.AddModelError("PromenaCene", "Nije moguce promeniti cenu leta koji ima aktivne rezervacije.");
                    return BadRequest(ModelState);
                }
            }

            // Ažuriranje podataka o letu
            letToUpdate.PolaznaDestinacija = updatedLet.PolaznaDestinacija;
            letToUpdate.OdredisnaDestinacija = updatedLet.OdredisnaDestinacija;
            letToUpdate.DatumIVremePolaska = updatedLet.DatumIVremePolaska;
            letToUpdate.DatumIVremeDolaska = updatedLet.DatumIVremeDolaska;
            letToUpdate.BrojSlobodnihMesta = updatedLet.BrojSlobodnihMesta;
            letToUpdate.Cena = updatedLet.Cena;
            letToUpdate.Status = updatedLet.Status;

            SaveAviokompanije(aviokompanije); // Čuvanje promena u fajlu

            return Ok(letToUpdate);
        }

        private List<Aviokompanija> LoadAviokompanije()
        {
            if (!File.Exists(FilePath))
            {
                return new List<Aviokompanija>();
            }

            var json = File.ReadAllText(FilePath);
            return JsonConvert.DeserializeObject<List<Aviokompanija>>(json) ?? new List<Aviokompanija>();
        }

        private void SaveAviokompanije(List<Aviokompanija> aviokompanije)
        {
            var json = JsonConvert.SerializeObject(aviokompanije, Formatting.Indented);
            File.WriteAllText(FilePath, json);
        }

        private List<Let> LoadLetovi()
        {
            var aviokompanije = LoadAviokompanije();
            var letovi = new List<Let>();
            var korisniciController = new KorisniciController(); // Instanciraj kontroler za korisnike

            // Dobavi listu korisnika
            IHttpActionResult result = korisniciController.Get();
            if (result is OkNegotiatedContentResult<List<Korisnik>> okResult)
            {
                // Dobijanje liste korisnika iz rezultata
                var korisnici = okResult.Content;

                foreach (var aviokompanija in aviokompanije)
                {
                    foreach (var let in aviokompanija.ListaLetova)
                    {
                        // Ažuriranje statusa leta na Zavrsen ako je prošlo vreme dolaska
                        if (let.Status != StatusLeta.Otkazan && let.DatumIVremeDolaska < DateTime.Now)
                        {
                            let.Status = StatusLeta.Zavrsen;
                        }

                        // Ažuriranje statusa rezervacija za let koji je završen
                        if (let.Status == StatusLeta.Zavrsen)
                        {
                            foreach (var kor in korisnici)
                            {
                                foreach (var rezervacija in kor.ListaRezervacija)
                                {
                                    if (rezervacija.Let == let.Id &&
                                        (rezervacija.Status == StatusRezervacije.Kreirana || rezervacija.Status == StatusRezervacije.Odobrena))
                                    {
                                        rezervacija.Status = StatusRezervacije.Zavrsena;
                                    }
                                }
                            }
                        }
                    }
                }
                korisniciController.SaveKorisnici(korisnici);
            }
            
            SaveAviokompanije(aviokompanije);
            var korisnik = CheckSession();

            if (korisnik == null)
            {
                foreach (var aviokompanija in aviokompanije)
                {
                    letovi.AddRange(aviokompanija.ListaLetova
                        .Where(let => let.Status == StatusLeta.Aktivan && !let.IsRemoved && !aviokompanija.IsRemoved));
                }
            }
            else if (korisnik.Tip == TipKorisnika.Administrator)
            {
                foreach (var aviokompanija in aviokompanije)
                {
                    letovi.AddRange(aviokompanija.ListaLetova
                        .Where(let => !let.IsRemoved && !aviokompanija.IsRemoved));
                }
            }
            else
            {
                foreach (var aviokompanija in aviokompanije)
                {
                    letovi.AddRange(aviokompanija.ListaLetova
                        .Where(let => (let.Status == StatusLeta.Aktivan ||
                                       (korisnik.ListaRezervacija.Any(r => r.Let == let.Id && (let.Status == StatusLeta.Otkazan || let.Status == StatusLeta.Zavrsen))))
                                       && !let.IsRemoved && !aviokompanija.IsRemoved));
                }
            }

            SaveAviokompanije(aviokompanije); // Save the updated statuses back to the file

            return letovi;
        }

        private Korisnik CheckSession()
        {
            Korisnik korisnik = null;
            if (HttpContext.Current.Session["Korisnik"] != null)
            {
                korisnik = (Korisnik)HttpContext.Current.Session["Korisnik"];
            }
            return korisnik;
        }
    }
}
