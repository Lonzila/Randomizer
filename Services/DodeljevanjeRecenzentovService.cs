using Randomizer.Data; // Zamenjajte z ustreznim namespace za vaš DbContext
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Randomizer.Models;
public static class PartnerskaAgencijaDrzavaMap
{
    public static readonly Dictionary<string, string> KodaNaDrzavo = new Dictionary<string, string>
    {
        { "GAČR", "Češka" },
        { "FWF", "Avstrija" },
        { "HRZZ", "Hrvaška" },
        { "NKFIH", "Madžarska" },
        { "NCN", "Poljska" },
        { "FWO", "Belgija" },
        { "FNR", "Luksemburg" },
        { "SNSF", "Švica" }
        // Dodajte vse druge ustrezne mape
    };
  
    private static Random rng = new Random();
    public static void Shuffle<T>(this IList<T> list)
    {
        int n = list.Count;
        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1);
            T value = list[k];
            list[k] = list[n];
            list[n] = value;
        }
    }

    public static string PretvoriVDrzavo(string kodaAgencije)
    {
        if (KodaNaDrzavo.TryGetValue(kodaAgencije, out string drzava))
        {
            return drzava;
        }
        else
        {
            // Vrnite privzeto vrednost ali obravnavajte neznano kodo
            return null; // ali "Neznana"
        }
    }

}

public class DodeljevanjeRecenzentovService
{
    private readonly ApplicationDbContext _context;
    private Dictionary<int, (int TrenutnoSteviloPrijav, int? MaksimalnoSteviloPrijav)> prostorRecenzentov;

