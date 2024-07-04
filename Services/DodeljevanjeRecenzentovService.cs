using Randomizer.Data;
using Microsoft.EntityFrameworkCore;
using Randomizer.Models;
using Randomizer.Helpers;


public class DodeljevanjeRecenzentovService
{
    private readonly ApplicationDbContext _context;
    private Dictionary<int, (int TrenutnoSteviloPrijav, int? MaksimalnoSteviloPrijav, string Vloga)> _prostorRecenzentov;

    public DodeljevanjeRecenzentovService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task DodeliRecenzenteAsync()
    {
        await InicializirajProstorRecenzentovAsync();
        var podpodrocjeRecenzenti = await PridobiPodpodrocjeRecenzenteAsync();
        var urejeniGrozdi = await PridobiUrejeneGrozdeAsync(podpodrocjeRecenzenti);

        foreach (var grozd in urejeniGrozdi)
        {
            var recenzenti = await IzberiRecenzenteZaGrozdAsync(grozd);
            await DodeliRecenzenteZaGrozdAsync(grozd, recenzenti);
        }

        await _context.SaveChangesAsync();
    }

    private async Task InicializirajProstorRecenzentovAsync()
    {
        var recenzentiPodatki = await _context.Recenzenti
            .Select(r => new { r.RecenzentID, r.SteviloProjektov })
            .ToListAsync();

        _prostorRecenzentov = recenzentiPodatki.ToDictionary(
            r => r.RecenzentID,
            r => (0, r.SteviloProjektov, (string)null)
        );
    }

    private async Task<List<PodpodrocjeRecenzentovViewModel>> PridobiPodpodrocjeRecenzenteAsync()
    {
        return await _context.Podpodrocje
            .GroupJoin(
                _context.RecenzentiPodrocja,
                podpodrocje => podpodrocje.PodpodrocjeID,
                recenzentPodrocja => recenzentPodrocja.PodpodrocjeID,
                (podpodrocje, recenzentiPodrocja) => new PodpodrocjeRecenzentovViewModel
                {
                    PodpodrocjeID = podpodrocje.PodpodrocjeID,
                    Koda = podpodrocje.Koda,
                    Naziv = podpodrocje.Naziv,
                    SteviloRecenzentov = recenzentiPodrocja.Count()
                }
            )
            .OrderBy(p => p.SteviloRecenzentov)
            .ToListAsync();
    }

    private async Task<List<Grozdi>> PridobiUrejeneGrozdeAsync(List<PodpodrocjeRecenzentovViewModel> podpodrocjeRecenzenti)
    {
        var urejeniGrozdi = new List<Grozdi>();
        foreach (var podpodrocje in podpodrocjeRecenzenti)
        {
            var grozdiZaPodpodrocje = await _context.Grozdi
                .Include(g => g.Podpodrocje)
                .Include(g => g.PrijavaGrozdi)
                    .ThenInclude(pg => pg.Prijava)
                .Where(g => g.PodpodrocjeID == podpodrocje.PodpodrocjeID)
                .ToListAsync();

            urejeniGrozdi.AddRange(grozdiZaPodpodrocje);
        }
        return urejeniGrozdi;
    }

