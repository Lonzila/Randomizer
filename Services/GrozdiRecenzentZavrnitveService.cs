namespace Randomizer.Services
{
    using Randomizer.Data;
    using Microsoft.EntityFrameworkCore;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Randomizer.Models;

    public class GrozdiRecenzentZavrnitveService
    {
        private readonly ApplicationDbContext _context;
        private Dictionary<int, (int trenutnoSteviloPrijav, int? maksimalnoSteviloPrijav, string vloga)> recenzentiStanje;
        
        public GrozdiRecenzentZavrnitveService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task ObdelajZavrnitveInDodeliNoveRecenzenteAsync2()
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

            var zavrnitve = await _context.GrozdiRecenzentiZavrnitve.ToListAsync();
            foreach (var zavrnitev in zavrnitve)
            {
                var prijaveVGrozdih = await _context.PrijavaGrozdi
                    .Where(pg => pg.GrozdID == zavrnitev.GrozdID)
                    .Select(pg => pg.PrijavaID)
                    .ToListAsync();

                var potencialniNadomestniRecenzent = await NajdiNadomestnegaRecenzentaAsync(zavrnitev.GrozdID, prijaveVGrozdih, zavrnitev.RecenzentID);

                if (potencialniNadomestniRecenzent != null)
                {
                    foreach (var prijavaId in prijaveVGrozdih)
                    {
                        var originalnaDodelitev = await _context.GrozdiRecenzenti
                            .FirstOrDefaultAsync(gr => gr.PrijavaID == prijavaId && gr.RecenzentID == zavrnitev.RecenzentID);

                        Console.WriteLine($"Iskanje grozdId: {zavrnitev.GrozdID}, PrijavaID: {prijavaId}, RecenzentID: {zavrnitev.RecenzentID}");
                        if (originalnaDodelitev != null)
                        {
                            Console.WriteLine("Gre skozi");
                            var trenutni = recenzentiStanje[zavrnitev.RecenzentID];
                            var nadomestni = recenzentiStanje[potencialniNadomestniRecenzent.RecenzentID];
                            recenzentiStanje[zavrnitev.RecenzentID] = (trenutni.trenutnoSteviloPrijav - 1, trenutni.maksimalnoSteviloPrijav, trenutni.vloga);
                            recenzentiStanje[potencialniNadomestniRecenzent.RecenzentID] = (nadomestni.trenutnoSteviloPrijav + 1, nadomestni.maksimalnoSteviloPrijav, trenutni.vloga);
                            originalnaDodelitev.RecenzentID = potencialniNadomestniRecenzent.RecenzentID;
                            _context.GrozdiRecenzenti.Update(originalnaDodelitev);
                        }
                        else
                        {
                            Console.WriteLine("Ne gre skozi");
                        }
                    }
                }
            }

            await _context.SaveChangesAsync();
        }
        private async Task<Recenzent> NajdiNadomestnegaRecenzentaAsync(int grozdId, List<int> prijaveVGrozdih, int izkljuceniRecenzentId)
        {
            // Pridobitev podatkov o grozdu
            var izkljuceniRecenzenti = new List<int> { izkljuceniRecenzentId };

            // Zbiranje izključenih recenzentov iz celotnega grozda, ne posameznih prijav
            var zavrnitveGrozd = await _context.GrozdiRecenzentiZavrnitve
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
            var originalniPotencialniRecenzenti = potencialniRecenzenti.ToList();
            potencialniRecenzenti = potencialniRecenzenti
                .Where(r => !partnerskeAgencijeDrzave.Contains(r.Drzava))
                .ToList();



            Console.WriteLine(izkljuceniRecenzentId);
            // Določitev vloge zavrnjenega recenzenta
            var vlogaZavrnjenegaRecenzenta = recenzentiStanje.ContainsKey(izkljuceniRecenzentId) ? recenzentiStanje[izkljuceniRecenzentId].vloga : null;

            // Filtriranje recenzentov glede na prostor in vlogo
            var skupnoSteviloPrijavVGrozdu = prijaveVGrozdih.Count;
            var recenzentiZDovoljProstoraInVlogo = potencialniRecenzenti
                .Where(r => recenzentiStanje.ContainsKey(r.RecenzentID) &&
                            recenzentiStanje[r.RecenzentID].trenutnoSteviloPrijav + skupnoSteviloPrijavVGrozdu <= recenzentiStanje[r.RecenzentID].maksimalnoSteviloPrijav &&
                            (string.IsNullOrEmpty(recenzentiStanje[r.RecenzentID].vloga) || recenzentiStanje[r.RecenzentID].vloga == vlogaZavrnjenegaRecenzenta))
                .ToList();

            foreach (var recenzent in recenzentiZDovoljProstoraInVlogo)
            {
                Console.WriteLine($"Recenzent ID: {recenzent.RecenzentID}, Trenutno število prijav: {recenzentiStanje[recenzent.RecenzentID].trenutnoSteviloPrijav}, Maksimalno število prijav: {recenzentiStanje[recenzent.RecenzentID].maksimalnoSteviloPrijav}, Št projektov v grozdu: {skupnoSteviloPrijavVGrozdu}");
            }

            // Naključna izbira nadomestnega recenzenta
            var random = new Random();
            var nakljucniRecenzentIndex = random.Next(recenzentiZDovoljProstoraInVlogo.Count);
            var nakljucniRecenzent = recenzentiZDovoljProstoraInVlogo.ElementAtOrDefault(nakljucniRecenzentIndex);
            if (nakljucniRecenzent == null)
            {
                Console.WriteLine("Ni več recenzentov za zamenjavo");
            }

            return nakljucniRecenzent;
        }
    }
}
