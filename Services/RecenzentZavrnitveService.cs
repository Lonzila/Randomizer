namespace Randomizer.Services
{
    using Randomizer.Data;
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
    public class RecenzentZavrnitveService
    {
        private readonly ApplicationDbContext _context;

        public RecenzentZavrnitveService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task ObdelajZavrnitveInDodeliNoveRecenzenteAsync()
        {
            var zavrnitve = await _context.RecenzentiZavrnitve.ToListAsync();

            foreach (var zavrnitev in zavrnitve)
            {
                var grozdId = await PridobiGrozdIdZaPrijavoAsync(zavrnitev.PrijavaID);
                if (grozdId == null) continue;

                var originalnaDodelitev = await _context.GrozdiRecenzenti
                    .FirstOrDefaultAsync(gr => gr.GrozdID == grozdId.Value && gr.PrijavaID == zavrnitev.PrijavaID && gr.RecenzentID == zavrnitev.RecenzentID);

                if (originalnaDodelitev != null)
                {
                    var nadomestniRecenzent = await NajdiNadomestnegaRecenzentaAsync(grozdId.Value, zavrnitev.PrijavaID, zavrnitev.RecenzentID);
                    if (nadomestniRecenzent != null)
                    {
                        // Posodobite RecenzentID z ID-jem nadomestnega recenzenta
                        originalnaDodelitev.RecenzentID = nadomestniRecenzent.RecenzentID;
                        _context.GrozdiRecenzenti.Update(originalnaDodelitev);
                    }
                }
            }

            await _context.SaveChangesAsync();
        }

        private async Task<int?> PridobiGrozdIdZaPrijavoAsync(int prijavaId)
        {
            var grozdId = await _context.PrijavaGrozdi
                .Where(pg => pg.PrijavaID == prijavaId)
                .Select(pg => pg.GrozdID)
                .FirstOrDefaultAsync();

            return grozdId == 0 ? (int?)null : grozdId;
        }

        private async Task<Recenzent> NajdiNadomestnegaRecenzentaAsync(int grozdId, int prijavaId, int izkljuceniRecenzentId)
        {
            var vlogaZavrnjenegaRecenzenta = await _context.GrozdiRecenzenti
                .Where(gr => gr.RecenzentID == izkljuceniRecenzentId && gr.PrijavaID == prijavaId)
                .Select(gr => gr.Vloga)
                .FirstOrDefaultAsync();

            bool jePorocevalec = vlogaZavrnjenegaRecenzenta == "Poročevalec";

            // Pridobitev podatkov o prijavi in grozdu
            var prijava = await _context.Prijave.FindAsync(prijavaId);
            var grozd = await _context.Grozdi.Include(g => g.Podpodrocje).FirstOrDefaultAsync(g => g.GrozdID == grozdId);

            // Izločitev recenzentov, ki so že zavrnili ali so izključeni zaradi konflikta interesov
            var izkljuceniRecenzenti = new List<int> { izkljuceniRecenzentId };
            var zavrnitve = await _context.RecenzentiZavrnitve.Where(z => z.PrijavaID == prijavaId).Select(z => z.RecenzentID).ToListAsync();
            izkljuceniRecenzenti.AddRange(zavrnitve);

            var izloceniRecenzentiCOI = await _context.IzloceniCOI
                .Where(coi => coi.PrijavaID == prijavaId)
                .Select(coi => coi.RecenzentID)
                .ToListAsync();
            izkljuceniRecenzenti.AddRange(izloceniRecenzentiCOI);

            // Filtriranje potencialnih recenzentov
            var recenzentiPodpodrocja = await _context.RecenzentiPodrocja
                .Where(rp => rp.PodpodrocjeID == grozd.PodpodrocjeID && !izkljuceniRecenzenti.Contains(rp.RecenzentID))
                .Select(rp => rp.RecenzentID)
                .ToListAsync();

            var potencialniRecenzenti = await _context.Recenzenti
                .Where(r => recenzentiPodpodrocja.Contains(r.RecenzentID))
                .ToListAsync();

            // Upoštevanje partnerskih agencij in držav
            var partnerskeAgencijeKode = new List<string> { prijava.PartnerskaAgencija1, prijava.PartnerskaAgencija2 }.Where(k => !string.IsNullOrEmpty(k)).Distinct();
            var partnerskeAgencijeDrzave = partnerskeAgencijeKode.Select(koda => PartnerskaAgencijaDrzavaMap.PretvoriVDrzavo(koda)).ToList();
            potencialniRecenzenti = potencialniRecenzenti.Where(r => !partnerskeAgencijeDrzave.Contains(r.Drzava)).ToList();

            if (jePorocevalec)
            {
                potencialniRecenzenti = potencialniRecenzenti
                    .Where(r => r.Porocevalec != false)
                    .ToList();
            }

            var recenzentiZDovoljProstora = new List<Recenzent>();
            foreach (var recenzent in potencialniRecenzenti)
            {
                var trenutnoSteviloDodeljenihPrijav = await _context.GrozdiRecenzenti
                    .Where(gr => gr.RecenzentID == recenzent.RecenzentID)
                    .SelectMany(gr => _context.PrijavaGrozdi.Where(pg => pg.GrozdID == gr.GrozdID))
                    .CountAsync();

                bool imaDovoljProstora = trenutnoSteviloDodeljenihPrijav < recenzent.SteviloProjektov;

                if (imaDovoljProstora)
                {
                    recenzentiZDovoljProstora.Add(recenzent);
                }

            }
            // Naključna izbira nadomestnega recenzenta
            var random = new Random();
            var nakljucniRecenzentIndex = random.Next(recenzentiZDovoljProstora.Count);
            var nakljucniRecenzent = recenzentiZDovoljProstora.ElementAtOrDefault(nakljucniRecenzentIndex);

            return nakljucniRecenzent;
        }
        
    }
}
