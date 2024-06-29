using ClosedXML.Excel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Randomizer.Data;
using Randomizer.Models;
using Randomizer.Services;

public class DodeljevanjeController : Controller
{
    private readonly DodeljevanjeRecenzentovService _dodeljevanjeRecenzentovService;
    private readonly RecenzentZavrnitveService _recenzentZavrnitveService;
    private readonly ApplicationDbContext _context;

    public DodeljevanjeController(DodeljevanjeRecenzentovService dodeljevanjeRecenzentovService, RecenzentZavrnitveService recenzentZavrnitveService, ApplicationDbContext context)
    {
        _dodeljevanjeRecenzentovService = dodeljevanjeRecenzentovService;
        _recenzentZavrnitveService = recenzentZavrnitveService;
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

        var timestamp = DateTime.Now;
        var userIpAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        using (var workbook = new XLWorkbook())
        {
            var worksheet = workbook.Worksheets.Add("Podatki");
            // Dodajanje loga v glavo
            worksheet.Cell("A1").Value = $"IP naslov uporabnika: {userIpAddress}";
            worksheet.Cell("A2").Value = $"Datum in čas izvedbe: {timestamp}";

            // Nastavitev začetne vrstice za podatke
            var currentRow = 4; // Začnemo pri vrstici 4, da pustimo prostor za log

            // Nastavitev glave z novimi naslovi
            worksheet.Cell(currentRow, 1).Value = "Naziv";
            worksheet.Cell(currentRow, 2).Value = "Datum začetka ocenjevanja";
            worksheet.Cell(currentRow, 3).Value = "Datum konca ocenjevanja";
            worksheet.Cell(currentRow, 4).Value = "Datum začetka, ko lahko vodje pregledajo skupne ocene";
            worksheet.Cell(currentRow, 5).Value = "Datum konca, ko lahko vodje pregledajo skupno oceno";
            worksheet.Cell(currentRow, 6).Value = "Seznam skrbnikov za razpis (lahko tudi naknadno, za fazo testiranja pa že nujno)";
            worksheet.Cell(currentRow, 7).Value = "Centralni e-mail za obveščanje";
            worksheet.Cell(currentRow, 8).Value = "Prijavna številka (Application number)";
            worksheet.Cell(currentRow, 9).Value = "Tip raziskovalnega projekta (Type of the research project)";
            worksheet.Cell(currentRow, 10).Value = "ProjektTipOpisAng";
            worksheet.Cell(currentRow, 11).Value = "ProjektNaslovSlo";
            worksheet.Cell(currentRow, 12).Value = "Naslov raziskovalnega projekta (Title of the research project)";
            worksheet.Cell(currentRow, 13).Value = "ProgrSifra";
            worksheet.Cell(currentRow, 14).Value = "(Project leader) o Šifra (Code number) ";
            worksheet.Cell(currentRow, 15).Value = "Vodja raziskovalnega projekta (Project leader) o Ime in priimek (Name and Surname)";
            worksheet.Cell(currentRow, 16).Value = "Prijavitelj - raziskovalna organizacija (RO) (Applicant - Research organisation) o Šifra (Code number) ";
            worksheet.Cell(currentRow, 17).Value = "Prijavitelj - raziskovalna organizacija (RO) (Applicant - Research organisation)2";
            worksheet.Cell(currentRow, 18).Value = "Prijavitelj - raziskovalna organizacija (RO) (Applicant - Research organisation)  Naziv organizacije ";
            worksheet.Cell(currentRow, 19).Value = "OrgSodelSifra1";
            worksheet.Cell(currentRow, 20).Value = "OrgSodelNazivSLO1";
            worksheet.Cell(currentRow, 21).Value = "OrgSodelNazivANG1";
            worksheet.Cell(currentRow, 22).Value = "OrgSodelSifra2";
            worksheet.Cell(currentRow, 23).Value = "OrgSodelNazivSLO2";
            worksheet.Cell(currentRow, 24).Value = "OrgSodelNazivANG2";
            worksheet.Cell(currentRow, 25).Value = "OrgSodelSifra3";
            worksheet.Cell(currentRow, 26).Value = "OrgSodelNazivSLO3";
            worksheet.Cell(currentRow, 27).Value = "OrgSodelNazivANG3";
            worksheet.Cell(currentRow, 28).Value = "OrgSodelSifra4";
            worksheet.Cell(currentRow, 29).Value = "OrgSodelNazivSLO4";
            worksheet.Cell(currentRow, 30).Value = "OrgSodelNazivANG4";
            worksheet.Cell(currentRow, 31).Value = "OrgSodelSifra5";
            worksheet.Cell(currentRow, 32).Value = "OrgSodelNazivSLO5";
            worksheet.Cell(currentRow, 33).Value = "OrgSodelNazivANG5";
            worksheet.Cell(currentRow, 34).Value = "OrgSodelSifra6";
            worksheet.Cell(currentRow, 35).Value = "OrgSodelNazivSLO6";
            worksheet.Cell(currentRow, 36).Value = "OrgSodelNazivANG6";
            worksheet.Cell(currentRow, 37).Value = "Research hours";
            worksheet.Cell(currentRow, 38).Value = "Periods of funding";
            worksheet.Cell(currentRow, 39).Value = "Interdisc";
            worksheet.Cell(currentRow, 40).Value = "VPPSifra";
            worksheet.Cell(currentRow, 41).Value = "VPPVedaSifra";
            worksheet.Cell(currentRow, 42).Value = "VPPVedaNazivSlo";
            worksheet.Cell(currentRow, 43).Value = "VPPVedaNazivAng";
            worksheet.Cell(currentRow, 44).Value = "Primarno raziskovalno področje po šifrantu ARRS (Primary research field – Classification ARRS)";
            worksheet.Cell(currentRow, 45).Value = "VPPPodrocjeNazivSlo";
            worksheet.Cell(currentRow, 46).Value = "VPPPodrocjeNazivAng";
            worksheet.Cell(currentRow, 47).Value = "Sekundarno raziskovalno področje po šifrantu ARRS (Secondary research field – Classification ARRS) pri interdisciplinarnih raziskavah";
            worksheet.Cell(currentRow, 48).Value = "VPPPodrocjeNazivSlo2";
            worksheet.Cell(currentRow, 49).Value = "VPPPodrocjeNazivAng2";
            worksheet.Cell(currentRow, 54).Value = "Poročevalec (Rapporteur) - Šifra";
            worksheet.Cell(currentRow, 55).Value = "Znanstveni urednik";

            // Pripravimo štiri stolpce za šifre recenzentov
            for (int i = 1; i <= 4; i++)
            {
                worksheet.Cell(currentRow, 49 + i).Value = $"Ocenjevalec {i} - Šifra";
            }
            // Dodajanje podatkov
            foreach (var item in results)
            {
                List<string> recenzentiSifreUrejene = new List<string>();

                // Določitev, kdo je poročevalec (že storjeno prej)
                if (item.PorocevalecSifra != 0 && !recenzentiSifreUrejene.Contains(item.PorocevalecSifra.ToString()))
                {
                    recenzentiSifreUrejene.Add(item.PorocevalecSifra.ToString());
                }

                // Glede na to, da sta poročevalec in Ocenjevalec 2 vedno skupaj, določite indeks za Ocenjevalca 2
                int porocevalecIndex = item.RecenzentiSifre.FindIndex(s => s.ToString() == item.PorocevalecSifra.ToString());
                if (porocevalecIndex != -1) // Poročevalec najden
                {
                    int ocenjevalec2Index = porocevalecIndex % 2 == 0 ? porocevalecIndex + 1 : porocevalecIndex - 1; // Določitev nasprotnega indeksa
                    if (ocenjevalec2Index >= 0 && ocenjevalec2Index < item.RecenzentiSifre.Count) // Preverjanje, če je indeks veljaven
                    {
                        string ocenjevalec2Sifra = item.RecenzentiSifre[ocenjevalec2Index].ToString();
                        if (!recenzentiSifreUrejene.Contains(ocenjevalec2Sifra))
                        {
                            recenzentiSifreUrejene.Add(ocenjevalec2Sifra); // Dodaj Ocenjevalca 2 na seznam
                        }
                    }
                }

                // Dodajte preostale recenzente, izogibajte se ponovitvi poročevalca in Ocenjevalca 2
                foreach (var sifra in item.RecenzentiSifre)
                {
                    string sifraString = sifra.ToString();
                    if (sifraString != item.PorocevalecSifra.ToString() && !recenzentiSifreUrejene.Contains(sifraString))
                    {
                        recenzentiSifreUrejene.Add(sifraString);
                    }
                }

                currentRow++;
                worksheet.Cell(currentRow, 1).Value = "ARRS-RPROJ/2024"; // Naziv je fiksen za vse vnose
                
                worksheet.Cell(currentRow, 8).Value = item.StevilkaPrijave;
                worksheet.Cell(currentRow, 10).Value = item.VrstaProjekta;
                worksheet.Cell(currentRow, 11).Value = item.Naslov; // Slovenski naslov prijave
                worksheet.Cell(currentRow, 12).Value = item.AngNaslov; // Angleški naslov prijave
                worksheet.Cell(currentRow, 14).Value = item.SifraVodje;
                worksheet.Cell(currentRow, 15).Value = item.Vodja;
                worksheet.Cell(currentRow, 16).Value = item.SifraRO; // Prijavitelj - RO Šifra
                worksheet.Cell(currentRow, 17).Value = item.NazivRO; // Prijavitelj - RO Naziv

                // Ker so naslednje kolone od S do AL specifične za sodelujoče organizacije in so lahko prazne, jih preskočimo
                worksheet.Cell(currentRow, 39).Value = item.Interdisc;
                worksheet.Cell(currentRow, 40).Value = item.PodpodrocjeKoda;
                // Nadaljnje vrednosti za področja in podpodročja dodajte glede na vaše podatke
                worksheet.Cell(currentRow, 44).Value = item.PodpodrocjeKoda;
                worksheet.Cell(currentRow, 45).Value = item.PodpodrocjeNaziv;

                worksheet.Cell(currentRow, 47).Value = item.DodatnoPodpodrocjeKoda;
                worksheet.Cell(currentRow, 48).Value = item.DodatnoPodpodrocjeNaziv;
                // Šifre recenzentov - te so že pravilno dodane v vaši kodi, primer:
                for (int i = 0; i < 4; i++)
                {
                    worksheet.Cell(currentRow, 50 + i).Value = recenzentiSifreUrejene.Count > i ? recenzentiSifreUrejene[i] : string.Empty;
                }
                worksheet.Cell(currentRow, 54).Value = item.PorocevalecSifra;
                worksheet.Cell(currentRow, 55).Value = "9999999"; // Znanstveni urednik
            }
          
            using (var stream = new MemoryStream())
            {
                workbook.SaveAs(stream);
                var content = stream.ToArray();

                return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "PrijaveInRecenzenti.xlsx");
            }
        }
    }

    //-------------------------------------------------------------------------------------------------------------------------------------------------

    public async Task<IActionResult> ExportMenjaveToExcel()
    {
        var menjave = _recenzentZavrnitveService.PridobiMenjaveRecenzentov();

        using (var workbook = new XLWorkbook())
        {
            var worksheet = workbook.Worksheets.Add("Menjave Recenzentov");
            worksheet.Cell("A1").Value = "Originalni Recenzent ID";
            worksheet.Cell("B1").Value = "Predlagan Recenzent ID";
            worksheet.Cell("C1").Value = "Prijava ID";

            int currentRow = 2;
            foreach (var menjava in menjave)
            {
                worksheet.Cell(currentRow, 1).Value = menjava.OriginalniRecenzentID;
                worksheet.Cell(currentRow, 2).Value = menjava.NadomestniRecenzentID;
                worksheet.Cell(currentRow, 3).Value = menjava.PrijavaID;
                currentRow++;
            }

            using (var stream = new MemoryStream())
            {
                workbook.SaveAs(stream);
                var content = stream.ToArray();
                return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "MenjaveRecenzentov.xlsx");
            }
        }
    }


    //-------------------------------------------------------------------------------------------------------------------------------------------------
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

    // Nova metoda za obdelavo zavrnitev in preusmeritev na prikaz zamenjav
    public async Task<IActionResult> ObdelajZavrnitve()
    {
        await _recenzentZavrnitveService.ObdelajZavrnitveInDodeliNoveRecenzenteAsync();
        return RedirectToAction("PrikazZamenjav");
    }

    // Nova metoda za prikaz zamenjav
    public IActionResult PrikazZamenjav()
    {
        var menjave = _recenzentZavrnitveService.PridobiMenjaveRecenzentov();
        return View(menjave);
    }



}