    private async Task DodeliRecenzenteZaGrozdAsync(Grozdi grozd, List<Recenzent> recenzenti)
    {
        // Naključno izberite enega recenzenta za dodelitev interdisciplinarnim prijavam, če obstajajo
        var random = new Random();
        Recenzent izbraniRecenzent = null;
        //recenzenti.OrderBy(x => random.Next()).FirstOrDefault();
        int steviloPrijavVGrozd = grozd.PrijavaGrozdi.Count;
        foreach (var prijavaGrozdi in grozd.PrijavaGrozdi)
        {

            var prijava = prijavaGrozdi.Prijava;

            string prvaVloga = null;
            string drugaVloga = null;

            if (recenzenti.Count() < 2)
            {
                prvaVloga = _prostorRecenzentov.ContainsKey(recenzenti[0].RecenzentID) ? _prostorRecenzentov[recenzenti[0].RecenzentID].Vloga : null;
            }
            else
            {
                prvaVloga = _prostorRecenzentov.ContainsKey(recenzenti[0].RecenzentID) ? _prostorRecenzentov[recenzenti[0].RecenzentID].Vloga : null;
                drugaVloga = _prostorRecenzentov.ContainsKey(recenzenti[1].RecenzentID) ? _prostorRecenzentov[recenzenti[1].RecenzentID].Vloga : null;
            }


            if (prijava.Interdisc == true && prijava.SteviloRecenzentov == 2)
            {
                if (prvaVloga != null || drugaVloga != null)
                {
                    bool jePrimarnoPodpodrocje = prijava.PodpodrocjeID == grozd.PodpodrocjeID;
                    if (jePrimarnoPodpodrocje)
                    {
                        Recenzent recenzentZaPorocevalca = null;
                        // Preverimo, ali ima prvi recenzent določeno vlogo
                        if (prvaVloga != null)
                        {
                            // Če ima prvi recenzent določeno vlogo, določimo nasprotno vlogo drugemu recenzentu
                            recenzentZaPorocevalca = prvaVloga == "Poročevalec" ? recenzenti[0] : recenzenti[1];
                        }
                        // Preverimo, ali ima drugi recenzent določeno vlogo
                        else if (drugaVloga != null)
                        {
                            recenzentZaPorocevalca = drugaVloga == "Poročevalec" ? recenzenti[1] : recenzenti[0];
                        }
                        // Če je prijava povezana s primarnim podpodročjem, dodeli recenzenta z vlogo poročevalca

                        var vloga = "Poročevalec"; // Ker je prijava povezana s primarnim podpodročjem
                        DodeliRecenzentaPrijava(recenzentZaPorocevalca, grozd.GrozdID, prijava.PrijavaID, vloga, steviloPrijavVGrozd);

                    }
                    else
                    {
                        Recenzent recenzentZaRecenzenta = null;
                        // Preverimo, ali ima prvi recenzent določeno vlogo
                        if (prvaVloga != null)
                        {
                            // Če ima prvi recenzent določeno vlogo, določimo nasprotno vlogo drugemu recenzentu
                            recenzentZaRecenzenta = prvaVloga == "Recenzent" ? recenzenti[0] : recenzenti[1];
                        }
                        // Preverimo, ali ima drugi recenzent določeno vlogo
                        else if (drugaVloga != null)
                        {
                            recenzentZaRecenzenta = drugaVloga == "Recenzent" ? recenzenti[1] : recenzenti[0];
                        }
                        // Če je prijava povezana s primarnim podpodročjem, dodeli recenzenta z vlogo poročevalca

                        var vloga = "Recenzent"; // Ker je prijava povezana s primarnim podpodročjem
                        DodeliRecenzentaPrijava(recenzentZaRecenzenta, grozd.GrozdID, prijava.PrijavaID, vloga, steviloPrijavVGrozd);
                    }

                }
                else
                {
                    // Preveri, če kateri od recenzentov ima v polju Porocevalec vrednost true
                    var recenzentZaPorocevalca = recenzenti.FirstOrDefault(r => r.Porocevalec == true);
                    if (recenzentZaPorocevalca != null)
                    {
                        // Če je najden recenzent, ki želi biti poročevalec
                        izbraniRecenzent = recenzentZaPorocevalca;
                    }
                    else
                    {
                        // Če noben od recenzentov ne izraža želje biti poročevalec, ali so vsi neodločeni, izberi naključnega
                        izbraniRecenzent = recenzenti.OrderBy(x => random.Next()).FirstOrDefault();
                    }
                    // Preverite, ali je trenutni grozd povezan s primarnim podpodročjem prijave
                    bool jePrimarnoPodpodrocje = prijava.PodpodrocjeID == grozd.PodpodrocjeID;

                    if (izbraniRecenzent != null)
                    {
                        // Dodeli izbranega recenzenta za vse interdisciplinarne prijave v grozdu
                        var vloga = jePrimarnoPodpodrocje ? "Poročevalec" : "Recenzent";

                        DodeliRecenzentaPrijava(izbraniRecenzent, grozd.GrozdID, prijava.PrijavaID, vloga, steviloPrijavVGrozd);
                    }
                }

            }
            else
            {
                if (prijava.Interdisc == true && prijava.SteviloRecenzentov == 4)
                {
                    bool jePrimarnoPodpodrocje = prijava.PodpodrocjeID == grozd.PodpodrocjeID;
                    if (jePrimarnoPodpodrocje)
                    {
                        // Preveri, ali imajo recenzenti voljo biti poročevalec
                        var recenzentiZVoljo = recenzenti.Where(r => r.Porocevalec == true).ToList();

                        if (recenzentiZVoljo.Count > 0)
                        {
                            // Če obstaja vsaj en recenzent, ki želi biti poročevalec
                            var porocevalec = recenzentiZVoljo.First(); // Vzame prvega, ki želi biti poročevalec
                            var drugiRecenzent = recenzenti.First(r => r.RecenzentID != porocevalec.RecenzentID);

                            DodeliRecenzentaPrijava(porocevalec, grozd.GrozdID, prijava.PrijavaID, "Poročevalec", steviloPrijavVGrozd);
                            DodeliRecenzentaPrijava(drugiRecenzent, grozd.GrozdID, prijava.PrijavaID, "Recenzent", steviloPrijavVGrozd);
                        }
                        else
                        {
                            // Če noben ne želi biti poročevalec ali imata oba null, izberi poljubno
                            DodeliRecenzentaPrijava(recenzenti[0], grozd.GrozdID, prijava.PrijavaID, "Poročevalec", steviloPrijavVGrozd);
                            DodeliRecenzentaPrijava(recenzenti[1], grozd.GrozdID, prijava.PrijavaID, "Recenzent", steviloPrijavVGrozd);
                        }
                    }
                    else
                    {
                        foreach (var recenzent in recenzenti)
                        {
                            DodeliRecenzentaPrijava(recenzent, grozd.GrozdID, prijava.PrijavaID, "Recenzent", steviloPrijavVGrozd);
                        }
                    }

                }
                else
                {
                    // Preveri, če obstaja recenzent, ki izrecno ne želi biti poročevalec
                    var recenzentiNeZelijo = recenzenti.Where(r => r.Porocevalec == false).ToList();

                    if (recenzentiNeZelijo.Count == recenzenti.Count)
                    {
                        Console.WriteLine("Zgodilo se je, da sta bila določena dva, ki ne želita biti poročevalca");
                        // Če oba recenzenta izrecno ne želita biti poročevalca, izvedi ponovno pridobivanje recenzentov
                        // To lahko vključuje ponovno klicanje IzberiRecenzenteZaGrozdAsync z nekimi dodatnimi parametri ali logiko
                        // za ponovno pridobitev ustreznih recenzentov.
                        recenzenti = await IzberiRecenzenteZaGrozdAsync(grozd);

                        var recenzentNeZeliBitiPoroc = recenzenti.FirstOrDefault(r => r.Porocevalec == false);

                        // Preveri, ali obstaja recenzent, ki želi biti poročevalec
                        var recenzentiZVoljo = recenzenti.Where(r => r.Porocevalec == true).ToList();

                        if (recenzentiZVoljo.Count > 0)
                        {
                            // Obstaja vsaj en recenzent, ki želi biti poročevalec
                            var porocevalec = recenzentiZVoljo.First(); // Vzame prvega, ki želi biti poročevalec
                            var drugiRecenzent = recenzenti.First(r => r.RecenzentID != porocevalec.RecenzentID);
                            DodeliRecenzentaPrijava(porocevalec, grozd.GrozdID, prijava.PrijavaID, "Poročevalec", steviloPrijavVGrozd);
                            DodeliRecenzentaPrijava(drugiRecenzent, grozd.GrozdID, prijava.PrijavaID, "Recenzent", steviloPrijavVGrozd);
                        }
                        else if (recenzentNeZeliBitiPoroc != null)
                        {
                            // Če točno en recenzent izrecno ne želi biti poročevalec, dodeli vlogo poročevalca drugemu recenzentu
                            var porocevalec = recenzenti.First(r => r.RecenzentID != recenzentNeZeliBitiPoroc.RecenzentID);
                            DodeliRecenzentaPrijava(porocevalec, grozd.GrozdID, prijava.PrijavaID, "Poročevalec", steviloPrijavVGrozd);
                            DodeliRecenzentaPrijava(recenzentNeZeliBitiPoroc, grozd.GrozdID, prijava.PrijavaID, "Recenzent", steviloPrijavVGrozd);
                        }
                        else
                        {
                            // Če noben ne želi biti poročevalec ali imata oba null, izberi prvega kot poročevalca
                            DodeliRecenzentaPrijava(recenzenti[0], grozd.GrozdID, prijava.PrijavaID, "Poročevalec", steviloPrijavVGrozd);
                            DodeliRecenzentaPrijava(recenzenti[1], grozd.GrozdID, prijava.PrijavaID, "Recenzent", steviloPrijavVGrozd);
                        }

                    }
                    else
                    {
                        if (prvaVloga != null || drugaVloga != null)
                        {

                            Recenzent recenzentZaPorocevalca = null;
                            Recenzent recenzentZaRecenzenta = null;
                            // Preverimo, ali ima prvi recenzent določeno vlogo
                            if (prvaVloga != null)
                            {
                                // Če ima prvi recenzent določeno vlogo, določimo nasprotno vlogo drugemu recenzentu
                                recenzentZaPorocevalca = prvaVloga == "Poročevalec" ? recenzenti[0] : recenzenti[1];
                                recenzentZaRecenzenta = prvaVloga == "Poročevalec" ? recenzenti[1] : recenzenti[0];

                            }
                            // Preverimo, ali ima drugi recenzent določeno vlogo
                            else if (drugaVloga != null)
                            {
                                recenzentZaPorocevalca = drugaVloga == "Poročevalec" ? recenzenti[1] : recenzenti[0];
                                recenzentZaRecenzenta = drugaVloga == "Poročevalec" ? recenzenti[0] : recenzenti[1];
                            }
                            DodeliRecenzentaPrijava(recenzentZaPorocevalca, grozd.GrozdID, prijava.PrijavaID, "Poročevalec", steviloPrijavVGrozd);
                            DodeliRecenzentaPrijava(recenzentZaRecenzenta, grozd.GrozdID, prijava.PrijavaID, "Recenzent", steviloPrijavVGrozd);

                        }
                        else
                        {
                            // Identificiraj recenzenta, ki izrecno ne želi biti poročevalec
                            var recenzentNeZeliBitiPoroc = recenzenti.FirstOrDefault(r => r.Porocevalec == false);

                            // Preveri, ali obstaja recenzent, ki želi biti poročevalec
                            var recenzentiZVoljo = recenzenti.Where(r => r.Porocevalec == true).ToList();

                            if (recenzentiZVoljo.Count > 0)
                            {
                                // Obstaja vsaj en recenzent, ki želi biti poročevalec
                                var porocevalec = recenzentiZVoljo.First(); // Vzame prvega, ki želi biti poročevalec
                                var drugiRecenzent = recenzenti.First(r => r.RecenzentID != porocevalec.RecenzentID);
                                DodeliRecenzentaPrijava(porocevalec, grozd.GrozdID, prijava.PrijavaID, "Poročevalec", steviloPrijavVGrozd);
                                DodeliRecenzentaPrijava(drugiRecenzent, grozd.GrozdID, prijava.PrijavaID, "Recenzent", steviloPrijavVGrozd);
                            }
                            else if (recenzentNeZeliBitiPoroc != null)
                            {
                                // Če točno en recenzent izrecno ne želi biti poročevalec, dodeli vlogo poročevalca drugemu recenzentu
                                var porocevalec = recenzenti.First(r => r.RecenzentID != recenzentNeZeliBitiPoroc.RecenzentID);
                                DodeliRecenzentaPrijava(porocevalec, grozd.GrozdID, prijava.PrijavaID, "Poročevalec", steviloPrijavVGrozd);
                                DodeliRecenzentaPrijava(recenzentNeZeliBitiPoroc, grozd.GrozdID, prijava.PrijavaID, "Recenzent", steviloPrijavVGrozd);
                            }
                            else
                            {
                                // Če noben ne želi biti poročevalec ali imata oba null, izberi prvega kot poročevalca
                                DodeliRecenzentaPrijava(recenzenti[0], grozd.GrozdID, prijava.PrijavaID, "Poročevalec", steviloPrijavVGrozd);
                                DodeliRecenzentaPrijava(recenzenti[1], grozd.GrozdID, prijava.PrijavaID, "Recenzent", steviloPrijavVGrozd);
                            }
                        }

                    }
                }
            }
        }
    }

