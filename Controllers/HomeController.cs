using Microsoft.AspNetCore.Mvc;
using Randomizer.Models;
using System.Diagnostics;
using Randomizer.Services;

namespace Randomizer.Controllers
{
    public class HomeController : Controller
    {
        private readonly DodeljevanjeRecenzentovService _dodeljevanjeRecenzentovService;
        private readonly RecenzentZavrnitveService _recenzentZavrnitveService;
        private readonly GrozdiRecenzentZavrnitveService _grozdiRecenzentZavrnitveService;
        
        public HomeController(DodeljevanjeRecenzentovService dodeljevanjeRecenzentovService, RecenzentZavrnitveService recenzentZavrnitveService, GrozdiRecenzentZavrnitveService grozdiRecenzentZavrnitveService)
        {
            _dodeljevanjeRecenzentovService = dodeljevanjeRecenzentovService;
            _recenzentZavrnitveService = recenzentZavrnitveService;
            _grozdiRecenzentZavrnitveService = grozdiRecenzentZavrnitveService;
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
            return RedirectToAction("Index"); // Ali kamor koli že želite preusmeriti uporabnika po èišèenju
        }
        public async Task<IActionResult> PrikazPosodobljenihRecenzentov()
        {
            var grozdiViewModels = await _dodeljevanjeRecenzentovService.PridobiInformacijeZaIzpisAsync();
            return View("~/Views/Dodeljevanje/PrikazGrozdov.cshtml", grozdiViewModels); // Uporabite natanèno pot do pogleda
        }
        public async Task<IActionResult> ObdelajZavrnitve()
        {
            await _recenzentZavrnitveService.ObdelajZavrnitveInDodeliNoveRecenzenteAsync();
            return RedirectToAction("PrikazPosodobljenihRecenzentov"); // Preusmeritev na novo akcijo
        }

        public async Task<IActionResult> ObdelajZavrnitveGrozda()
        {
            await _grozdiRecenzentZavrnitveService.ObdelajZavrnitveInDodeliNoveRecenzenteAsync2();
            return RedirectToAction("PrikazPosodobljenihRecenzentov"); // Preusmeritev na novo akcijo
        }
    }
}
