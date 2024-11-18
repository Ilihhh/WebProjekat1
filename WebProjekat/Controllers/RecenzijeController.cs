using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using System.Web.Hosting;
using System.Web.Http;
using Newtonsoft.Json;
using WebProjekat.Models;

namespace WebProjekat.Controllers
{
    public class RecenzijeController : ApiController
    {
        private static readonly string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "App_Data", "aviokompanije.json");

        [HttpPost]
        [Route("api/Recenzije/Ostavi")]
        public async Task<IHttpActionResult> PostRecenzija()
        {
            if (!Request.Content.IsMimeMultipartContent())
            {
                throw new HttpResponseException(HttpStatusCode.UnsupportedMediaType);
            }

            string uploadFolderPath = HostingEnvironment.MapPath("~/Images");
            var provider = new MultipartFormDataStreamProvider(uploadFolderPath);

            try
            {
                await Request.Content.ReadAsMultipartAsync(provider);

                var naslov = provider.FormData["naslov"];
                var sadrzaj = provider.FormData["sadrzaj"];
                var aviokompanija = provider.FormData["aviokompanija"];
                var recezent = provider.FormData["recezent"];
                string slikaPath = null;

                if (provider.FileData.Count > 0)
                {
                    foreach (var file in provider.FileData)
                    {
                        string originalFileName = file.Headers.ContentDisposition.FileName.Trim('"');
                        string uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(originalFileName);
                        string filePath = Path.Combine(uploadFolderPath, uniqueFileName);
                        File.Move(file.LocalFileName, filePath);
                        slikaPath = "/Images/" + uniqueFileName;
                    }
                }

                string jsonFilePath = HostingEnvironment.MapPath("~/App_Data/Aviokompanije.json");
                List<Aviokompanija> aviokompanije = new List<Aviokompanija>();

                if (File.Exists(jsonFilePath))
                {
                    string json = File.ReadAllText(jsonFilePath);
                    aviokompanije = JsonConvert.DeserializeObject<List<Aviokompanija>>(json);
                }

                var kompanija = aviokompanije.FirstOrDefault(a => a.Naziv == aviokompanija);
                if (kompanija != null)
                {
                    // Check if the user has already reviewed this airline
                    var existingReview = kompanija.ListaRecenzija.FirstOrDefault(r => r.Recenzent == recezent);

                    if (existingReview != null)
                    {
                        // Update the existing review
                        existingReview.Naslov = naslov;
                        existingReview.Sadrzaj = sadrzaj;
                        existingReview.Slika = slikaPath;
                        existingReview.Status = StatusRecenzije.Kreirana;
                    }
                    else
                    {
                        // Add the new review
                        var novaRecenzija = new Recenzija
                        {
                            Recenzent = recezent,
                            Aviokompanija = aviokompanija,
                            Naslov = naslov,
                            Sadrzaj = sadrzaj,
                            Slika = slikaPath,
                            Status = StatusRecenzije.Kreirana
                        };

                        kompanija.ListaRecenzija.Add(novaRecenzija);
                    }
                }

                string updatedJson = JsonConvert.SerializeObject(aviokompanije, Formatting.Indented);
                File.WriteAllText(jsonFilePath, updatedJson);

                return Ok("Recenzija successfully received and processed.");
            }
            catch (Exception e)
            {
                return InternalServerError(e);
            }
        }

        [HttpPost]
        [Route("api/Recenzije/UpdateStatus")]
        public IHttpActionResult UpdateStatus(ReviewStatusUpdateRequest request)
        {
            var korisnik = CheckSession();
            if (korisnik == null || korisnik.Tip != TipKorisnika.Administrator)
            {
                return Unauthorized();
            }

            var aviokompanije = LoadAviokompanije();
            foreach (var aviokompanija in aviokompanije)
            {
                var recenzija = aviokompanija.ListaRecenzija.FirstOrDefault(r => r.Recenzent == request.Recenzent && r.Aviokompanija == request.Aviokompanija);
                if (recenzija != null)
                {
                    recenzija.Status = request.Status;
                    SaveAviokompanije(aviokompanije);
                    return Ok();
                }
            }

            return NotFound();
        }

        private List<Aviokompanija> LoadAviokompanije()
        {
            if (File.Exists(filePath))
            {
                var jsonData = File.ReadAllText(filePath);
                return JsonConvert.DeserializeObject<List<Aviokompanija>>(jsonData);
            }
            return new List<Aviokompanija>();
        }

        private void SaveAviokompanije(List<Aviokompanija> aviokompanije)
        {
            var jsonData = JsonConvert.SerializeObject(aviokompanije, Formatting.Indented);
            File.WriteAllText(filePath, jsonData);
        }

        public class ReviewStatusUpdateRequest
        {
            public string Recenzent { get; set; }
            public string Aviokompanija { get; set; }
            public StatusRecenzije Status { get; set; }
        }

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