    public DodeljevanjeRecenzentovService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task DodeliRecenzenteAsync()
    {
        
        // Pridobi podatke o vseh recenzentih in njihovo maksimalno število prijav
        var recenzentiPodatki = await _context.Recenzenti
            .Select(r => new { r.RecenzentID, r.SteviloProjektov })
            .ToListAsync();

        // Inicializacija slovarja za sledenje prostora recenzentov
        prostorRecenzentov = recenzentiPodatki.ToDictionary(
            r => r.RecenzentID,
            r => (0, r.SteviloProjektov)
        );
        /*
        var grozdi = await _context.Grozdi
                                .Include(g => g.Podpodrocje) // Zagotovite, da je ta vrstica pravilno nastavljena glede na vaš model
                                .Include(g => g.PrijavaGrozdi)
                                .ThenInclude(pg => pg.Prijava)
                                .ToListAsync();

        // Premešaj seznam grozdov z metodo Shuffle
        //debugger, da vidimo, če se seznam grozdov premeša
        grozdi.Shuffle();
        */
        // Najprej pridobite število recenzentov za vsako podpodročje
        var podpodrocjeRecenzenti = await _context.Podpodrocje
            .GroupJoin(
                _context.RecenzentiPodrocja, // druga tabela, s katero se združuje
                podpodrocje => podpodrocje.PodpodrocjeID, // ključ iz prve tabele
                recenzentPodrocja => recenzentPodrocja.PodpodrocjeID, // ključ iz druge tabele
                (podpodrocje, recenzentiPodrocja) => new // rezultat združevanja
                {
                    PodpodrocjeID = podpodrocje.PodpodrocjeID,
                    Koda = podpodrocje.Koda,
                    Naziv = podpodrocje.Naziv,
                    SteviloRecenzentov = recenzentiPodrocja.Count() // število recenzentov za to podpodročje
                }
            )
            .OrderBy(p => p.SteviloRecenzentov) // Urejanje po številu recenzentov
            .ToListAsync();

        // Pridobivanje in urejanje grozdov glede na prej pridobljene podatke
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
       

        foreach (var grozd in urejeniGrozdi)
        {
            //Console.WriteLine(grozd.Podpodrocje.Naziv);
            var recenzenti = await IzberiRecenzenteZaGrozdAsync(grozd);
            //Console.WriteLine($"Izbrani recenzenti za grozd {grozd.GrozdID}, {grozd.Podpodrocje.Naziv}: {string.Join(", ", recenzenti.Select(r => r.Priimek))}");

            // Naključno izberite enega recenzenta za dodelitev interdisciplinarnim prijavam, če obstajajo
            var random = new Random();
            Recenzent izbraniRecenzent = recenzenti.OrderBy(x => random.Next()).FirstOrDefault();
            int steviloPrijavVGrozd = grozd.PrijavaGrozdi.Count;
            foreach (var prijavaGrozdi in grozd.PrijavaGrozdi)
            {
                var prijava = prijavaGrozdi.Prijava;
                if (prijava.Interdisc == true && prijava.SteviloRecenzentov == 2)
                {
                    // Preverite, ali je trenutni grozd povezan s primarnim podpodročjem prijave
                    bool jePrimarnoPodpodrocje = prijava.PodpodrocjeID == grozd.PodpodrocjeID;

                    if (izbraniRecenzent != null)
                    {
                        // Dodeli izbranega recenzenta za vse interdisciplinarne prijave v grozdu
                        var vloga = jePrimarnoPodpodrocje ? "Poročevalec" : "Recenzent";

                        DodeliRecenzentaPrijava(izbraniRecenzent, grozd.GrozdID, prijava.PrijavaID, vloga, steviloPrijavVGrozd);
                    }
                }
                else
                {
                    if (prijava.Interdisc == true && prijava.SteviloRecenzentov == 4) 
                    {

                        // Preverite, ali je trenutni grozd povezan s primarnim podpodročjem prijave
                        bool jePrimarnoPodpodrocje = prijava.PodpodrocjeID == grozd.PodpodrocjeID;
                        if (jePrimarnoPodpodrocje)
                        {
                            var i = 0;
                            // Dodeli oba recenzenta za neinterdisciplinarno prijavo
                            foreach (var recenzent in recenzenti)
                            {

                                if (i == 0)
                                {
                                    DodeliRecenzentaPrijava(recenzent, grozd.GrozdID, prijava.PrijavaID, "Poročevalec", steviloPrijavVGrozd);
                                }
                                else
                                {
                                    DodeliRecenzentaPrijava(recenzent, grozd.GrozdID, prijava.PrijavaID, "Recenzent", steviloPrijavVGrozd);
                                }
                                i++;
                            }
                        } else
                        {
                            foreach (var recenzent in recenzenti)
                            {
                               DodeliRecenzentaPrijava(recenzent, grozd.GrozdID, prijava.PrijavaID, "Recenzent", steviloPrijavVGrozd);
                            }
                        }
                       
                    }
                    else
                    {
                        var vlogaPrvega = "Recenzent";
                        var vlogaDrugega = "Recenzent";

                        if (recenzenti.Count >= 2)
                        {
                            // Preveri, če kateri od recenzentov ni voljan biti poročevalec
                            if (recenzenti[0].Porocevalec == false && recenzenti[1].Porocevalec != false)
                            {
                                vlogaPrvega = "Recenzent";
                                vlogaDrugega = "Poročevalec";
                            }
                            else if (recenzenti[1].Porocevalec == false && recenzenti[0].Porocevalec != false)
                            {
                                vlogaPrvega = "Poročevalec";
                                vlogaDrugega = "Recenzent";
                            }
                            else
                            {
                                // Če oba recenzenta ali noben ne izrazi preference, uporabi prednastavljeno logiko za dodelitev
                                // Tukaj lahko uporabite dodatno logiko za odločitev
                                vlogaPrvega = "Poročevalec"; // Lahko uporabite naključno izbiro ali katero koli drugo logiko
                            }

                            // Dodeli vloge recenzentoma
                            DodeliRecenzentaPrijava(recenzenti[0], grozd.GrozdID, prijava.PrijavaID, vlogaPrvega, steviloPrijavVGrozd);
                            DodeliRecenzentaPrijava(recenzenti[1], grozd.GrozdID, prijava.PrijavaID, vlogaDrugega, steviloPrijavVGrozd);
                        }
                    }     
                    
                }
            }
        }

        await _context.SaveChangesAsync();

    }

