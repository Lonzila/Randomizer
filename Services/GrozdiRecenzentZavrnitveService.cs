namespace Randomizer.Services
{
    using Randomizer.Data;
    using Randomizer.Helpers;
    using Microsoft.EntityFrameworkCore;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Randomizer.Models;

    public class GrozdiRecenzentZavrnitveService
    {
        private readonly ApplicationDbContext _context;
        private Dictionary<int, (int trenutnoSteviloPrijav, int? maksimalnoSteviloPrijav, string vloga)> _recenzentiStanje;

        public GrozdiRecenzentZavrnitveService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task ObdelajZavrnitveInDodeliNoveRecenzenteAsync()
        {
            _recenzentiStanje = await RecenzentHelper.InicializirajRecenzenteStanjeAsync(_context);

            var zavrnitve = await _context.GrozdiRecenzentiZavrnitve.ToListAsync();
            foreach (var zavrnitev in zavrnitve)
            {
                var prijaveVGrozdih = await PridobiPrijaveVGrozdihAsync(zavrnitev.GrozdID);

                var potencialniNadomestniRecenzent = await NajdiNadomestnegaRecenzentaAsync(zavrnitev.GrozdID, prijaveVGrozdih, zavrnitev.RecenzentID);

                if (potencialniNadomestniRecenzent != null)
                {
                    await ZamenjajRecenzentaAsync(zavrnitev.GrozdID, zavrnitev.RecenzentID, potencialniNadomestniRecenzent.RecenzentID, prijaveVGrozdih);
                }
            }

            await _context.SaveChangesAsync();
        }

       
        private async Task<List<int>> PridobiPrijaveVGrozdihAsync(int grozdID)
        {
            return await _context.PrijavaGrozdi
                .Where(pg => pg.GrozdID == grozdID)
                .Select(pg => pg.PrijavaID)
                .ToListAsync();
        }

        private async Task<Recenzent> NajdiNadomestnegaRecenzentaAsync(int grozdId, List<int> prijaveVGrozdih, int izkljuceniRecenzentId)
        {
            var izkljuceniRecenzenti = new List<int> { izkljuceniRecenzentId };
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

            var podpodrocjeId = (await _context.Grozdi.FindAsync(grozdId))?.PodpodrocjeID;
            if (!podpodrocjeId.HasValue) return null;

            var potencialniRecenzenti = await _context.Recenzenti
                .Where(r => !izkljuceniRecenzenti.Contains(r.RecenzentID) &&
                            r.OdpovedPredDolocitvijo != true &&
                            _context.RecenzentiPodrocja.Any(rp => rp.RecenzentID == r.RecenzentID && rp.PodpodrocjeID == podpodrocjeId))
                .ToListAsync();

            var prijave = await _context.Prijave
                 .Where(p => prijaveVGrozdih.Contains(p.PrijavaID))
                 .Select(p => new { p.PartnerskaAgencija1, p.PartnerskaAgencija2 })
                 .ToListAsync();

            var partnerskeAgencijeKode = prijave
                .SelectMany(p => new List<string> { p.PartnerskaAgencija1, p.PartnerskaAgencija2 })
                .Where(k => !string.IsNullOrEmpty(k))
                .Distinct()
                .ToList();

            var partnerskeAgencijeDrzave = new List<string>();
            foreach (var kod in partnerskeAgencijeKode)
            {
                var drzava = PartnerskaAgencijaDrzavaMap.PretvoriVDrzavo(kod);
                if (drzava != null && !partnerskeAgencijeDrzave.Contains(drzava))
                {
                    partnerskeAgencijeDrzave.Add(drzava);
                }
            }

            potencialniRecenzenti = potencialniRecenzenti
                .Where(r => !partnerskeAgencijeDrzave.Contains(r.Drzava))
                .ToList();

            var skupnoSteviloPrijavVGrozdu = prijaveVGrozdih.Count;
            var recenzentiZDovoljProstoraInVlogo = potencialniRecenzenti
                .Where(r => _recenzentiStanje.ContainsKey(r.RecenzentID) &&
                            _recenzentiStanje[r.RecenzentID].trenutnoSteviloPrijav + skupnoSteviloPrijavVGrozdu <= _recenzentiStanje[r.RecenzentID].maksimalnoSteviloPrijav &&
                            (string.IsNullOrEmpty(_recenzentiStanje[r.RecenzentID].vloga) || _recenzentiStanje[r.RecenzentID].vloga == _recenzentiStanje[izkljuceniRecenzentId].vloga))
                .ToList();

            var random = new Random();
            return recenzentiZDovoljProstoraInVlogo.OrderBy(x => random.Next()).FirstOrDefault();
        }

        private async Task ZamenjajRecenzentaAsync(int grozdID, int originalniRecenzentID, int nadomestniRecenzentID, List<int> prijaveVGrozdih)
        {
            var originalneDodelitve = await _context.GrozdiRecenzenti
                .Where(gr => gr.GrozdID == grozdID && gr.RecenzentID == originalniRecenzentID && prijaveVGrozdih.Contains(gr.PrijavaID))
                .ToListAsync();

            foreach (var dodelitev in originalneDodelitve)
            {
                dodelitev.RecenzentID = nadomestniRecenzentID;
                _context.GrozdiRecenzenti.Update(dodelitev);

                var trenutno = _recenzentiStanje[originalniRecenzentID];
                _recenzentiStanje[originalniRecenzentID] = (trenutno.trenutnoSteviloPrijav - 1, trenutno.maksimalnoSteviloPrijav, trenutno.vloga);

                trenutno = _recenzentiStanje[nadomestniRecenzentID];
                _recenzentiStanje[nadomestniRecenzentID] = (trenutno.trenutnoSteviloPrijav + 1, trenutno.maksimalnoSteviloPrijav, _recenzentiStanje[originalniRecenzentID].vloga);
            }
        }
    }
}
