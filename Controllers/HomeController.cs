using Microsoft.AspNetCore.Mvc;
using Randomizer.Models;
using Randomizer.Services;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Randomizer.Data;
using DocumentFormat.OpenXml.InkML;
using Microsoft.EntityFrameworkCore;

namespace Randomizer.Controllers
{
    public class HomeController : Controller
    {
        private readonly DodeljevanjeRecenzentovService _dodeljevanjeRecenzentovService;
        private readonly RecenzentZavrnitveService _recenzentZavrnitveService;
        private readonly GrozdiRecenzentZavrnitveService _grozdiRecenzentZavrnitveService;
        private readonly TretjiRecenzentService _tretjiRecenzentService;
        private readonly ApplicationDbContext _context;

        public HomeController(DodeljevanjeRecenzentovService dodeljevanjeRecenzentovService, RecenzentZavrnitveService recenzentZavrnitveService, GrozdiRecenzentZavrnitveService grozdiRecenzentZavrnitveService, TretjiRecenzentService tretjiRecenzentService, ApplicationDbContext context)
        {
            _dodeljevanjeRecenzentovService = dodeljevanjeRecenzentovService;
            _recenzentZavrnitveService = recenzentZavrnitveService;
            _grozdiRecenzentZavrnitveService = grozdiRecenzentZavrnitveService;
            _tretjiRecenzentService = tretjiRecenzentService;
            _context = context;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult VnosZavrnitve()
        {
            return View();
        }

        public async Task<IActionResult> DodeliRecenzente()
        {
            await _dodeljevanjeRecenzentovService.DodeliRecenzenteAsync();
            return RedirectToAction("PrikazGrozdov", "Dodeljevanje");
        }

        public async Task<IActionResult> PocistiDodelitve()
        {
            await _dodeljevanjeRecenzentovService.PocistiDodelitveRecenzentovAsync();
            return RedirectToAction("Index");
        }

        public async Task<IActionResult> PrikazPosodobljenihRecenzentov()
        {
            var grozdiViewModels = await _dodeljevanjeRecenzentovService.PridobiInformacijeZaIzpisAsync();
            return View("~/Views/Dodeljevanje/PrikazGrozdov.cshtml", grozdiViewModels);
        }

        /*public async Task<IActionResult> ObdelajZavrnitve()
        {
            await _recenzentZavrnitveService.ObdelajZavrnitveInDodeliNoveRecenzenteAsync();
            return RedirectToAction("PrikazPosodobljenihRecenzentov");
        }*/

        public async Task<IActionResult> ObdelajZavrnitveGrozda()
        {
            await _grozdiRecenzentZavrnitveService.ObdelajZavrnitveInDodeliNoveRecenzenteAsync2();
            return RedirectToAction("PrikazPosodobljenihRecenzentov");
        }

        [HttpPost]
        public async Task<IActionResult> DodeliTretjegaRecenzenta(string prijavaIDs)
        {
            if (string.IsNullOrEmpty(prijavaIDs))
            {
                return RedirectToAction("Index");
            }

            var prijavaIDsList = prijavaIDs.Split(',')
                                           .Select(id => int.TryParse(id.Trim(), out var parsedId) ? parsedId : (int?)null)
                                           .Where(id => id.HasValue)
                                           .Select(id => id.Value)
                                           .ToList();

            if (!prijavaIDsList.Any())
            {
                return RedirectToAction("Index");
            }

            await _tretjiRecenzentService.DodeliTretjegaRecenzentaAsync(prijavaIDsList);
            return RedirectToAction("PrikazPosodobljenihRecenzentov");
        }

        [HttpPost]
        public async Task<IActionResult> IskanjeRecenzenta(string sifraRecenzenta)
        {
            if (!int.TryParse(sifraRecenzenta, out int sifraRecenzentaInt))
            {
                ViewBag.Message = "Neveljavna šifra recenzenta.";
                return View("Index");
            }

            var recenzent = await _context.Recenzenti
            .Where(r => r.Sifra == sifraRecenzentaInt)
            .Select(r => new { r.RecenzentID, r.Sifra })
            .FirstOrDefaultAsync();


            if (recenzent == null)
            {
                ViewBag.Message = "Recenzent ni bil najden.";
                return View("Index");
            }

            ViewBag.RecenzentID = recenzent.RecenzentID;
            return View("Index");
        }

        [HttpPost]
        public async Task<IActionResult> IskanjePrijave(string stevilkaPrijave)
        {
            if (!int.TryParse(stevilkaPrijave, out int stevilkaPrijaveInt))
            {
                ViewBag.Message = "Neveljavna številka prijave.";
                return View("Index");
            }

            var prijava = await _context.Prijave
                .Where(p => p.StevilkaPrijave == stevilkaPrijaveInt)
                .Select(p => new { p.PrijavaID, p.StevilkaPrijave })
                .FirstOrDefaultAsync();

            if (prijava == null)
            {
                ViewBag.Message = "Prijava ni bila najdena.";
                return View("Index");
            }

            ViewBag.PrijavaID = prijava.PrijavaID;
            return View("Index");
        }

        public async Task<IActionResult> PrikaziSteviloPrijav()
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
                .Where(p => p.StevilkaPrijave == stevilkaPrijaveInt)
                .Select(p => new { p.PrijavaID, p.StevilkaPrijave })
                .FirstOrDefaultAsync();

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
    }
}
