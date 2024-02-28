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
                                .ToListAsync();

        foreach (var grozd in grozdi)
        {
            var recenzenti = await IzberiRecenzenteZaGrozdAsync(grozd);
            Console.WriteLine($"Izbrani recenzenti za grozd {grozd.GrozdID}, {grozd.Podpodrocje.Naziv}: {string.Join(", ", recenzenti.Select(r => r.Priimek))}");
            /*
            foreach (var recenzent in recenzenti)
            {
                var dodelitev = new GrozdiRecenzenti
                {
                    GrozdID = grozd.GrozdID,
                    RecenzentID = recenzent.RecenzentID,
                    Vloga = "Poročevalec" // Določite vlogo glede na vaša pravila
                };

                if (!_context.GrozdiRecenzenti.Any(gr => gr.GrozdID == dodelitev.GrozdID && gr.RecenzentID == dodelitev.RecenzentID))
                {
                    _context.GrozdiRecenzenti.Add(dodelitev);
                }
            }
            */
        }

        await _context.SaveChangesAsync();
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
        // Preverite, če je dovolj recenzentov za naključno izbiro
        if (potencialniRecenzenti.Count <= 2)
        {
            return potencialniRecenzenti;
        }

        // Naključno izberite recenzente
        var nakljucniRecenzenti = new List<Recenzent>();
        var random = new Random();
        for (int i = 0; i < 2; i++)
        {
            int index = random.Next(potencialniRecenzenti.Count);
            nakljucniRecenzenti.Add(potencialniRecenzenti[index]);
            potencialniRecenzenti.RemoveAt(index); // Odstranite, da preprečite ponovno izbiro
        }

        return nakljucniRecenzenti;
    }
}
