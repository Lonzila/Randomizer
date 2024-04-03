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
    private Dictionary<int, (int TrenutnoSteviloPrijav, int? MaksimalnoSteviloPrijav, string Vloga)> prostorRecenzentov;

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
            r => (0, r.SteviloProjektov, (string)null)
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
            
            var recenzenti = await IzberiRecenzenteZaGrozdAsync(grozd);
            
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

                if ( recenzenti.Count() < 2)
                {
                    prvaVloga = prostorRecenzentov.ContainsKey(recenzenti[0].RecenzentID) ? prostorRecenzentov[recenzenti[0].RecenzentID].Vloga : null;
                }else
                {
                    prvaVloga = prostorRecenzentov.ContainsKey(recenzenti[0].RecenzentID) ? prostorRecenzentov[recenzenti[0].RecenzentID].Vloga : null;
                    drugaVloga = prostorRecenzentov.ContainsKey(recenzenti[1].RecenzentID) ? prostorRecenzentov[recenzenti[1].RecenzentID].Vloga : null;
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
                                recenzentZaPorocevalca = drugaVloga == "Poročevalec" ? recenzenti[1] : recenzenti[0] ;
                            }
                            // Če je prijava povezana s primarnim podpodročjem, dodeli recenzenta z vlogo poročevalca
                           
                            var vloga = "Poročevalec"; // Ker je prijava povezana s primarnim podpodročjem
                            DodeliRecenzentaPrijava(recenzentZaPorocevalca, grozd.GrozdID, prijava.PrijavaID, vloga, steviloPrijavVGrozd);

                        } else
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

                                Recenzent recenzentZaPorocevalca= null;
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
            prostorRecenzentov[recenzent.RecenzentID] = (trenutno.TrenutnoSteviloPrijav + steviloPrijav, trenutno.MaksimalnoSteviloPrijav, vloga);
        }
    }
    int steviloUporabe = 0;
    private async Task<List<Recenzent>> IzberiRecenzenteZaGrozdAsync(Grozdi grozd)
    {
        Console.WriteLine("Novo iskanje recenzentov za grozd");

        // Najprej filtrirajte recenzente, ki so povezani s podpodročjem grozda
        var recenzentiPodpodrocja = await _context.RecenzentiPodrocja
            .Where(rp => rp.PodpodrocjeID == grozd.PodpodrocjeID)
            .Select(rp => rp.RecenzentID)
            .ToListAsync();


        var potencialniRecenzenti = await _context.Recenzenti
            .Where(r => recenzentiPodpodrocja.Contains(r.RecenzentID) && r.OdpovedPredDolocitvijo != true)
            .ToListAsync();


        /*if (potencialniRecenzenti.Count < 2) // MinSteviloRecenzentovZaDodelitev je konstanta ali konfiguracija
        {
            steviloUporabe++;
            Console.WriteLine("Uporabil je vsa podpodročja ARIS in anketo: " + steviloUporabe);
            potencialniRecenzenti.AddRange(await PridobiPotencialneRecenzenteIzPodpodrocjaFull(grozd));
            // Odstranite podvojene vnose, če je to potrebno
            potencialniRecenzenti = potencialniRecenzenti.Distinct().ToList();
        }*/
        //partnerskeAgencijeKode.AddRange(dodatnePartnerskeAgencijeKode);
        //partnerskeAgencijeKode = partnerskeAgencijeKode.Distinct().ToList();

        // Pretvorite kode partnerskih agencij v države
        /*var partnerskeAgencijeDrzave = partnerskeAgencijeKode
            .Where(koda => !string.IsNullOrEmpty(koda)) // Odstrani null ali prazne kode
            .Select(koda => PartnerskaAgencijaDrzavaMap.PretvoriVDrzavo(koda))
            .Where(drzava => drzava != null)
            .ToList();
        */
        // Pridobitev seznamov kod partnerskih agencij iz prijav, ki so del tega grozda
        var partnerskeAgencijeKode = await _context.PrijavaGrozdi
            .Where(pg => pg.GrozdID == grozd.GrozdID)
            .SelectMany(pg => _context.Prijave
                .Where(p => p.PrijavaID == pg.PrijavaID)
                .Select(p => new { p.PartnerskaAgencija1, p.PartnerskaAgencija2 }))
            .Distinct()
            .ToListAsync();

        // Izpis vseh kod partnerskih agencij pred pretvorbo
        //Console.WriteLine("Kode partnerskih agencij pred pretvorbo:");
        foreach (var kode in partnerskeAgencijeKode)
        {
            Console.WriteLine($"PartnerskaAgencija1: {kode.PartnerskaAgencija1}, PartnerskaAgencija2: {kode.PartnerskaAgencija2}");
        }

        // Pretvorite kode partnerskih agencij v države
        var partnerskeAgencijeDrzave = new List<string>();
        foreach (var kode in partnerskeAgencijeKode)
        {
            if (!string.IsNullOrEmpty(kode.PartnerskaAgencija1))
            {
                var drzava1 = PartnerskaAgencijaDrzavaMap.PretvoriVDrzavo(kode.PartnerskaAgencija1);
                if (drzava1 != null && !partnerskeAgencijeDrzave.Contains(drzava1))
                {
                    partnerskeAgencijeDrzave.Add(drzava1);
                    Console.WriteLine($"Koda {kode.PartnerskaAgencija1} pretvorjena v državo: {drzava1}");
                }
            }

            if (!string.IsNullOrEmpty(kode.PartnerskaAgencija2))
            {
                var drzava2 = PartnerskaAgencijaDrzavaMap.PretvoriVDrzavo(kode.PartnerskaAgencija2);
                if (drzava2 != null && !partnerskeAgencijeDrzave.Contains(drzava2))
                {
                    partnerskeAgencijeDrzave.Add(drzava2);
                    Console.WriteLine($"Koda {kode.PartnerskaAgencija2} pretvorjena v državo: {drzava2}");
                }
            }
        }

        // Končni seznam držav partnerskih agencij
        Console.WriteLine("Končni seznam držav partnerskih agencij:");
        foreach (var drzava in partnerskeAgencijeDrzave.Distinct())
        {
            Console.WriteLine(drzava);
        }


        // Shranite originalni seznam potencialnih recenzentov
        var originalniPotencialniRecenzenti = potencialniRecenzenti.ToList();

        // Filtriranje potencialnih recenzentov, ki ne prihajajo iz teh držav
        potencialniRecenzenti = potencialniRecenzenti
            .Where(r => !partnerskeAgencijeDrzave.Contains(r.Drzava))
            .ToList();

        // Izpis recenzentov, ki so bili izločeni
        var izloceniRecenzenti = originalniPotencialniRecenzenti.Except(potencialniRecenzenti).ToList();
        Console.WriteLine("Recenzenti izločeni na podlagi države:");
        foreach (var recenzent in izloceniRecenzenti)
        {
            Console.WriteLine($"RecenzentID: {recenzent.RecenzentID}, Država: {recenzent.Drzava}");
        }

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



        /*if (izloceniRecenzentiCOI.Any())
        {
            //Console.WriteLine($"--------Najdeni so bili recenzenti s konfliktom interesov za grozd {grozd.GrozdID}. Izločeni recenzenti: {string.Join(", ", izloceniRecenzentiCOI)}");
        }*/

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
        //--------------------------------------------------------------------------------------------------------------------------------

        // Ločevanje recenzentov po možnih vlogah
        var možniPoročevalci = recenzentiZDovoljProstoraPlusOsebni
            .Where(r => !prostorRecenzentov.ContainsKey(r.RecenzentID) || prostorRecenzentov[r.RecenzentID].Vloga == "Poročevalec" || prostorRecenzentov[r.RecenzentID].Vloga == null)
            .ToList();

        var možniRecenzenti = recenzentiZDovoljProstoraPlusOsebni
            .Where(r => !prostorRecenzentov.ContainsKey(r.RecenzentID) || prostorRecenzentov[r.RecenzentID].Vloga == "Recenzent" || prostorRecenzentov[r.RecenzentID].Vloga == null)
            .ToList();
    
        // Preverjanje, ali obstajajo vsaj en možni poročevalec in en možni recenzent
        if (možniPoročevalci.Any() && možniRecenzenti.Any())
        {
            var izbraniRecenzenti = new List<Recenzent>();
            var random = new Random();

            // Dodajanje enega naključnega poročevalca
            var izbraniPoročevalecIndex = random.Next(možniPoročevalci.Count);
            var izbraniPoročevalec = možniPoročevalci[izbraniPoročevalecIndex];
            izbraniRecenzenti.Add(izbraniPoročevalec);

            // Odstrani izbranega poročevalca iz možnih recenzentov, da se prepreči izbor istega recenzenta za obe vlogi
            možniRecenzenti.Remove(izbraniPoročevalec);

            // Dodajanje enega naključnega recenzenta, ki ni isti kot poročevalec
            var izbraniRecenzentIndex = random.Next(možniRecenzenti.Count);
            var izbraniRecenzent = možniRecenzenti[izbraniRecenzentIndex];
            izbraniRecenzenti.Add(izbraniRecenzent);

            return izbraniRecenzenti;
        }
        else
        {
            Console.WriteLine("Zgodilo se je, da ne bo šlo določiti enakih vlog enemu recenzentu povsod!");
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

            }
            else
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
                        Drzava = gr.Recenzent.Drzava
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
                    PartnerskaAgencija1 = prijava.PartnerskaAgencija1,
                    PartnerskaAgencija2 = prijava.PartnerskaAgencija2,
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
