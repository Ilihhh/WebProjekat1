using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web.Hosting;
using System.Web.Http;
using WebProjekat.Models;

namespace WebProjekat.Controllers
{
    [RoutePrefix("api/Aviokompanije")]
    public class AviokompanijeController : ApiController
    {
        private static readonly string FilePath = HostingEnvironment.MapPath("~/App_Data/aviokompanije.json");

        // GET: api/Aviokompanije
        [HttpGet]
        [Route("")]
        public IHttpActionResult Get()
        {
            try
            {
                var aviokompanije = LoadAviokompanije().Where(a => !a.IsRemoved).ToList();
                return Ok(aviokompanije);
            }
            catch (Exception ex)
            {
                // Log the exception
                System.Diagnostics.Trace.TraceError("Error in Get method: " + ex.Message + "\n" + ex.StackTrace);
                return InternalServerError(ex);
            }
        }

        // POST: api/Aviokompanije/Edit
        [HttpPost]
        [Route("Edit")]
        public IHttpActionResult Edit([FromBody] EditedAviokompanijaModel editedAviokompanija)
        {
            try
            {
                var aviokompanije = LoadAviokompanije();

                // Pronalazimo aviokompaniju koju želimo da izmenimo
                var aviokompanija = aviokompanije.FirstOrDefault(a => a.Naziv.Equals(editedAviokompanija.StariNaziv, StringComparison.OrdinalIgnoreCase));

                if (aviokompanija == null)
                {
                    return NotFound();
                }

                // Provera da li novi naziv vec postoji, osim ako stari i novi naziv nisu isti
                if (!editedAviokompanija.StariNaziv.Equals(editedAviokompanija.NoviNaziv, StringComparison.OrdinalIgnoreCase) &&
                    aviokompanije.Any(a => a.Naziv.Equals(editedAviokompanija.NoviNaziv, StringComparison.OrdinalIgnoreCase)))
                {
                    return BadRequest("Aviokompanija sa unetim novim nazivom već postoji.");
                }

                // Ažuriranje naziva u aviokompaniji
                aviokompanija.Naziv = editedAviokompanija.NoviNaziv;
                aviokompanija.Adresa = editedAviokompanija.NovaAdresa;
                aviokompanija.KontaktInformacije = editedAviokompanija.NoveKontaktInformacije;

                // Ažuriranje naziva aviokompanije u svim letovima koji pripadaju toj aviokompaniji
                foreach (var let in aviokompanija.ListaLetova)
                {
                    let.Aviokompanija = editedAviokompanija.NoviNaziv;
                }

                SaveAviokompanije(aviokompanije);

                return Ok();
            }
            catch (Exception ex)
            {
                // Log the exception
                System.Diagnostics.Trace.TraceError("Error in Edit method: " + ex.Message + "\n" + ex.StackTrace);
                return InternalServerError(ex);
            }
        }




        // GET: api/Aviokompanije/Naziv
        [HttpGet]
        [Route("{naziv}")]
        public IHttpActionResult GetByNaziv(string naziv)
        {
            try
            {
                var aviokompanije = LoadAviokompanije().Where(a => !a.IsRemoved).ToList();
                var aviokompanija = aviokompanije.FirstOrDefault(a => a.Naziv.Equals(naziv, StringComparison.OrdinalIgnoreCase));

                if (aviokompanija == null)
                {
                    return NotFound();
                }

                return Ok(aviokompanija);
            }
            catch (Exception ex)
            {
                // Log the exception
                System.Diagnostics.Trace.TraceError("Error in GetByNaziv method: " + ex.Message + "\n" + ex.StackTrace);
                return InternalServerError(ex);
            }
        }

        // DELETE: api/Aviokompanije/{naziv}
        [HttpDelete]
        [Route("{naziv}")]
        public IHttpActionResult Delete(string naziv)
        {
            try
            {
                var aviokompanije = LoadAviokompanije();
                var aviokompanija = aviokompanije.FirstOrDefault(a => a.Naziv.Equals(naziv, StringComparison.OrdinalIgnoreCase));

                if (aviokompanija == null)
                {
                    return NotFound();
                }

                if (aviokompanija.ListaLetova.Any(let => let.Status == 0))
                {
                    return BadRequest("Aviokompanija ima aktivne letove i ne može biti obrisana.");
                }

                aviokompanija.IsRemoved = true;
                SaveAviokompanije(aviokompanije);

                return Ok();
            }
            catch (Exception ex)
            {
                // Log the exception
                System.Diagnostics.Trace.TraceError("Error in Delete method: " + ex.Message + "\n" + ex.StackTrace);
                return InternalServerError(ex);
            }
        }

        // POST: api/Aviokompanije/Add
        [HttpPost]
        [Route("Add")]
        public IHttpActionResult Add([FromBody] Aviokompanija novaAviokompanija)
        {
            try
            {
                var aviokompanije = LoadAviokompanije();

                // Provera da li već postoji aviokompanija sa istim nazivom
                if (aviokompanije.Any(a => a.Naziv.Equals(novaAviokompanija.Naziv, StringComparison.OrdinalIgnoreCase)))
                {
                    return BadRequest("Aviokompanija sa unetim nazivom već postoji.");
                }

                // Dodavanje nove aviokompanije
                aviokompanije.Add(novaAviokompanija);
                SaveAviokompanije(aviokompanije);

                return Ok();
            }
            catch (Exception ex)
            {
                // Log the exception
                System.Diagnostics.Trace.TraceError("Error in Add method: " + ex.Message + "\n" + ex.StackTrace);
                return InternalServerError(ex);
            }
        }



        private List<Aviokompanija> LoadAviokompanije()
        {
            try
            {
                if (!File.Exists(FilePath))
                {
                    return new List<Aviokompanija>();
                }

                var json = File.ReadAllText(FilePath);
                return JsonConvert.DeserializeObject<List<Aviokompanija>>(json) ?? new List<Aviokompanija>();
            }
            catch (Exception ex)
            {
                // Log the exception
                System.Diagnostics.Trace.TraceError("Error loading airlines: " + ex.Message + "\n" + ex.StackTrace);
                throw;
            }
        }

        private void SaveAviokompanije(List<Aviokompanija> aviokompanije)
        {
            try
            {
                var json = JsonConvert.SerializeObject(aviokompanije, Formatting.Indented);
                File.WriteAllText(FilePath, json);
            }
            catch (Exception ex)
            {
                // Log the exception
                System.Diagnostics.Trace.TraceError("Error saving airlines: " + ex.Message + "\n" + ex.StackTrace);
                throw;
            }
        }
    }
}