    private void DodeliRecenzentaPrijava(Recenzent recenzent, int grozdID, int prijavaID, string vloga, int steviloPrijav)
    {
        var dodelitev = new GrozdiRecenzenti
        {
            GrozdID = grozdID,
            PrijavaID = prijavaID,
            RecenzentID = recenzent.RecenzentID,
            Vloga = vloga
        };
        _context.GrozdiRecenzenti.Add(dodelitev);

        if (_prostorRecenzentov.ContainsKey(recenzent.RecenzentID))
        {
            var trenutno = _prostorRecenzentov[recenzent.RecenzentID];
            _prostorRecenzentov[recenzent.RecenzentID] = (trenutno.TrenutnoSteviloPrijav + steviloPrijav, trenutno.MaksimalnoSteviloPrijav, vloga);
        }
    }

    private async Task<List<Recenzent>> IzberiRecenzenteZaGrozdAsync(Grozdi grozd)
    {
        var recenzentiPodpodrocja = await _context.RecenzentiPodrocja
            .Where(rp => rp.PodpodrocjeID == grozd.PodpodrocjeID)
            .Select(rp => rp.RecenzentID)
            .ToListAsync();

        var potencialniRecenzenti = await _context.Recenzenti
            .Where(r => recenzentiPodpodrocja.Contains(r.RecenzentID) && r.OdpovedPredDolocitvijo != true)
            .ToListAsync();

        var partnerskeAgencijeKode = await _context.PrijavaGrozdi
            .Where(pg => pg.GrozdID == grozd.GrozdID)
            .SelectMany(pg => _context.Prijave
                .Where(p => p.PrijavaID == pg.PrijavaID)
                .Select(p => new { p.PartnerskaAgencija1, p.PartnerskaAgencija2 }))
            .Distinct()
            .ToListAsync();

        var partnerskeAgencijeDrzave = new List<string>();
        foreach (var kode in partnerskeAgencijeKode)
        {
            if (!string.IsNullOrEmpty(kode.PartnerskaAgencija1))
            {
                var drzava1 = PartnerskaAgencijaDrzavaMap.PretvoriVDrzavo(kode.PartnerskaAgencija1);
                if (drzava1 != null && !partnerskeAgencijeDrzave.Contains(drzava1))
                {
                    partnerskeAgencijeDrzave.Add(drzava1);
                }
            }

            if (!string.IsNullOrEmpty(kode.PartnerskaAgencija2))
            {
                var drzava2 = PartnerskaAgencijaDrzavaMap.PretvoriVDrzavo(kode.PartnerskaAgencija2);
                if (drzava2 != null && !partnerskeAgencijeDrzave.Contains(drzava2))
                {
                    partnerskeAgencijeDrzave.Add(drzava2);
                }
            }
        }

        potencialniRecenzenti = potencialniRecenzenti
            .Where(r => !partnerskeAgencijeDrzave.Contains(r.Drzava))
            .ToList();

        var prijaveVGrozd = await _context.PrijavaGrozdi
            .Where(pg => pg.GrozdID == grozd.GrozdID)
            .Select(pg => pg.PrijavaID)
            .ToListAsync();

        var potrebujePorocevalcaIzPrimarnegaPodpodrocja = grozd.PrijavaGrozdi.Any(pg => pg.Prijava.Interdisc == true && pg.Prijava.PodpodrocjeID == grozd.PodpodrocjeID);

        if (potrebujePorocevalcaIzPrimarnegaPodpodrocja)
        {
            potencialniRecenzenti = potencialniRecenzenti
                .Where(r => r.Porocevalec != false && recenzentiPodpodrocja.Contains(r.RecenzentID))
                .ToList();
        }

        var izloceniRecenzentiCOI = await _context.IzloceniCOI
            .Where(coi => prijaveVGrozd.Contains(coi.PrijavaID))
            .Select(coi => coi.RecenzentID)
            .Distinct()
            .ToListAsync();

        potencialniRecenzenti = potencialniRecenzenti
            .Where(r => !izloceniRecenzentiCOI.Contains(r.RecenzentID))
            .ToList();

        var recenzentiZDovoljProstora = new List<Recenzent>();
        foreach (var recenzent in potencialniRecenzenti)
        {
            if (_prostorRecenzentov.TryGetValue(recenzent.RecenzentID, out var recenzentInfo))
            {
                var trenutnoSteviloPrijavGrozda = await _context.PrijavaGrozdi
                    .Where(pg => pg.GrozdID == grozd.GrozdID)
                    .CountAsync();

                bool imaDovoljProstora = recenzentInfo.TrenutnoSteviloPrijav + trenutnoSteviloPrijavGrozda <= recenzentInfo.MaksimalnoSteviloPrijav;
                if (imaDovoljProstora)
                {
                    recenzentiZDovoljProstora.Add(recenzent);
                }
            }
            else
            {
                recenzentiZDovoljProstora.Add(recenzent);
            }
        }

        if (recenzentiZDovoljProstora.Count <= 2)
        {
            return recenzentiZDovoljProstora;
        }

        var izloceniRecenzentiOsebni = await _context.IzloceniOsebni
            .Where(osebni => prijaveVGrozd.Contains(osebni.PrijavaID))
            .Select(osebni => osebni.RecenzentID)
            .Distinct()
            .ToListAsync();

        var recenzentiZDovoljProstoraPlusOsebni = recenzentiZDovoljProstora
            .Where(r => !izloceniRecenzentiOsebni.Contains(r.RecenzentID))
            .ToList();

        var možniPoročevalci = recenzentiZDovoljProstoraPlusOsebni
            .Where(r => !_prostorRecenzentov.ContainsKey(r.RecenzentID) || _prostorRecenzentov[r.RecenzentID].Vloga == "Poročevalec" || _prostorRecenzentov[r.RecenzentID].Vloga == null)
            .ToList();

        var možniRecenzenti = recenzentiZDovoljProstoraPlusOsebni
            .Where(r => !_prostorRecenzentov.ContainsKey(r.RecenzentID) || _prostorRecenzentov[r.RecenzentID].Vloga == "Recenzent" || _prostorRecenzentov[r.RecenzentID].Vloga == null)
            .ToList();

        if (možniPoročevalci.Any() && možniRecenzenti.Any())
        {
            var izbraniRecenzenti = new List<Recenzent>();
            var random = new Random();

            var izbraniPoročevalecIndex = random.Next(možniPoročevalci.Count);
            var izbraniPoročevalec = možniPoročevalci[izbraniPoročevalecIndex];
            izbraniRecenzenti.Add(izbraniPoročevalec);

            možniRecenzenti.Remove(izbraniPoročevalec);
            var izbraniRecenzentIndex = random.Next(možniRecenzenti.Count);
            var izbraniRecenzent = možniRecenzenti[izbraniRecenzentIndex];
            izbraniRecenzenti.Add(izbraniRecenzent);

            return izbraniRecenzenti;
        }
        else
        {
            var nakljucniRecenzenti = new List<Recenzent>();
            var random = new Random();
            for (int i = 0; i < 2; i++)
            {
                int index = random.Next(recenzentiZDovoljProstoraPlusOsebni.Count);
                nakljucniRecenzenti.Add(recenzentiZDovoljProstoraPlusOsebni[index]);
                recenzentiZDovoljProstoraPlusOsebni.RemoveAt(index);
            }
            return nakljucniRecenzenti;
        }
    }
}

public class PodpodrocjeRecenzentovViewModel
{
    public int PodpodrocjeID { get; set; }
    public string Koda { get; set; }
    public string Naziv { get; set; }
    public int SteviloRecenzentov { get; set; }
}

