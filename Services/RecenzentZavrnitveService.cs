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
        private List<(int OriginalniRecenzentID, int NadomestniRecenzentID)> menjaveRecenzentov; 
        public RecenzentZavrnitveService(ApplicationDbContext context)
        {
            _context = context;
            menjaveRecenzentov = new List<(int, int)>();
        }

        public async Task ObdelajZavrnitveInDodeliNoveRecenzenteAsync()
        {
            var recenzentiMaxPrijav = await _context.Recenzenti
                .ToDictionaryAsync(r => r.RecenzentID, r => r.SteviloProjektov);

            var trenutnoSteviloPrijav = await _context.GrozdiRecenzenti
                .GroupBy(gr => gr.RecenzentID)
                .Select(g => new { RecenzentID = g.Key, Stevilo = g.Count() })
                .ToDictionaryAsync(g => g.RecenzentID, g => g.Stevilo);

            var trenutneVlogeRecenzentov = await _context.GrozdiRecenzenti
                .Where(gr => recenzentiMaxPrijav.Keys.Contains(gr.RecenzentID))
                .GroupBy(gr => gr.RecenzentID)
                .Select(g => new { RecenzentID = g.Key, Vloga = g.FirstOrDefault().Vloga })
                .ToDictionaryAsync(g => g.RecenzentID, g => g.Vloga ?? "");

            recenzentiStanje = recenzentiMaxPrijav.Keys.ToDictionary(
                recenzentID => recenzentID,
                recenzentID => (
                    trenutnoSteviloPrijav.ContainsKey(recenzentID) ? trenutnoSteviloPrijav[recenzentID] : 0,
                    recenzentiMaxPrijav[recenzentID],
                    trenutneVlogeRecenzentov.ContainsKey(recenzentID) ? trenutneVlogeRecenzentov[recenzentID] : ""
                )
            );

            var zavrnitve = await _context.RecenzentiZavrnitve.ToListAsync();

            // Skupinska obravnava zavrnitev po grozdu in recenzentu
            var grozdneZavrnitve = zavrnitve.GroupBy(z => new { z.RecenzentID, z.GrozdID });

            foreach (var skupina in grozdneZavrnitve)
            {
                int recenzentID = skupina.Key.RecenzentID;
                int grozdID = skupina.Key.GrozdID;

                // Pridobivanje prijav, ki potrebujejo zamenjavo v grozdu
                var prijaveVGrozdih = skupina
                    .Select(z => z.PrijavaID)
                    .ToList();

                var nadomestniRecenzent = await NajdiNadomestnegaRecenzentaAsync(grozdID, prijaveVGrozdih, recenzentID);

                if (nadomestniRecenzent == null) continue;

                foreach (var zavrnitev in skupina)
                {
                    var originalneDodelitve = await _context.GrozdiRecenzenti
                        .Where(gr => gr.GrozdID == grozdID && gr.RecenzentID == recenzentID && prijaveVGrozdih.Contains(gr.PrijavaID))
                        .ToListAsync();

                    if (!originalneDodelitve.Any())
                    {
                        Console.WriteLine($"RecenzentID {recenzentID} za GrozdID {grozdID} ni najden v tabeli GrozdiRecenzenti za prijave {string.Join(", ", prijaveVGrozdih)}.");
                        continue;
                    }

                    foreach (var dodelitev in originalneDodelitve)
                    {
                        dodelitev.RecenzentID = nadomestniRecenzent.RecenzentID;
                        _context.GrozdiRecenzenti.Update(dodelitev);

                        menjaveRecenzentov.Add((recenzentID, nadomestniRecenzent.RecenzentID));
                    }

                    if (recenzentiStanje.ContainsKey(recenzentID))
                    {
                        var trenutno = recenzentiStanje[recenzentID];
                        recenzentiStanje[recenzentID] = (trenutno.trenutnoSteviloPrijav - originalneDodelitve.Count, trenutno.maksimalnoSteviloPrijav, trenutno.vloga);
                    }

                    if (recenzentiStanje.ContainsKey(nadomestniRecenzent.RecenzentID))
                    {
                        var trenutno = recenzentiStanje[nadomestniRecenzent.RecenzentID];
                        var vlogaZavrnjenegaRecenzenta = recenzentiStanje[recenzentID].vloga;
                        recenzentiStanje[nadomestniRecenzent.RecenzentID] = (trenutno.trenutnoSteviloPrijav + originalneDodelitve.Count, trenutno.maksimalnoSteviloPrijav, vlogaZavrnjenegaRecenzenta);
                    }
                    Console.WriteLine($"Dodano: OriginalniRecenzentID = {recenzentID}, NadomestniRecenzentID = {nadomestniRecenzent.RecenzentID}");
                }
            }
            Console.WriteLine(menjaveRecenzentov);
            await _context.SaveChangesAsync();
        }
        public List<(int OriginalniRecenzentID, int NadomestniRecenzentID)> GetMenjaveRecenzentov()
        {
            Console.WriteLine($"Število menjav: {menjaveRecenzentov.Count}");
            return menjaveRecenzentov;
        }
        private async Task<Recenzent> NajdiNadomestnegaRecenzentaAsync(int grozdId, List<int> prijaveVGrozdih, int izkljuceniRecenzentId)
        {
            // Pridobitev podatkov o grozdu
            var izkljuceniRecenzenti = new List<int> { izkljuceniRecenzentId };

            // Zbiranje izključenih recenzentov iz celotnega grozda, ne posameznih prijav
            var zavrnitveGrozd = await _context.RecenzentiZavrnitve
                .Where(z => z.GrozdID == grozdId)
                .Select(z => z.RecenzentID)
                .ToListAsync();
            izkljuceniRecenzenti.AddRange(zavrnitveGrozd);

            var izloceniRecenzentiCOI = await _context.IzloceniCOI
                .Where(coi => prijaveVGrozdih.Contains(coi.PrijavaID))
                .Select(coi => coi.RecenzentID)
                .ToListAsync();
            izkljuceniRecenzenti.AddRange(izloceniRecenzentiCOI);

            var izloceniRecenzentiOsebni = await _context.IzloceniOsebni
                .Where(osebni => prijaveVGrozdih.Contains(osebni.PrijavaID))
                .Select(osebni => osebni.RecenzentID)
                .ToListAsync();
            izkljuceniRecenzenti.AddRange(izloceniRecenzentiOsebni);

            // Pridobivanje podpodročja grozda
            var podpodrocjeId = (await _context.Grozdi.FindAsync(grozdId))?.PodpodrocjeID;

            if (!podpodrocjeId.HasValue) return null;

            // Filtriranje potencialnih recenzentov glede na podpodročje in izključitve
            var potencialniRecenzenti = await _context.Recenzenti
                .Where(r => !izkljuceniRecenzenti.Contains(r.RecenzentID) &&
                            r.OdpovedPredDolocitvijo != true &&
                            _context.RecenzentiPodrocja.Any(rp => rp.RecenzentID == r.RecenzentID && rp.PodpodrocjeID == podpodrocjeId))
                .ToListAsync();

            // Pridobivanje partnerskih agencij iz vseh prijav v grozdu
            var prijave = await _context.Prijave
                .Where(p => prijaveVGrozdih.Contains(p.PrijavaID))
                .Select(p => new { p.PartnerskaAgencija1, p.PartnerskaAgencija2 })
                .ToListAsync();

            var partnerskeAgencijeKode = prijave
                .SelectMany(p => new List<string> { p.PartnerskaAgencija1, p.PartnerskaAgencija2 })
                .Where(k => !string.IsNullOrEmpty(k))
                .Distinct()
                .ToList();

            // Pretvorba kod partnerskih agencij v države
            var partnerskeAgencijeDrzave = new List<string>();
            foreach (var kod in partnerskeAgencijeKode)
            {
                var drzava = PartnerskaAgencijaDrzavaMap.PretvoriVDrzavo(kod);
                if (drzava != null && !partnerskeAgencijeDrzave.Contains(drzava))
                {
                    partnerskeAgencijeDrzave.Add(drzava);
                }
            }

            // Filtriranje potencialnih recenzentov glede na države partnerskih agencij
            potencialniRecenzenti = potencialniRecenzenti
                .Where(r => !partnerskeAgencijeDrzave.Contains(r.Drzava))
                .ToList();

            // Določitev vloge zavrnjenega recenzenta
            var vlogaZavrnjenegaRecenzenta = recenzentiStanje.ContainsKey(izkljuceniRecenzentId) ? recenzentiStanje[izkljuceniRecenzentId].vloga : null;

            // Filtriranje recenzentov glede na prostor in vlogo
            var skupnoSteviloPrijavVGrozdu = prijaveVGrozdih.Count;
            
            var recenzentiZDovoljProstoraInVlogo = potencialniRecenzenti
                .Where(r => recenzentiStanje.ContainsKey(r.RecenzentID) &&
                            recenzentiStanje[r.RecenzentID].trenutnoSteviloPrijav + skupnoSteviloPrijavVGrozdu <= recenzentiStanje[r.RecenzentID].maksimalnoSteviloPrijav &&
                            (string.IsNullOrEmpty(recenzentiStanje[r.RecenzentID].vloga) || recenzentiStanje[r.RecenzentID].vloga == vlogaZavrnjenegaRecenzenta))
                .ToList();


            // Ločevanje recenzentov na tiste, ki že imajo prijave, in tiste, ki jih nimajo
            var recenzentiZObstojecimiPrijavami = recenzentiZDovoljProstoraInVlogo
                .Where(r => _context.GrozdiRecenzenti.Any(gr => gr.RecenzentID == r.RecenzentID))
                .ToList();

            var recenzentiBrezPrijav = recenzentiZDovoljProstoraInVlogo
                .Where(r => !_context.GrozdiRecenzenti.Any(gr => gr.RecenzentID == r.RecenzentID))
                .ToList();

            // Poskus izbire nadomestnega recenzenta iz recenzentov, ki že imajo prijave
            if (recenzentiZObstojecimiPrijavami.Any())
            {
                //Console.WriteLine("Vrnilo je recenzenta z prijavami");
                var random = new Random();
                var nakljucniRecenzentIndex = random.Next(recenzentiZObstojecimiPrijavami.Count);
                //Console.WriteLine("Število prijav, ki jih že ima: " + recenzentiStanje[recenzentiZObstojecimiPrijavami[nakljucniRecenzentIndex].RecenzentID].trenutnoSteviloPrijav + "Število prijav, ki jih bo sprejel: " + skupnoSteviloPrijavVGrozdu);
                return recenzentiZObstojecimiPrijavami[nakljucniRecenzentIndex];
            }

            // Če ni recenzentov z obstoječimi prijavami, poskus izbire iz preostalih recenzentov
            if (recenzentiBrezPrijav.Any())
            {
                //Console.WriteLine("Vrnilo je recenzenta brez prijav");
                var random = new Random();
                var nakljucniRecenzentIndex = random.Next(recenzentiBrezPrijav.Count);
                return recenzentiBrezPrijav[nakljucniRecenzentIndex];
            }

            // Če ni več recenzentov za zamenjavo
            Console.WriteLine("Ni več recenzentov za zamenjavo");
            return null;
        }
    }
}
