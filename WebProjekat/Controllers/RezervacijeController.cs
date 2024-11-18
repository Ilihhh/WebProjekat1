using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Hosting;
using System.Web.Http;
using WebProjekat.Models;

namespace WebProjekat.Controllers
{
    public class RezervacijeController : ApiController
    {
        private LetoviController letoviController = new LetoviController(); // Instantiate LetoviController

        [HttpPost]
        [Route("api/Reservations/Create")]
        public IHttpActionResult CreateReservation(ReservationRequest request)
        {
            var korisnik = CheckSession(); // Get the logged-in user

            if (korisnik == null)
            {
                return Unauthorized();
            }

            var letId = request.FlightId;
            var numPassengers = request.NumPassengers;

            // Fetch the flight from LetoviController
            var let = GetFlight(letId);

            if (let == null)
            {
                return NotFound();
            }

            // Check if there are enough available seats
            if (let.BrojSlobodnihMesta < numPassengers)
            {
                return BadRequest("Not enough available seats for this flight.");
            }

            // Update flight seats availability
            let.BrojSlobodnihMesta -= numPassengers;
            let.BrojZauzetihMesta += numPassengers;

            // Update flight in JSON file
            UpdateFlight(let);

            // Calculate the total price based on the number of passengers and flight price
            decimal totalPrice = let.Cena * numPassengers;

            // Create the reservation
            var reservation = new Rezervacija
            {
                Id = Guid.NewGuid(),
                Korisnik = korisnik.KorisnickoIme,
                Let = let.Id,
                BrojPutnika = numPassengers,
                UkupnaCena = totalPrice,
                Status = StatusRezervacije.Kreirana // Default status when created
            };

            // Add reservation to user's list
            korisnik.ListaRezervacija.Add(reservation);

            // Update user in JSON file and session
            UpdateUserAndSession(korisnik);

            return Ok(reservation);
        }

        [HttpPost]
        [Route("api/Reservations/Cancel")]
        public IHttpActionResult CancelReservation(ReservationIdClass k)
        {
            var korisnik = CheckSession(); // Get the logged-in user

            if (korisnik == null)
            {
                return Unauthorized();
            }

            // Find the reservation by ID in user's list
            var reservation = korisnik.ListaRezervacija.FirstOrDefault(r => r.Id == k.reservationId);

            if (reservation == null)
            {
                return NotFound();
            }

            // Check if the reservation can be canceled
            if (reservation.Status != StatusRezervacije.Kreirana && reservation.Status != StatusRezervacije.Odobrena)
            {
                return BadRequest("Reservation cannot be canceled at this time.");
            }

            // Fetch the flight from LetoviController
            var let = GetFlight(reservation.Let);

            if (let == null)
            {
                return NotFound();
            }

            // Update flight seats availability
            let.BrojSlobodnihMesta += reservation.BrojPutnika;
            let.BrojZauzetihMesta -= reservation.BrojPutnika;

            // Update flight in JSON file
            UpdateFlight(let);

            // Update reservation status to "Otkazana"
            reservation.Status = StatusRezervacije.Otkazana;

            // Update user in JSON file and session
            UpdateUserAndSession(korisnik);

            return Ok("Reservation canceled successfully.");
        }

