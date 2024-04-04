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
        private Dictionary<int, (int trenutnoSteviloPrijav, int? maksimalnoSteviloPrijav, string vloga)> recenzentiStanje;
        public RecenzentZavrnitveService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task ObdelajZavrnitveInDodeliNoveRecenzenteAsync()
        {

            var recenzentiMaxPrijav = await _context.Recenzenti
                .ToDictionaryAsync(r => r.RecenzentID, r => r.SteviloProjektov);

            // Pridobivanje trenutnega števila dodeljenih prijav za vse recenzente
            var trenutnoSteviloPrijav = await _context.GrozdiRecenzenti
                .GroupBy(gr => gr.RecenzentID)
                .Select(g => new { RecenzentID = g.Key, Stevilo = g.Count() })
                .ToDictionaryAsync(g => g.RecenzentID, g => g.Stevilo);

            // Pridobivanje trenutnih vlog recenzentov
            var trenutneVlogeRecenzentov = await _context.GrozdiRecenzenti
                .Where(gr => recenzentiMaxPrijav.Keys.Contains(gr.RecenzentID))
                .GroupBy(gr => gr.RecenzentID)
                .Select(g => new { RecenzentID = g.Key, Vloga = g.FirstOrDefault().Vloga })
                .ToDictionaryAsync(g => g.RecenzentID, g => g.Vloga ?? "");

            // Inicializacija slovarja z dodajanjem informacij o trenutnih vlogah
            recenzentiStanje = recenzentiMaxPrijav.Keys.ToDictionary(
                recenzentID => recenzentID,
                recenzentID => (
                    trenutnoSteviloPrijav.ContainsKey(recenzentID) ? trenutnoSteviloPrijav[recenzentID] : 0,
                    recenzentiMaxPrijav[recenzentID],
                    trenutneVlogeRecenzentov.ContainsKey(recenzentID) ? trenutneVlogeRecenzentov[recenzentID] : ""
                )
            );

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

                        // Posodobitev slovarja za originalnega recenzenta, ki je zavrnil
                        if (recenzentiStanje.ContainsKey(zavrnitev.RecenzentID))
                        {
                            var trenutno = recenzentiStanje[zavrnitev.RecenzentID];
                            // Zmanjšaj trenutno število dodeljenih prijav za recenzenta, ki je zavrnil, brez spreminjanja njegove vloge
                            recenzentiStanje[zavrnitev.RecenzentID] = (trenutno.trenutnoSteviloPrijav - 1, trenutno.maksimalnoSteviloPrijav, trenutno.vloga);
                        }

                        // Posodobitev slovarja za nadomestnega recenzenta
                        if (recenzentiStanje.ContainsKey(nadomestniRecenzent.RecenzentID))
                        {
                            var trenutno = recenzentiStanje[nadomestniRecenzent.RecenzentID];
                            var vlogaZavrnjenegaRecenzenta = recenzentiStanje[zavrnitev.RecenzentID].vloga;
                            // Povečaj trenutno število dodeljenih prijav za nadomestnega recenzenta in nastavi vlogo
                            recenzentiStanje[nadomestniRecenzent.RecenzentID] = (trenutno.trenutnoSteviloPrijav + 1, trenutno.maksimalnoSteviloPrijav, vlogaZavrnjenegaRecenzenta);
                        }
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
            // Pridobitev podatkov o prijavi in grozdu
            var prijava = await _context.Prijave.FindAsync(prijavaId);
            var grozd = await _context.Grozdi.Include(g => g.Podpodrocje).FirstOrDefaultAsync(g => g.GrozdID == grozdId);

            // Izločitev recenzentov, ki so že zavrnili ali so izključeni zaradi konflikta interesov
            var izkljuceniRecenzenti = new List<int> { izkljuceniRecenzentId };
            var zavrnitve = await _context.RecenzentiZavrnitve
                                          .Where(z => z.PrijavaID == prijavaId)
                                          .Select(z => z.RecenzentID)
                                          .ToListAsync();
            izkljuceniRecenzenti.AddRange(zavrnitve);

            var izloceniRecenzentiCOI = await _context.IzloceniCOI
                                                      .Where(coi => coi.PrijavaID == prijavaId)
                                                      .Select(coi => coi.RecenzentID)
                                                      .ToListAsync();
            izkljuceniRecenzenti.AddRange(izloceniRecenzentiCOI);

            // Določitev vloge zavrnjenega recenzenta
            var vlogaZavrnjenegaRecenzenta = recenzentiStanje.ContainsKey(izkljuceniRecenzentId) ? recenzentiStanje[izkljuceniRecenzentId].vloga : null;

            // Filtriranje potencialnih recenzentov glede na podpodročje in izključitve
            var recenzentiPodpodrocja = await _context.RecenzentiPodrocja
                .Where(rp => rp.PodpodrocjeID == grozd.PodpodrocjeID && !izkljuceniRecenzenti.Contains(rp.RecenzentID))
                .Select(rp => rp.RecenzentID)
                .ToListAsync();

            var potencialniRecenzenti = await _context.Recenzenti
                .Where(r =>
                    recenzentiPodpodrocja.Contains(r.RecenzentID) &&
                    !izkljuceniRecenzenti.Contains(r.RecenzentID) &&
                    r.OdpovedPredDolocitvijo != true
                )
                .ToListAsync();

            // Upoštevanje partnerskih agencij in držav
            var partnerskeAgencijeKode = new List<string> { prijava.PartnerskaAgencija1, prijava.PartnerskaAgencija2 }
                                            .Where(k => !string.IsNullOrEmpty(k))
                                            .Distinct();
            var partnerskeAgencijeDrzave = partnerskeAgencijeKode
                                            .Select(koda => PartnerskaAgencijaDrzavaMap.PretvoriVDrzavo(koda))
                                            .ToList();
            potencialniRecenzenti = potencialniRecenzenti
                                        .Where(r => !partnerskeAgencijeDrzave.Contains(r.Drzava))
                                        .ToList();

            // Filtriranje recenzentov glede na prostor in vlogo
            var recenzentiZDovoljProstoraInVlogo = potencialniRecenzenti
                .Where(r => recenzentiStanje.ContainsKey(r.RecenzentID) &&
                            recenzentiStanje[r.RecenzentID].trenutnoSteviloPrijav < recenzentiStanje[r.RecenzentID].maksimalnoSteviloPrijav &&
                            (string.IsNullOrEmpty(recenzentiStanje[r.RecenzentID].vloga) || recenzentiStanje[r.RecenzentID].vloga == vlogaZavrnjenegaRecenzenta))
                .ToList();

            // Naključna izbira nadomestnega recenzenta
            var random = new Random();
            var nakljucniRecenzentIndex = random.Next(recenzentiZDovoljProstoraInVlogo.Count);
            var nakljucniRecenzent = recenzentiZDovoljProstoraInVlogo.ElementAtOrDefault(nakljucniRecenzentIndex);

            return nakljucniRecenzent;
        }


    }
}
