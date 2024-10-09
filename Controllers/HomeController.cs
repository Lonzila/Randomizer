using Microsoft.AspNetCore.Mvc;
using Randomizer.Models;
using Randomizer.Services;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Randomizer.Data;
using Microsoft.EntityFrameworkCore;
using ClosedXML.Excel;
using Randomizer.Models.Randomizer.Models;

namespace Randomizer.Controllers
{
    public class HomeController : Controller
    {
        private readonly DodeljevanjeRecenzentovService _dodeljevanjeRecenzentovService;
        private readonly RecenzentZavrnitveService _recenzentZavrnitveService;
        private readonly GrozdiRecenzentZavrnitveService _grozdiRecenzentZavrnitveService;
        private readonly TretjiRecenzentService _tretjiRecenzentService;
        private readonly ExcelService _excelService;
        private readonly ApplicationDbContext _context;

        public HomeController(DodeljevanjeRecenzentovService dodeljevanjeRecenzentovService, RecenzentZavrnitveService recenzentZavrnitveService, GrozdiRecenzentZavrnitveService grozdiRecenzentZavrnitveService, TretjiRecenzentService tretjiRecenzentService, ExcelService excelService, ApplicationDbContext context)
        {
            _dodeljevanjeRecenzentovService = dodeljevanjeRecenzentovService;
            _recenzentZavrnitveService = recenzentZavrnitveService;
            _grozdiRecenzentZavrnitveService = grozdiRecenzentZavrnitveService;
            _tretjiRecenzentService = tretjiRecenzentService;
            _excelService = excelService;
            _context = context;
        }

        public IActionResult Index()
        {
            return View();
        }

     
        public async Task<IActionResult> UpravljanjeRecenzentov()
        {
            return View();
        }

        public async Task<IActionResult> PocistiDodelitve()
        {
            await _dodeljevanjeRecenzentovService.PocistiDodelitveRecenzentovAsync();
            return RedirectToAction("Index");
        }

        public IActionResult VnosZavrnitve()
        {
            return View();
        }
        public IActionResult PrimerjajRecenzente()
        {
            return View();
        }
        public IActionResult Iskanje()
        {
            return View();
        }
        [HttpGet]
        public async Task<IActionResult> PrikazGrozdov()
        {
            var grozdiViewModels = await _dodeljevanjeRecenzentovService.PridobiInformacijeZaIzpisAsync();
            return View(grozdiViewModels);
        }
        // Prikaz posodobljenih recenzentov po zavrnitvah (metoda ostane nespremenjena)
        public async Task<IActionResult> PrikazPosodobljenihRecenzentov()
        {
            var grozdiViewModels = await _dodeljevanjeRecenzentovService.PridobiInformacijeZaIzpisAsync();
            return View("~/Views/Home/PrikazGrozdov.cshtml", grozdiViewModels);
        }

        // Metoda za obdelavo zavrnitev grozda
        public async Task<IActionResult> ObdelajZavrnitveGrozda()
        {
            await _grozdiRecenzentZavrnitveService.ObdelajZavrnitveInDodeliNoveRecenzenteAsync();
            return RedirectToAction("PrikazPosodobljenihRecenzentov");
        }

        // Dodelitev tretjega recenzenta (dodano ustrezno preusmerjanje)
        [HttpPost]
        public async Task<IActionResult> DodeliTretjegaRecenzenta(string prijavaIDs)
        {
            if (string.IsNullOrEmpty(prijavaIDs))
            {
                ViewBag.Message = "ID-ji prijav niso bili podani.";
                return View("Index");
            }

            var prijavaIDsList = prijavaIDs.Split(',')
                                           .Select(id => int.TryParse(id.Trim(), out var parsedId) ? parsedId : (int?)null)
                                           .Where(id => id.HasValue)
                                           .Select(id => id.Value)
                                           .ToList();

            if (!prijavaIDsList.Any())
            {
                ViewBag.Message = "Noben veljaven ID prijave ni bil podan.";
                return View("Index");
            }

            await _tretjiRecenzentService.DodeliTretjegaRecenzentaAsync(prijavaIDsList);
            return RedirectToAction("PrikazPosodobljenihRecenzentov");
        }

        // Metoda za iskanje recenzenta (POST)
        [HttpPost]
        public async Task<IActionResult> IskanjeRecenzenta(string sifraRecenzenta)
        {
            if (!int.TryParse(sifraRecenzenta, out int sifraRecenzentaInt))
            {
                ViewBag.Message = "Neveljavna šifra recenzenta.";
                return View("Iskanje");
            }

            var recenzent = await _context.Recenzenti
            .Where(r => r.Sifra == sifraRecenzentaInt)
            .Select(r => new { r.RecenzentID, r.Sifra })
            .FirstOrDefaultAsync();

            if (recenzent == null)
            {
                ViewBag.Message = "Recenzent ni bil najden.";
                return View("Iskanje");
            }

            ViewBag.RecenzentID = recenzent.RecenzentID;
            return View("Iskanje");
        }

        // Metoda za iskanje prijave (POST)
        [HttpPost]
        public async Task<IActionResult> IskanjePrijave(string stevilkaPrijave)
        {
            if (!int.TryParse(stevilkaPrijave, out int stevilkaPrijaveInt))
            {
                ViewBag.Message = "Neveljavna številka prijave.";
                return View("Iskanje");
            }

            var prijava = await _context.Prijave
                .Where(p => p.StevilkaPrijave == stevilkaPrijaveInt)
                .Select(p => new { p.PrijavaID, p.StevilkaPrijave })
                .FirstOrDefaultAsync();

            if (prijava == null)
            {
                ViewBag.Message = "Prijava ni bila najdena.";
                return View("Iskanje");
            }

            ViewBag.PrijavaID = prijava.PrijavaID;
            return View("Iskanje");
        }

