using Microsoft.AspNetCore.Mvc;
using Randomizer.Models;
using System.Diagnostics;

namespace Randomizer.Controllers
{
    public class HomeController : Controller
    {
        private readonly DodeljevanjeRecenzentovService _dodeljevanjeRecenzentovService;

        public HomeController(DodeljevanjeRecenzentovService dodeljevanjeRecenzentovService)
        {
            _dodeljevanjeRecenzentovService = dodeljevanjeRecenzentovService;
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
    }
}
