using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Hosting;
using System.Web.Http;
using Newtonsoft.Json;
using WebProjekat.Models;

namespace WebProjekat.Controllers
{
    public class KorisniciController : ApiController
    {
        private readonly string filePath = HttpContext.Current.Server.MapPath("~/App_Data/korisnici.json");

        [HttpGet]
        public IHttpActionResult Get()
        {
            var korisnici = LoadKorisnici();
            return Ok(korisnici);
        }

        [HttpPost]
        [Route("api/Korisnici/Register")]
        public IHttpActionResult Register([FromBody] Korisnik noviKorisnik)
        {
            var korisnici = LoadKorisnici();

            // Proveri da li korisničko ime već postoji
            if (korisnici.Any(k => k.KorisnickoIme == noviKorisnik.KorisnickoIme))
            {
                return BadRequest("Korisničko ime već postoji.");
            }

            // Dodaj novog korisnika
            noviKorisnik.Tip = TipKorisnika.Putnik; // Uvek postavljamo tip na 'Putnik'
            korisnici.Add(noviKorisnik);

            // Sačuvaj korisnike nazad u JSON fajl  
            SaveKorisnici(korisnici);

            return Ok("Uspešno ste registrovani.");
        }

        [HttpPost]
        [Route("api/Korisnici/Login")]
        public IHttpActionResult Login([FromBody] LoginModel model)
        {
            var korisnici = LoadKorisnici();

            var korisnik = korisnici.SingleOrDefault(k => k.KorisnickoIme == model.KorisnickoIme && k.Lozinka == model.Lozinka);
            if (korisnik == null)
            {
                return Unauthorized();
            }

            // Postavljanje sesije
            HttpContext.Current.Session["Korisnik"] = korisnik;

            return Ok("Uspešno ste se prijavili.");
        }

        [HttpGet]
        [Route("api/Korisnici/UpdateSession")]
        public IHttpActionResult UpdateSession()
        {
            var korisnik = HttpContext.Current.Session["Korisnik"] as Korisnik;

            if (korisnik == null)
            {
                return Unauthorized();
            }

            var filePath = HostingEnvironment.MapPath("~/App_Data/korisnici.json");

            if (!File.Exists(filePath))
            {
                return NotFound();
            }

            var json = File.ReadAllText(filePath);
            var korisnici = JsonConvert.DeserializeObject<List<Korisnik>>(json) ?? new List<Korisnik>();

            var updatedKorisnik = korisnici.FirstOrDefault(k => k.KorisnickoIme == korisnik.KorisnickoIme);

            if (updatedKorisnik == null)
            {
                return NotFound();
            }

            // Update session with the newly loaded user
            HttpContext.Current.Session["Korisnik"] = updatedKorisnik;

            return Ok("Session updated successfully.");
        }


        [HttpGet]
        [Route("api/Korisnici/checkSession")]
        public IHttpActionResult CheckSession()
        {
            var korisnik = HttpContext.Current.Session["Korisnik"];
            if (korisnik != null)
            {
                return Ok(new { isLoggedIn = true, Korisnik = korisnik });
            }
            return Ok(new { isLoggedIn = false });
        }

        [HttpGet]
        [Route("api/Korisnici/logout")]
        public IHttpActionResult Logout()
        {
            HttpContext.Current.Session["Korisnik"] = null;
            return Ok("Uspesno ste se odjavili");
        }

        [HttpPost]
        [Route("api/Korisnici/editProfile")]
        public IHttpActionResult EditProfile([FromBody] EditProfileModel izmenjeniKorisnik)
        {
            var korisnik = HttpContext.Current.Session["Korisnik"] as Korisnik;
            if (korisnik == null)
            {
                return Unauthorized();
            }

            var korisnici = LoadKorisnici();

            // Pronađi korisnika koji se menja
            var korisnikZaIzmenu = korisnici.FirstOrDefault(k => k.KorisnickoIme == korisnik.KorisnickoIme);
            if (korisnikZaIzmenu == null)
            {
                return BadRequest("Korisnik nije pronađen.");
            }

            // Proveri da li se korisničko ime menja
            if (korisnikZaIzmenu.KorisnickoIme != izmenjeniKorisnik.KorisnickoIme)
            {
                // Ažuriraj korisničko ime u korisnikovim rezervacijama
                foreach (var rezervacija in korisnikZaIzmenu.ListaRezervacija)
                {
                    rezervacija.Korisnik = izmenjeniKorisnik.KorisnickoIme;
                }
            }

            // Provera trenutne lozinke
            if (!string.IsNullOrEmpty(izmenjeniKorisnik.StaraLozinka) && izmenjeniKorisnik.StaraLozinka != korisnikZaIzmenu.Lozinka)
            {
                return BadRequest("Pogrešna trenutna lozinka. Promene nisu sačuvane.");
            }

            // Provera da nova lozinka nije ista kao stara
            if (!string.IsNullOrEmpty(izmenjeniKorisnik.NovaLozinka) && izmenjeniKorisnik.NovaLozinka == korisnikZaIzmenu.Lozinka)
            {
                return BadRequest("Nova lozinka ne može biti ista kao stara lozinka.");
            }

            // Ažuriraj ostale podatke korisnika
            korisnikZaIzmenu.KorisnickoIme = izmenjeniKorisnik.KorisnickoIme;
            korisnikZaIzmenu.Ime = izmenjeniKorisnik.Ime;
            korisnikZaIzmenu.Prezime = izmenjeniKorisnik.Prezime;
            korisnikZaIzmenu.Email = izmenjeniKorisnik.Email;
            korisnikZaIzmenu.DatumRodjenja = izmenjeniKorisnik.DatumRodjenja;
            korisnikZaIzmenu.Pol = izmenjeniKorisnik.Pol;

            // Ako je lozinka prosleđena, ažuriraj je
            if (!string.IsNullOrEmpty(izmenjeniKorisnik.NovaLozinka))
            {
                korisnikZaIzmenu.Lozinka = izmenjeniKorisnik.NovaLozinka;
            }

            // Sačuvaj korisnike nazad u JSON fajl  
            SaveKorisnici(korisnici);

            // Resetuj sesiju korisnika
            HttpContext.Current.Session["Korisnik"] = korisnikZaIzmenu;

            return Ok("Uspešno ste izmenili profil.");
        }



        private List<Korisnik> LoadKorisnici()
        {
            if (!File.Exists(filePath))
            {
                return new List<Korisnik>();
            }

            var json = File.ReadAllText(filePath);
            return JsonConvert.DeserializeObject<List<Korisnik>>(json);
        }

        public void SaveKorisnici(List<Korisnik> korisnici)
        {
            var json = JsonConvert.SerializeObject(korisnici, Formatting.Indented);
            File.WriteAllText(filePath, json);
        }
    }


}
