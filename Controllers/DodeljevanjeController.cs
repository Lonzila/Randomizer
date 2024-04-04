﻿using ClosedXML.Excel;
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

                // Dodajte preostale recenzente, preskočite poročevalca, če je že dodan
                /*foreach (var sifra in item.RecenzentiSifre)
                {
                    if (sifra.ToString() != item.PorocevalecSifra.ToString())
                    {
                        recenzentiSifreUrejene.Add(sifra.ToString());
                    }
                }*/

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


    //-----------------------------------------------------------------------------------------------------------------------------------------------

    public async Task<IActionResult> ExportToExcel2()
    {
        // Najprej pridobimo vse prijave z njihovimi recenzenti

        var prijaveRecenzenti = await _context.Prijave
            .SelectMany(prijava => _context.GrozdiRecenzenti
                .Where(gr => gr.PrijavaID == prijava.PrijavaID)
                .Join(_context.Recenzenti, gr => gr.RecenzentID, recenzent => recenzent.RecenzentID,
                    (gr, recenzent) => new { prijava.PrijavaID, recenzent.Sifra, recenzent.Ime, recenzent.Priimek, recenzent.Drzava }))
            .ToListAsync();

        // Nato ustvarimo slovar, ki prijavi priredi seznam šifer recenzentov
        /*var recenzentiPoPrijavah = prijaveRecenzenti
            .GroupBy(pr => pr.PrijavaID)
            .ToDictionary(
                g => g.Key,
                g => g.Select(pr => pr.Sifra).Distinct().ToList()
            );
        */
        // Ustvarimo slovar, ki prijavi priredi seznam šifer recenzentov
        var recenzentiPoPrijavah = prijaveRecenzenti.ToList()
            .GroupBy(pr => pr.PrijavaID)
            .ToDictionary(
                g => g.Key,
                g => g.Select(pr => new { Sifra = pr.Sifra.ToString(), Ime = pr.Ime, Priimek = pr.Priimek, Drzava = pr.Drzava }).Distinct().ToList()
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

        // Pridobimo izločene recenzente za vsako prijavo
        /*var izloceniRecenzenti = await _context.IzloceniOsebni
              .Join(_context.Recenzenti,
                    izloceni => izloceni.RecenzentID,
                    recenzent => recenzent.RecenzentID,
                    (izloceni, recenzent) => new { izloceni.PrijavaID, recenzent.Sifra, recenzent.Ime, recenzent.Priimek })
              .ToListAsync();
        */
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
                                 Recenzenti = recenzentiPoPrijavah.ContainsKey(prijave.PrijavaID)
                                      ? recenzentiPoPrijavah[prijave.PrijavaID].Select(r => new { Sifra = r.Sifra, Ime = r.Ime, Priimek = r.Priimek, Drzava = r.Drzava }).ToList<object>()
                                      : new List<object>(),
                                 PorocevalecSifra = porocevalciIdsInSifre.ContainsKey(prijave.PrijavaID)
                                            ? porocevalciIdsInSifre[prijave.PrijavaID]
                                            : 0,
                                 /*IzloceniRecenzenti = izloceniRecenzenti
                                     .Where(x => x.PrijavaID == prijave.PrijavaID)
                                     .Select(r => new { Sifra = r.Sifra, Ime = r.Ime, Priimek = r.Priimek })
                                     .ToList()*/
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
            worksheet.Cell(currentRow, 67).Value = "Poročevalec (Rapporteur) - Šifra";
            worksheet.Cell(currentRow, 68).Value = "Znanstveni urednik";
            worksheet.Cell(currentRow, 69).Value = "Recenzent NE 1";
            worksheet.Cell(currentRow, 70).Value = "Recenzent NE 2";

            // Pripravimo štiri stolpce za ocenjevalce
            for (int i = 1; i <= 4; i++)
            {
                worksheet.Cell(currentRow, 49 + i).Value = $"Ocenjevalec {i} - Šifra";
            }

            // Dodamo stolpce za ime, priimek in državo vsakega recenzenta
            for (int i = 1; i <= 4; i++)
            {
                worksheet.Cell(currentRow, 53 + (i - 1) * 3).Value = $"Ocenjevalec {i} - Ime";
                worksheet.Cell(currentRow, 54 + (i - 1) * 3).Value = $"Ocenjevalec {i} - Priimek";
                worksheet.Cell(currentRow, 55 + (i - 1) * 3).Value = $"Ocenjevalec {i} - Država";
            }
            // Dodajanje podatkov
            foreach (var item in results)
            {
                // Pridobitev šifer recenzentov kot seznam, kjer je poročevalec vedno prvi
                List<string> recenzentiSifreUrejene = new List<string>();

                // Dodajte šifro poročevalca na prvo mesto, če obstaja
                if (item.PorocevalecSifra != 0 && !recenzentiSifreUrejene.Contains(item.PorocevalecSifra.ToString()))
                {
                    recenzentiSifreUrejene.Add(item.PorocevalecSifra.ToString());
                }

                // Dodajte preostale recenzente, preskočite poročevalca, če je že dodan
                foreach (var sifra in item.Recenzenti)
                {
                    if (sifra.ToString() != item.PorocevalecSifra.ToString())
                    {
                        recenzentiSifreUrejene.Add(sifra.ToString());
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
                // Dodajte ime, priimek in državo vsakega recenzenta v ustrezne stolpce
                int columnOffset = 4; // Preskoči štiri stolpce za šifre recenzentov
                foreach (var recenzentSifra in recenzentiSifreUrejene)
                {
                    var recenzent = await _context.Recenzenti.FirstOrDefaultAsync(r => r.Sifra.ToString() == recenzentSifra);
                    if (recenzent != null)
                    {
                        // Ustvarjanje imena stolpca (npr. ImeRecenzenta1, PriimekRecenzenta1, DrzavaRecenzenta1)
                        string columnNamePrefix = $"Recenzent{recenzentiSifreUrejene.IndexOf(recenzentSifra) + 1}";
                        worksheet.Cell(currentRow, 48 + columnOffset).Value = recenzent.Ime; // Ime recenzenta
                        worksheet.Cell(currentRow, 49 + columnOffset).Value = recenzent.Priimek; // Priimek recenzenta
                        worksheet.Cell(currentRow, 50 + columnOffset).Value = recenzent.Drzava; // Država recenzenta
                        columnOffset += 3; // Premakni se na naslednji stolpec za naslednjega recenzenta
                    }
                }

                worksheet.Cell(currentRow, 67).Value = item.PorocevalecSifra;
                worksheet.Cell(currentRow, 68).Value = "9999999"; // Znanstveni urednik
                                                                  // Dodajanje izločenih recenzentov na koncu vrstice
                /*for (int i = 0; i < item.IzloceniRecenzenti.Count; i++)
                {
                    worksheet.Cell(currentRow, 69 + i).Value = $"{item.IzloceniRecenzenti[i].Sifra} - {item.IzloceniRecenzenti[i].Ime} {item.IzloceniRecenzenti[i].Priimek}";
                }*/
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


    public async Task<IActionResult> ExportToExcel3()
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
