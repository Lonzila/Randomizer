using Microsoft.AspNetCore.Mvc;
using Randomizer.Models;
using Randomizer.Services;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Randomizer.Controllers
{
    public class HomeController : Controller
    {
        private readonly DodeljevanjeRecenzentovService _dodeljevanjeRecenzentovService;
        private readonly RecenzentZavrnitveService _recenzentZavrnitveService;
        private readonly GrozdiRecenzentZavrnitveService _grozdiRecenzentZavrnitveService;
        private readonly TretjiRecenzentService _tretjiRecenzentService;

        public HomeController(DodeljevanjeRecenzentovService dodeljevanjeRecenzentovService, RecenzentZavrnitveService recenzentZavrnitveService, GrozdiRecenzentZavrnitveService grozdiRecenzentZavrnitveService, TretjiRecenzentService tretjiRecenzentService)
        {
            _dodeljevanjeRecenzentovService = dodeljevanjeRecenzentovService;
            _recenzentZavrnitveService = recenzentZavrnitveService;
            _grozdiRecenzentZavrnitveService = grozdiRecenzentZavrnitveService;
            _tretjiRecenzentService = tretjiRecenzentService;
        }

        public IActionResult Index()
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

        public async Task<IActionResult> ObdelajZavrnitve()
        {
            await _recenzentZavrnitveService.ObdelajZavrnitveInDodeliNoveRecenzenteAsync();
            return RedirectToAction("PrikazPosodobljenihRecenzentov");
        }

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
    }
}
