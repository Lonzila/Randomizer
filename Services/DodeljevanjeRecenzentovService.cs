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

    public DodeljevanjeRecenzentovService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task DodeliRecenzenteAsync()
    {
        var grozdi = await _context.Grozdi
                                .Include(g => g.Podpodrocje) // Zagotovite, da je ta vrstica pravilno nastavljena glede na vaš model
                                .Include(g => g.PrijavaGrozdi)
                                .ThenInclude(pg => pg.Prijava)
                                .ToListAsync();

        foreach (var grozd in grozdi)
        {
            var recenzenti = await IzberiRecenzenteZaGrozdAsync(grozd);
            Console.WriteLine($"Izbrani recenzenti za grozd {grozd.GrozdID}, {grozd.Podpodrocje.Naziv}: {string.Join(", ", recenzenti.Select(r => r.Priimek))}");

            // Naključno izberite enega recenzenta za dodelitev interdisciplinarnim prijavam, če obstajajo
            var random = new Random();
            Recenzent izbraniRecenzent = recenzenti.OrderBy(x => random.Next()).FirstOrDefault();

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
                        DodeliRecenzentaPrijava(izbraniRecenzent, grozd.GrozdID, vloga);
                    }
                }
                else
                {
                    // Dodeli oba recenzenta za neinterdisciplinarno prijavo
                    foreach (var recenzent in recenzenti)
                    {
                        DodeliRecenzentaPrijava(recenzent, grozd.GrozdID, "Recenzent");
                    }
                }
            }
        }

        await _context.SaveChangesAsync();

    }

    private void DodeliRecenzentaPrijava(Recenzent recenzent, int grozdID, string vloga)
    {
        var dodelitev = new GrozdiRecenzenti
        {
            GrozdID = grozdID,
            RecenzentID = recenzent.RecenzentID,
            Vloga = vloga
        };
        _context.GrozdiRecenzenti.Add(dodelitev);
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
            Console.WriteLine($"Najdeni so bili recenzenti s konfliktom interesov za grozd {grozd.GrozdID}. Izločeni recenzenti: {string.Join(", ", izloceniRecenzentiCOI)}");
        }

        //-------------------------------------------------------------------------------------

        var recenzentiZDovoljProstora = new List<Recenzent>();
        foreach (var recenzent in potencialniRecenzenti)
        {
            var trenutnoSteviloPrijav = await _context.PrijavaGrozdi
                .Where(pg => pg.GrozdID == grozd.GrozdID)
                .CountAsync();

            var trenutnoSteviloDodeljenihProjektov = await _context.GrozdiRecenzenti
                .Where(gr => gr.RecenzentID == recenzent.RecenzentID)
                .SelectMany(gr => _context.PrijavaGrozdi
                    .Where(pg => pg.GrozdID == gr.GrozdID))
                .CountAsync();
            // Preverjanje, ali ima recenzent neskončno kapaciteto (SteviloProjektov == null) ali če je trenutno število dodeljenih projektov v okviru njihove omejitve
            bool imaDovoljProstora = recenzent.SteviloProjektov == null || (trenutnoSteviloDodeljenihProjektov + trenutnoSteviloPrijav) <= recenzent.SteviloProjektov;

            if (imaDovoljProstora)
            {
                recenzentiZDovoljProstora.Add(recenzent);
            }
            
        }

        // Preverite, če je dovolj recenzentov za naključno izbiro
        if (recenzentiZDovoljProstora.Count <= 2)
        {
            return recenzentiZDovoljProstora;
        }

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


    public async Task<List<GrozdiViewModel>> PridobiInformacijeZaIzpisAsync()
    {
        // Pridobivanje informacij o grozdih in prijavah
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

            var prijaveInfo = prijavaGrozdi.Select(pg => new PrijavaInfo
            {
                PrijavaID = pg.PrijavaID,
                StevilkaPrijave = pg.Prijava.StevilkaPrijave,
                Naslov = pg.Prijava.Naslov,
                Interdisc = (bool)pg.Prijava.Interdisc, // Dodano
                SteviloRecenzentov = pg.Prijava.SteviloRecenzentov // Dodano
            }).ToList();

            var grozdViewModel = new GrozdiViewModel
            {
                GrozdID = grozd.GrozdID,
                PodpodrocjeNaziv = grozd.Podpodrocje.Naziv,
                Prijave = prijaveInfo,
                Recenzenti = new List<RecenzentInfo>()
            };

            // Pridobivanje dodeljenih recenzentov za grozd
            var dodeljeniRecenzenti = await _context.GrozdiRecenzenti
                .Where(gr => gr.GrozdID == grozd.GrozdID)
                .Select(gr => gr.RecenzentID)
                .Distinct()
                .ToListAsync();

            foreach (var recenzentID in dodeljeniRecenzenti)
            {
                var recenzent = await _context.Recenzenti
                    .FindAsync(recenzentID);

                var recenzentPodrocja = await _context.RecenzentiPodrocja
                    .Include(rp => rp.Podpodrocje)
                    .Where(rp => rp.RecenzentID == recenzentID)
                    .ToListAsync();

                var recenzentInfo = new RecenzentInfo
                {
                    RecenzentID = recenzent.RecenzentID,
                    Ime = recenzent.Ime,
                    Priimek = recenzent.Priimek,
                    Podpodrocja = recenzentPodrocja.Select(rp => rp.Podpodrocje.Naziv).ToList()
                };

                grozdViewModel.Recenzenti.Add(recenzentInfo);
            }

            grozdiViewModels.Add(grozdViewModel);
        }

        return grozdiViewModels;
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
