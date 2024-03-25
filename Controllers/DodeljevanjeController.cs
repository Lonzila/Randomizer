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
                .Join(_context.Recenzenti, gr => gr.RecenzentID, recenzent => recenzent.RecenzentID,
                    (gr, recenzent) => new { prijava.PrijavaID, recenzent.Sifra }))
            .ToListAsync();

        // Nato ustvarimo slovar, ki prijavi priredi seznam šifer recenzentov
        var recenzentiPoPrijavah = prijaveRecenzenti
            .GroupBy(pr => pr.PrijavaID)
            .ToDictionary(
                g => g.Key,
                g => g.Select(pr => pr.Sifra).Distinct().ToList()
            );
        var porocevalciIdsInSifre = await _context.GrozdiRecenzenti
            .Where(gr => gr.Vloga == "Poročevalec")
            .Select(gr => new { gr.RecenzentID, gr.PrijavaID })
            .Join(_context.Recenzenti,
                  gr => gr.RecenzentID,
                  r => r.RecenzentID,
                  (gr, r) => new { gr.PrijavaID, r.Sifra })
            .ToDictionaryAsync(
                x => x.PrijavaID,
                x => x.Sifra
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
                                                : new List<int>(),
                                 PorocevalecSifra = porocevalciIdsInSifre.ContainsKey(prijave.PrijavaID)
                                            ? porocevalciIdsInSifre[prijave.PrijavaID]
                                            : 0
                             }).ToListAsync();

        using (var workbook = new XLWorkbook())
        {
            var worksheet = workbook.Worksheets.Add("Podatki");
            int currentRow = 1; // Začnemo v prvi vrstici

            // Nastavitev glave z novimi naslovi
            worksheet.Cell(currentRow, 1).Value = "Naziv";
            worksheet.Cell(currentRow, 2).Value = "Datum začetka ocenjevanja";
            worksheet.Cell(currentRow, 3).Value = "Datum konca ocenjevanja";
            worksheet.Cell(currentRow, 4).Value = "Datum začetka, ko lahko vodje pregledajo skupne ocene";
            worksheet.Cell(currentRow, 5).Value = "Datum konca, ko lahko vodje pregledajo skupno oceno";
            worksheet.Cell(currentRow, 6).Value = "Seznam skrbnikov za razpis";
            worksheet.Cell(currentRow, 7).Value = "Centralni e-mail za obveščanje";
            worksheet.Cell(currentRow, 8).Value = "Prijavna številka (Application number)";
            worksheet.Cell(currentRow, 9).Value = "Tip raziskovalnega projekta";
            worksheet.Cell(currentRow, 10).Value = "ProjektTipOpisAng";
            worksheet.Cell(currentRow, 11).Value = "ProjektNaslovSlo";
            worksheet.Cell(currentRow, 12).Value = "Naslov raziskovalnega projekta (EN)";
            worksheet.Cell(currentRow, 13).Value = "(Project leader) Šifra";
            worksheet.Cell(currentRow, 14).Value = "Vodja raziskovalnega projekta Ime in priimek";
            worksheet.Cell(currentRow, 15).Value = "Prijavitelj - RO Šifra";
            worksheet.Cell(currentRow, 16).Value = "Prijavitelj - RO2";
            worksheet.Cell(currentRow, 17).Value = "Prijavitelj - RO Naziv";
            worksheet.Cell(currentRow, 18).Value = "OrgSodelSifra1";
            worksheet.Cell(currentRow, 19).Value = "OrgSodelNazivSLO1";
            worksheet.Cell(currentRow, 20).Value = "OrgSodelNazivANG1";
            worksheet.Cell(currentRow, 21).Value = "OrgSodelSifra2";
            worksheet.Cell(currentRow, 22).Value = "OrgSodelNazivSLO2";
            worksheet.Cell(currentRow, 23).Value = "OrgSodelNazivANG2";
            worksheet.Cell(currentRow, 24).Value = "OrgSodelSifra3";
            worksheet.Cell(currentRow, 25).Value = "OrgSodelNazivSLO3";
            worksheet.Cell(currentRow, 26).Value = "OrgSodelNazivANG3";
            worksheet.Cell(currentRow, 27).Value = "OrgSodelSifra4";
            worksheet.Cell(currentRow, 28).Value = "OrgSodelNazivSLO4";
            worksheet.Cell(currentRow, 29).Value = "OrgSodelNazivANG4";
            worksheet.Cell(currentRow, 30).Value = "OrgSodelSifra5";
            worksheet.Cell(currentRow, 31).Value = "OrgSodelNazivSLO5";
            worksheet.Cell(currentRow, 32).Value = "OrgSodelNazivANG5";
            worksheet.Cell(currentRow, 33).Value = "OrgSodelSifra6";
            worksheet.Cell(currentRow, 34).Value = "OrgSodelNazivSLO6";
            worksheet.Cell(currentRow, 35).Value = "OrgSodelNazivANG6";
            worksheet.Cell(currentRow, 36).Value = "Research hours";
            worksheet.Cell(currentRow, 37).Value = "Periods of funding";
            worksheet.Cell(currentRow, 38).Value = "Interdisc";
            worksheet.Cell(currentRow, 39).Value = "VPPSifra";
            worksheet.Cell(currentRow, 40).Value = "VPPVedaSifra";
            worksheet.Cell(currentRow, 41).Value = "VPPVedaNazivSlo";
            worksheet.Cell(currentRow, 42).Value = "VPPVedaNazivAng";
            worksheet.Cell(currentRow, 43).Value = "Primarno raziskovalno področje";
            worksheet.Cell(currentRow, 44).Value = "VPPPodrocjeNazivSlo";
            worksheet.Cell(currentRow, 45).Value = "VPPPodrocjeNazivAng";
            worksheet.Cell(currentRow, 46).Value = "Sekundarno raziskovalno področje";
            worksheet.Cell(currentRow, 47).Value = "VPPPodrocjeNazivSlo2";
            worksheet.Cell(currentRow, 48).Value = "VPPPodrocjeNazivAng2";
            worksheet.Cell(currentRow, 53).Value = "Poročevalec (Rapporteur) - Šifra";
            worksheet.Cell(currentRow, 54).Value = "Znanstveni urednik";

            // Pripravimo štiri stolpce za šifre recenzentov
            for (int i = 1; i <= 4; i++)
            {
                worksheet.Cell(currentRow, 48 + i).Value = $"Recenzent šifra {i}";
            }
            // Dodajanje podatkov
            foreach (var item in results)
            {
                currentRow++;
                worksheet.Cell(currentRow, 1).Value = "ARRS-RPROJ/2024"; // Naziv je fiksen za vse vnose
                worksheet.Cell(currentRow, 6).Value = "Vanja Rodič"; // Seznam skrbnikov za razpis je fiksen
                worksheet.Cell(currentRow, 7).Value = "proj-call-sd1@arrs.si"; // Centralni e-mail za obveščanje je fiksen
                worksheet.Cell(currentRow, 8).Value = item.StevilkaPrijave;
                worksheet.Cell(currentRow, 9).Value = item.VrstaProjekta;
                worksheet.Cell(currentRow, 11).Value = item.Naslov; // Slovenski naslov prijave
                worksheet.Cell(currentRow, 12).Value = item.AngNaslov; // Angleški naslov prijave
                worksheet.Cell(currentRow, 13).Value = item.SifraVodje;
                worksheet.Cell(currentRow, 14).Value = item.Vodja;
                worksheet.Cell(currentRow, 15).Value = item.SifraRO; // Prijavitelj - RO Šifra
                worksheet.Cell(currentRow, 17).Value = item.NazivRO; // Prijavitelj - RO Naziv

                // Ker so naslednje kolone od S do AL specifične za sodelujoče organizacije in so lahko prazne, jih preskočimo

                worksheet.Cell(currentRow, 36).Value = ""; // Research hours, če je potrebno dodati
                worksheet.Cell(currentRow, 37).Value = ""; // Periods of funding, če je potrebno dodati
                worksheet.Cell(currentRow, 38).Value = item.Interdisc;

                // Nadaljnje vrednosti za področja in podpodročja dodajte glede na vaše podatke
                worksheet.Cell(currentRow, 43).Value = item.PodpodrocjeKoda;
                worksheet.Cell(currentRow, 44).Value = item.PodpodrocjeNaziv;

                worksheet.Cell(currentRow, 46).Value = item.DodatnoPodpodrocjeKoda;
                worksheet.Cell(currentRow, 47).Value = item.DodatnoPodpodrocjeNaziv;
                // Šifre recenzentov - te so že pravilno dodane v vaši kodi, primer:
                for (int i = 0; i < 4; i++)
                {
                    worksheet.Cell(currentRow, 49 + i).Value = item.RecenzentiSifre.Count > i ? item.RecenzentiSifre[i] : string.Empty;
                }
                worksheet.Cell(currentRow, 53).Value = item.PorocevalecSifra;
                worksheet.Cell(currentRow, 54).Value = "9999999"; // Znanstveni urednik
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
                    worksheet.Cell(currentRow, 7).Value = string.Join(", ", prijava.Recenzenti.Select(r => r.Sifra + " " + r.Priimek + " (" + r.Vloga + ")"));
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
