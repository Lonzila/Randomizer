using ClosedXML.Excel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Randomizer.Data;
using Randomizer.Models;

public class DodeljevanjeController : Controller
{
    private readonly DodeljevanjeRecenzentovService _dodeljevanjeRecenzentovService;
    private readonly ApplicationDbContext _context;

     public DodeljevanjeController(DodeljevanjeRecenzentovService dodeljevanjeRecenzentovService, ApplicationDbContext context)
    {
        _dodeljevanjeRecenzentovService = dodeljevanjeRecenzentovService;
        _context = context;
    }

    public async Task<IActionResult> ExportToExcel()
    {
        // Najprej pridobimo vse prijave z njihovimi recenzenti
        var prijaveRecenzenti = await _context.Prijave
            .SelectMany(prijava => _context.GrozdiRecenzenti
                .Where(gr => gr.PrijavaID == prijava.PrijavaID)
                .Select(gr => new { prijava.PrijavaID, gr.RecenzentID }))
            .ToListAsync();

        // Nato ustvarimo slovar, ki prijavi priredi seznam šifer recenzentov
        var recenzentiPoPrijavah = prijaveRecenzenti
            .GroupBy(pr => pr.PrijavaID)
            .ToDictionary(
                g => g.Key,
                g => g.Select(pr => pr.RecenzentID.ToString()).Distinct().ToList()
            );


        // Nato pridobimo ostale podatke za prijave in povežemo informacije o recenzentih
        var results = await (from prijave in _context.Prijave
                          
                             join podpodrocje in _context.Podpodrocje on prijave.PodpodrocjeID equals podpodrocje.PodpodrocjeID
                            
                            
                             join dodatnoPodpodrocje in _context.Podpodrocje on prijave.DodatnoPodpodrocjeID equals dodatnoPodpodrocje.PodpodrocjeID into dpp
                             from dodatnoPodpodrocje in dpp.DefaultIfEmpty()
                            
                             select new
                             {
                                 prijave.StevilkaPrijave,
                                 prijave.Naslov,
                                 prijave.AngNaslov,
                                 prijave.VrstaProjekta,
                                 Interdisc = prijave.Interdisc != null && (bool)prijave.Interdisc ? "Da" : "Ne",
                                 prijave.Vodja,
                                 prijave.SifraVodje,
                                 prijave.NazivRO,
                                 prijave.SifraRO,
                                 PodpodrocjeKoda = podpodrocje.Koda,
                                 PodpodrocjeNaziv = podpodrocje.Naziv,
                                
                                 DodatnoPodpodrocjeKoda = dodatnoPodpodrocje != null ? dodatnoPodpodrocje.Koda : string.Empty,
                                 DodatnoPodpodrocjeNaziv = dodatnoPodpodrocje != null ? dodatnoPodpodrocje.Naziv : string.Empty,
                                 RecenzentiSifre = recenzentiPoPrijavah.ContainsKey(prijave.PrijavaID)
                                                ? recenzentiPoPrijavah[prijave.PrijavaID]
                                                : new List<string>(),
                             }).ToListAsync();

        using (var workbook = new XLWorkbook())
        {
            var worksheet = workbook.Worksheets.Add("Podatki");
            var currentRow = 1;

            // Nastavitev glave z novimi naslovi
            worksheet.Cell(currentRow, 1).Value = "Šifra prijave";
            worksheet.Cell(currentRow, 2).Value = "Naslov prijave (SL)";
            worksheet.Cell(currentRow, 3).Value = "Naslov prijave (EN)";
            worksheet.Cell(currentRow, 4).Value = "Vrsta prijave";
            worksheet.Cell(currentRow, 5).Value = "Interdisc";
            worksheet.Cell(currentRow, 6).Value = "Vodja";
            worksheet.Cell(currentRow, 7).Value = "Šifra vodje";
            worksheet.Cell(currentRow, 8).Value = "Raziskovalna organizacija (RO)";
            worksheet.Cell(currentRow, 9).Value = "Šifra RO";
            worksheet.Cell(currentRow, 10).Value = "Področje koda";
            worksheet.Cell(currentRow, 11).Value = "Področje naziv (SL)";
            worksheet.Cell(currentRow, 12).Value = "Področje naziv (EN)";
            worksheet.Cell(currentRow, 13).Value = "Podpodročje koda";
            worksheet.Cell(currentRow, 14).Value = "Podpodročje naziv";
            worksheet.Cell(currentRow, 15).Value = "Dodatno področje koda";
            worksheet.Cell(currentRow, 16).Value = "Dodatno področje naziv (SL)";
            worksheet.Cell(currentRow, 17).Value = "Dodatno področje naziv (EN)";
            worksheet.Cell(currentRow, 18).Value = "Dodatno podpodročje koda";
            worksheet.Cell(currentRow, 19).Value = "Dodatno podpodročje naziv";
            // Pripravimo štiri stolpce za šifre recenzentov
            for (int i = 1; i <= 4; i++)
            {
                worksheet.Cell(currentRow, 19 + i).Value = $"Recenzent šifra {i}";
            }

            // Dodajanje podatkov
            foreach (var item in results)
            {
                currentRow++;
                worksheet.Cell(currentRow, 1).Value = item.StevilkaPrijave;
                worksheet.Cell(currentRow, 2).Value = item.Naslov;
                worksheet.Cell(currentRow, 3).Value = item.AngNaslov;
                worksheet.Cell(currentRow, 4).Value = item.VrstaProjekta;
                worksheet.Cell(currentRow, 5).Value = item.Interdisc;
                worksheet.Cell(currentRow, 6).Value = item.Vodja;
                worksheet.Cell(currentRow, 7).Value = item.SifraVodje;
                worksheet.Cell(currentRow, 8).Value = item.NazivRO;
                worksheet.Cell(currentRow, 9).Value = item.SifraRO;
             
                worksheet.Cell(currentRow, 13).Value = item.PodpodrocjeKoda;
                worksheet.Cell(currentRow, 14).Value = item.PodpodrocjeNaziv;
             
                worksheet.Cell(currentRow, 18).Value = item.DodatnoPodpodrocjeKoda;
                worksheet.Cell(currentRow, 19).Value = item.DodatnoPodpodrocjeNaziv;

                // Dodajanje šifer recenzentov
                for (int i = 0; i < 4; i++)
                {
                    worksheet.Cell(currentRow, 20 + i).Value = item.RecenzentiSifre.Count > i ? item.RecenzentiSifre[i] : string.Empty;
                }
            }
            using (var stream = new MemoryStream())
            {
                workbook.SaveAs(stream);
                var content = stream.ToArray();

                return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "PrijaveInRecenzenti.xlsx");
            }
        }
    }
   
    public async Task<IActionResult> DodeliRecenzente()
    {
        await _dodeljevanjeRecenzentovService.DodeliRecenzenteAsync();
        return Ok("Recenzenti so bili dodeljeni.");
    }
    public async Task<IActionResult> PrikazGrozdov()
    {
        var grozdi = await _dodeljevanjeRecenzentovService.PridobiInformacijeZaIzpisAsync();
        return View(grozdi);
    }


    public async Task<IActionResult> ExportToExcel2()
    {
        var grozdiViewModels = await _dodeljevanjeRecenzentovService.PridobiInformacijeZaIzpisAsync();
        var timestamp = DateTime.Now;
        var userIpAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        using (var workbook = new XLWorkbook())
        {
            var worksheet = workbook.Worksheets.Add("Dodelitve Recenzentov");
            // Dodajanje loga v glavo
            worksheet.Cell("A1").Value = $"IP naslov uporabnika: {userIpAddress}";
            worksheet.Cell("A2").Value = $"Datum in čas izvedbe: {timestamp}";

            // Nastavitev začetne vrstice za podatke
            var currentRow = 4; // Začnemo pri vrstici 4, da pustimo prostor za log
            // Nastavitev glave
            
            worksheet.Cell(currentRow, 1).Value = "Številka prijave";
            worksheet.Cell(currentRow, 2).Value = "Naslov";
            worksheet.Cell(currentRow, 3).Value = "Interdisciplinarna";
            worksheet.Cell(currentRow, 4).Value = "Število recenzentov";
            worksheet.Cell(currentRow, 5).Value = "Podpodročje";
            worksheet.Cell(currentRow, 6).Value = "Dodatno podpodročje";
            worksheet.Cell(currentRow, 7).Value = "Recenzenti";

            // Dodajanje podatkov
            foreach (var grozd in grozdiViewModels)
            {
                foreach (var prijava in grozd.Prijave)
                {
                    currentRow++;
                    worksheet.Cell(currentRow, 1).Value = prijava.StevilkaPrijave;
                    worksheet.Cell(currentRow, 2).Value = prijava.Naslov;
                    worksheet.Cell(currentRow, 3).Value = prijava.Interdisc ? "Da" : "Ne";
                    worksheet.Cell(currentRow, 4).Value = prijava.SteviloRecenzentov;
                    worksheet.Cell(currentRow, 5).Value = prijava.Podpodrocje;
                    worksheet.Cell(currentRow, 6).Value = prijava.DodatnoPodpodrocje;
                    worksheet.Cell(currentRow, 7).Value = string.Join(", ", prijava.Recenzenti.Select(r => r.Ime + " " + r.Priimek + " (" + r.Vloga + ")"));
                }
            }

            using (var stream = new MemoryStream())
            {
                workbook.SaveAs(stream);
                var content = stream.ToArray();

                return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "DodelitveRecenzentov.xlsx");
            }
        }
    }

}