    private void DodeliRecenzentaPrijava(Recenzent recenzent, int grozdID, int prijavaID, string vloga, int steviloPrijav)
    {
        var dodelitev = new GrozdiRecenzenti
        {
            GrozdID = grozdID,
            PrijavaID = prijavaID, // Dodajte PrijavaID
            RecenzentID = recenzent.RecenzentID,
            Vloga = vloga
        };
        _context.GrozdiRecenzenti.Add(dodelitev);

        // Posodobite število dodeljenih prijav za recenzenta v slovarju
        if (prostorRecenzentov.ContainsKey(recenzent.RecenzentID))
        {
            var trenutno = prostorRecenzentov[recenzent.RecenzentID];
            prostorRecenzentov[recenzent.RecenzentID] = (trenutno.TrenutnoSteviloPrijav + steviloPrijav, trenutno.MaksimalnoSteviloPrijav);
        }
    }
    
    private async Task<List<Recenzent>> IzberiRecenzenteZaGrozdAsync(Grozdi grozd)
    {

        // Najprej filtrirajte recenzente, ki so povezani s podpodročjem grozda
        var recenzentiPodpodrocja = await _context.RecenzentiPodrocja
            .Where(rp => rp.PodpodrocjeID == grozd.PodpodrocjeID)
            .Select(rp => rp.RecenzentID)
            .ToListAsync();

        var potencialniRecenzenti = await _context.Recenzenti
            .Where(r => recenzentiPodpodrocja.Contains(r.RecenzentID))
            .ToListAsync();

        
        // Če ni dovolj recenzentov, razširi iskanje na recenzentipodpodrocjafull
        if (potencialniRecenzenti.Count < 2) // MinSteviloRecenzentovZaDodelitev je konstanta ali konfiguracija
        {
            potencialniRecenzenti.AddRange(await PridobiPotencialneRecenzenteIzPodpodrocjaFull(grozd));
            // Odstranite podvojene vnose, če je to potrebno
            potencialniRecenzenti = potencialniRecenzenti.Distinct().ToList();
        }

        // Pridobite seznam vseh partnerskih agencij iz prijav, ki so del tega grozda
        var partnerskeAgencijeKode = await _context.PrijavaGrozdi
            .Where(pg => pg.GrozdID == grozd.GrozdID)
            .SelectMany(pg => _context.Prijave
                .Where(p => p.PrijavaID == pg.PrijavaID)
                .Select(p => p.PartnerskaAgencija1))
            .Distinct()
            .ToListAsync();

        // Dodajte kode drugih potencialnih partnerskih agencij, če obstajajo
        var dodatnePartnerskeAgencijeKode = await _context.PrijavaGrozdi
            .Where(pg => pg.GrozdID == grozd.GrozdID)
            .SelectMany(pg => _context.Prijave
                .Where(p => p.PrijavaID == pg.PrijavaID)
                .Select(p => p.PartnerskaAgencija2)) // Če obstaja drugo polje za partnersko agencijo
            .Distinct()
            .ToListAsync();

        partnerskeAgencijeKode.AddRange(dodatnePartnerskeAgencijeKode);
        partnerskeAgencijeKode = partnerskeAgencijeKode.Distinct().ToList();

        // Pretvorite kode partnerskih agencij v države
        var partnerskeAgencijeDrzave = partnerskeAgencijeKode
            .Where(koda => !string.IsNullOrEmpty(koda)) // Odstrani null ali prazne kode
            .Select(koda => PartnerskaAgencijaDrzavaMap.PretvoriVDrzavo(koda))
            .Where(drzava => drzava != null)
            .ToList();

        // Pridobite vse potencialne recenzente, ki ne prihajajo iz teh držav
        potencialniRecenzenti
            .Where(r => !partnerskeAgencijeDrzave.Contains(r.Drzava));

        // Pridobite seznam PrijavaID, ki so del tega grozda
        var prijaveVGrozd = await _context.PrijavaGrozdi
            .Where(pg => pg.GrozdID == grozd.GrozdID)
            .Select(pg => pg.PrijavaID)
            .ToListAsync();


        var potrebujePorocevalcaIzPrimarnegaPodpodrocja = grozd.PrijavaGrozdi.Any(pg => pg.Prijava.Interdisc == true && pg.Prijava.PodpodrocjeID == grozd.PodpodrocjeID);

        if (potrebujePorocevalcaIzPrimarnegaPodpodrocja)
        {
            // Omeji na recenzente, ki so voljni biti poročevalci iz primarnega podpodročja
            potencialniRecenzenti = potencialniRecenzenti
                .Where(r => r.Porocevalec != false && recenzentiPodpodrocja.Contains(r.RecenzentID))
                .ToList();
        }


        // Pridobite seznam RecenzentID, ki so izločeni zaradi konflikta interesov za te prijave
        var izloceniRecenzentiCOI = await _context.IzloceniCOI
            .Where(coi => prijaveVGrozd.Contains(coi.PrijavaID))
            .Select(coi => coi.RecenzentID)
            .Distinct()
            .ToListAsync();
        
        // Izločite recenzente, ki so na seznamu izločenih zaradi konflikta interesov
        potencialniRecenzenti = potencialniRecenzenti
            .Where(r => !izloceniRecenzentiCOI.Contains(r.RecenzentID))
            .ToList();

        if (izloceniRecenzentiCOI.Any())
        {
            //Console.WriteLine($"--------Najdeni so bili recenzenti s konfliktom interesov za grozd {grozd.GrozdID}. Izločeni recenzenti: {string.Join(", ", izloceniRecenzentiCOI)}");
        }


        //-------------------------------------------------------------------------------------

        var recenzentiZDovoljProstora = new List<Recenzent>();

        foreach (var recenzent in potencialniRecenzenti)
        {
            // Uporabite slovar za preverjanje prostora za recenzenta
            if (prostorRecenzentov.TryGetValue(recenzent.RecenzentID, out var recenzentInfo))
            {
                var trenutnoSteviloPrijavGrozda = await _context.PrijavaGrozdi
                    .Where(pg => pg.GrozdID == grozd.GrozdID)
                    .CountAsync();
                // Preverjanje, ali ima recenzent še prostor za dodatne prijave
                bool imaDovoljProstora = recenzentInfo.TrenutnoSteviloPrijav + trenutnoSteviloPrijavGrozda <= recenzentInfo.MaksimalnoSteviloPrijav;

                if (imaDovoljProstora)
                {
                    recenzentiZDovoljProstora.Add(recenzent);
                }
            }
            else
            {
                // Če recenzent ni v slovarju, lahko predpostavimo, da ni bil še dodeljen nobeni prijavi,
                // ali pa se odločimo za dodatno logiko, kako ravnati v tem primeru.
                // Za zdaj ga dodamo v seznam, če želite obravnavati drugače, prilagodite kodo.
                Console.WriteLine($"RecenzentID {recenzent.RecenzentID} ni najden v slovarju.");
            }
        }

        // Preverite, če je dovolj recenzentov za naključno izbiro
        if (recenzentiZDovoljProstora.Count <= 2)
        {
            return recenzentiZDovoljProstora;
        }
        
        // Pridobite seznam RecenzentID, ki so izločeni zaradi osebnih razlogov za te prijave
        var izloceniRecenzentiOsebni = await _context.IzloceniOsebni
            .Where(osebni => prijaveVGrozd.Contains(osebni.PrijavaID))
            .Select(osebni => osebni.RecenzentID)
            .Distinct()
            .ToListAsync();

        // Izločite recenzente, ki so na seznamu izločenih zaradi osebnih razlogov
        var recenzentiZDovoljProstoraPlusOsebni = recenzentiZDovoljProstora
            .Where(r => !izloceniRecenzentiOsebni.Contains(r.RecenzentID))
            .ToList();

        
        if (recenzentiZDovoljProstoraPlusOsebni.Count > 2)
        {
            // Naključno izberite recenzente iz filtrirane liste recenzentov z dovolj prostora
            var nakljucniRecenzenti = new List<Recenzent>();
            var random = new Random();
            for (int i = 0; i < 2; i++)
            {
                int index = random.Next(recenzentiZDovoljProstoraPlusOsebni.Count);
                nakljucniRecenzenti.Add(recenzentiZDovoljProstoraPlusOsebni[index]);
                recenzentiZDovoljProstoraPlusOsebni.RemoveAt(index); // Odstranite, da preprečite ponovno izbiro
            }
            return nakljucniRecenzenti;

        } else
        {
            // Naključno izberite recenzente iz filtrirane liste recenzentov z dovolj prostora
            var nakljucniRecenzenti = new List<Recenzent>();
            var random = new Random();
            for (int i = 0; i < 2; i++)
            {
                int index = random.Next(recenzentiZDovoljProstora.Count);
                nakljucniRecenzenti.Add(recenzentiZDovoljProstora[index]);
                recenzentiZDovoljProstora.RemoveAt(index); // Odstranite, da preprečite ponovno izbiro
            }

            return nakljucniRecenzenti;
        }
    }