        [HttpPost]
        [Route("api/Reservations/ChangeStatus")]
        public IHttpActionResult UpdateReservationStatus(UpdateStatusRequest request)
        {
            var korisnik = CheckSession(); // Get the logged-in user

            if (korisnik == null || korisnik.Tip != TipKorisnika.Administrator)
            {
                return Unauthorized();
            }

            var reservationId = request.ReservationId;
            var newStatus = request.NewStatus;
            var brojPutnika = request.BrojPutnika;


            var korisniciFilePath = HostingEnvironment.MapPath("~/App_Data/korisnici.json");
            var aviokompanijeFilePath = HostingEnvironment.MapPath("~/App_Data/aviokompanije.json");

            if (!File.Exists(korisniciFilePath) || !File.Exists(aviokompanijeFilePath))
            {
                return NotFound();
            }

            var korisniciJson = File.ReadAllText(korisniciFilePath);
            var aviokompanijeJson = File.ReadAllText(aviokompanijeFilePath);

            var users = JsonConvert.DeserializeObject<List<Korisnik>>(korisniciJson) ?? new List<Korisnik>();
            var aviokompanije = JsonConvert.DeserializeObject<List<Aviokompanija>>(aviokompanijeJson) ?? new List<Aviokompanija>();

            Rezervacija reservation = null;
            Korisnik reservationOwner = null;

            foreach (var user in users)
            {
                reservation = user.ListaRezervacija.FirstOrDefault(r => r.Id == reservationId);
                if (reservation != null)
                {
                    reservationOwner = user;
                    break;
                }
            }

            if (reservation == null)
            {
                return NotFound();
            }
            //Provera da li je mozda korisnik vec otkazao rezervaciju
            if (reservation.Status == newStatus)
            {
                return BadRequest("Greska prilikom izmene statusa");
            }

            // Pronađi odgovarajući let
            Let let = null;
            foreach (var aviokompanija in aviokompanije)
            {
                let = aviokompanija.ListaLetova.FirstOrDefault(l => l.Id == reservation.Let);
                if (let != null)
                {
                    break;
                }
            }

            if (let == null)
            {
                return NotFound();
            }

            // Ako je nova status rezervacije Otkazana, smanji broj zauzetih mesta i povećaj broj slobodnih mesta
            if (newStatus == StatusRezervacije.Otkazana)
            {
                let.BrojZauzetihMesta -= brojPutnika;
                let.BrojSlobodnihMesta += brojPutnika;
            }
            

            reservation.Status = newStatus;

            // Update korisnici i aviokompanije u JSON fajlovima
            File.WriteAllText(korisniciFilePath, JsonConvert.SerializeObject(users, Formatting.Indented));
            File.WriteAllText(aviokompanijeFilePath, JsonConvert.SerializeObject(aviokompanije, Formatting.Indented));

            return Ok("Reservation status updated successfully.");
        }

        // Helper method to get flight details from LetoviController
        private Let GetFlight(Guid id)
        {
            IHttpActionResult result = letoviController.GetLetById(id);
            var contentResult = result as System.Web.Http.Results.OkNegotiatedContentResult<Let>;

            if (contentResult != null)
            {
                return contentResult.Content;
            }

            return null;
        }

        // Helper method to update flight in JSON file
        private void UpdateFlight(Let updatedLet)
        {
            var filePath = HostingEnvironment.MapPath("~/App_Data/aviokompanije.json");

            if (!File.Exists(filePath))
            {
                return;
            }

            var json = File.ReadAllText(filePath);
            var aviokompanije = JsonConvert.DeserializeObject<List<Aviokompanija>>(json) ?? new List<Aviokompanija>();

            foreach (var aviokompanija in aviokompanije)
            {
                var existingLet = aviokompanija.ListaLetova.FirstOrDefault(l => l.Id == updatedLet.Id);
                if (existingLet != null)
                {
                    existingLet.BrojSlobodnihMesta = updatedLet.BrojSlobodnihMesta;
                    existingLet.BrojZauzetihMesta = updatedLet.BrojZauzetihMesta;
                }
            }

            // Save updated list to file
            File.WriteAllText(filePath, JsonConvert.SerializeObject(aviokompanije, Formatting.Indented));
        }

        // Helper method to update user in JSON file and session
        private void UpdateUserAndSession(Korisnik updatedUser)
        {
            var filePath = HostingEnvironment.MapPath("~/App_Data/korisnici.json");

            if (!File.Exists(filePath))
            {
                return;
            }

            var json = File.ReadAllText(filePath);
            var users = JsonConvert.DeserializeObject<List<Korisnik>>(json) ?? new List<Korisnik>();

            var existingUser = users.FirstOrDefault(u => u.KorisnickoIme == updatedUser.KorisnickoIme);
            if (existingUser != null)
            {
                existingUser.ListaRezervacija = updatedUser.ListaRezervacija;

                // Save updated list to file
                File.WriteAllText(filePath, JsonConvert.SerializeObject(users, Formatting.Indented));

                // Update session
                HttpContext.Current.Session["Korisnik"] = existingUser;
            }
        }

        // Helper method to retrieve the logged-in user
        private Korisnik CheckSession()
        {
            var session = HttpContext.Current.Session;
            if (session["Korisnik"] != null)
            {
                return session["Korisnik"] as Korisnik;
            }

            return null;
        }
    }
}