        // Metoda za prikaz števila prijav na recenzenta
        public async Task<IActionResult> PrikazSteviloPrijav()
        {
            var rezultati = await _context.GrozdiRecenzenti
                .Join(_context.Recenzenti, gr => gr.RecenzentID, r => r.RecenzentID, (gr, r) => new { gr, r })
                .Join(_context.Prijave, grr => grr.gr.PrijavaID, p => p.PrijavaID, (grr, p) => new { grr.gr, grr.r, p })
                .GroupBy(g => new { g.r.RecenzentID, g.r.Sifra })
                .Select(g => new
                {
                    RecenzentID = g.Key.RecenzentID,
                    Sifra = g.Key.Sifra,
                    SteviloPrijav = g.Select(x => x.gr.PrijavaID).Distinct().Count()
                })
                .OrderByDescending(x => x.SteviloPrijav)
                .ToListAsync();

            return View(rezultati);
        }

        // Metoda za vnos zavrnitve
        [HttpPost]
        public async Task<IActionResult> VnosZavrnitve(string sifraRecenzenta, string stevilkaPrijave, string razlogZavrnitve)
        {
            if (!int.TryParse(sifraRecenzenta, out int sifraRecenzentaInt))
            {
                ViewBag.Message = "Neveljavna šifra recenzenta.";
                return View();
            }

            if (!int.TryParse(stevilkaPrijave, out int stevilkaPrijaveInt))
            {
                ViewBag.Message = "Neveljavna številka prijave.";
                return View();
            }

            var recenzent = await _context.Recenzenti
                .Where(r => r.Sifra == sifraRecenzentaInt)
                .Select(r => new { r.RecenzentID, r.Sifra })
                .FirstOrDefaultAsync();

            if (recenzent == null)
            {
                ViewBag.Message = "Recenzent ni bil najden.";
                return View();
            }

            var prijava = await _context.Prijave
                .Where(p => p.StevilkaPrijave == stevilkaPrijaveInt).Select(p => new { p.PrijavaID, p.StevilkaPrijave }).FirstOrDefaultAsync();
            if (prijava == null)
            {
                ViewBag.Message = "Prijava ni bila najdena.";
                return View();
            }

            var grozdRecenzent = await _context.GrozdiRecenzenti
                .Where(gr => gr.RecenzentID == recenzent.RecenzentID && gr.PrijavaID == prijava.PrijavaID)
                .Select(gr => new { gr.GrozdID, gr.PrijavaID, gr.RecenzentID })
                .FirstOrDefaultAsync();

            if (grozdRecenzent == null)
            {
                ViewBag.Message = "Grozd za ta par recenzenta in prijave ni bil najden.";
                return View();
            }

            var zavrnitev = new RecenzentiZavrnitve
            {
                RecenzentID = grozdRecenzent.RecenzentID,
                PrijavaID = grozdRecenzent.PrijavaID,
                GrozdID = grozdRecenzent.GrozdID,
                Razlog = razlogZavrnitve
            };

            _context.RecenzentiZavrnitve.Add(zavrnitev);
            await _context.SaveChangesAsync();

            ViewBag.Message = "Zavrnitev uspešno dodana.";
            return View();
        }

        // Metoda za nalaganje in primerjavo Excel datotek
        [HttpPost]
        public async Task<IActionResult> PrimerjajRecenzente(IFormFile excelFile)
        {
            if (excelFile == null || excelFile.Length == 0)
            {
                ViewBag.Message = "Prosimo, naložite veljavno Excel datoteko.";
                return View("PrimerjajRecenzente");
            }

            // Ustvari unikatno ime za datoteko z ustrezno pripono .xlsx
            var filePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".xlsx");

            // Shrani datoteko v zaèasno mapo
            using (var stream = System.IO.File.Create(filePath))
            {
                await excelFile.CopyToAsync(stream);
            }

            try
            {
                // Pridobi podatke iz naložene Excel datoteke
                var stanjeNaDF = await _excelService.PridobiPodatkeIzExcela(filePath);

                // Pridobi podatke iz baze
                var grozdiRecenzenti = await _context.GrozdiRecenzenti
                    .Include(gr => gr.Recenzent)
                    .Include(gr => gr.Prijava)
                    .ToListAsync();

                // Seznam za hranjenje primerjav
                var primerjave = new List<(int StevilkaPrijave, int SifraRecenzentaBaza, int? SifraRecenzentaExcel)>();

                foreach (var grozdRecenzent in grozdiRecenzenti)
                {
                    var prijavaExcel = stanjeNaDF.FirstOrDefault(s =>
                           s.Prijava == grozdRecenzent.Prijava.StevilkaPrijave &&
                           s.ID == grozdRecenzent.Recenzent.Sifra);

                    if (prijavaExcel == null)
                    {
                        primerjave.Add((grozdRecenzent.Prijava.StevilkaPrijave, grozdRecenzent.Recenzent.Sifra, null));
                    }
                }

                ViewBag.Primerjave = primerjave;
            }
            catch (Exception ex)
            {
                ViewBag.Message = $"Napaka pri obdelavi datoteke: {ex.Message}";
            }
            finally
            {
                // Po obdelavi izbriši zaèasno datoteko
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }
            }

            return View("PrimerjajRecenzente");
        }
    }
}