    public async Task<List<GrozdiViewModel>> PridobiInformacijeZaIzpisAsync()
    {
        var grozdi = await _context.Grozdi
            .Include(g => g.Podpodrocje)
            .ToListAsync();

        var grozdiViewModels = new List<GrozdiViewModel>();

        foreach (var grozd in grozdi)
        {
            var prijavaGrozdi = await _context.PrijavaGrozdi
                .Include(pg => pg.Prijava)
                .Where(pg => pg.GrozdID == grozd.GrozdID)
                .ToListAsync();

            var prijaveInfo = new List<PrijavaViewModel>();

            foreach (var prijavaGrozd in prijavaGrozdi)
            {
                var prijava = prijavaGrozd.Prijava;
                var recenzentiInfo = await _context.GrozdiRecenzenti
                    .Where(gr => gr.GrozdID == grozd.GrozdID && gr.PrijavaID == prijava.PrijavaID)
                    .Include(gr => gr.Recenzent)
                    .Select(gr => new RecenzentInfo
                    {
                        RecenzentID = gr.RecenzentID,
                        Sifra = gr.Recenzent.Sifra,
                        Priimek = gr.Recenzent.Priimek,
                        Vloga = gr.Vloga,
                    }).ToListAsync();

                prijaveInfo.Add(new PrijavaViewModel
                {
                    PrijavaID = prijava.PrijavaID,
                    StevilkaPrijave = prijava.StevilkaPrijave,
                    Naslov = prijava.Naslov,
                    Interdisc = (bool)prijava.Interdisc,
                    SteviloRecenzentov = prijava.SteviloRecenzentov,
                    Podpodrocje = prijava.Podpodrocje?.Naziv, // Dodajte preverjanje null, če je potrebno
                    DodatnoPodpodrocje = prijava.DodatnoPodpodrocje?.Naziv,
                    Recenzenti = recenzentiInfo
                });
            }

            grozdiViewModels.Add(new GrozdiViewModel
            {
                GrozdID = grozd.GrozdID,
                PodpodrocjeNaziv = grozd.Podpodrocje.Naziv,
                Prijave = prijaveInfo
            });
        }

        return grozdiViewModels;
    }

    private async Task<List<Recenzent>> PridobiPotencialneRecenzenteIzPodpodrocjaFull(Grozdi grozd)
    {
        // Logika za pridobivanje recenzentov iz recenzentipodpodrocjafull, ki so povezani s podpodročjem grozda
        var recenzentiPodpodrocjaFull = await _context.RecenzentiPodpodrocjaFull
            .Where(rpf => rpf.PodpodrocjeID == grozd.PodpodrocjeID)
            .Select(rpf => rpf.RecenzentID)
            .ToListAsync();

        var potencialniRecenzenti = await _context.Recenzenti
            .Where(r => recenzentiPodpodrocjaFull.Contains(r.RecenzentID))
            .ToListAsync();

        return potencialniRecenzenti;
    }
    public async Task PocistiDodelitveRecenzentovAsync()
    {
        // Pridobi vse obstoječe dodelitve recenzentov
        var vseDodelitve = await _context.GrozdiRecenzenti.ToListAsync();

        // Izbris vseh dodelitev
        _context.GrozdiRecenzenti.RemoveRange(vseDodelitve);
        await _context.SaveChangesAsync();
    }

}
