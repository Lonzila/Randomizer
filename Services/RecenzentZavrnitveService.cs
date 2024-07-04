namespace Randomizer.Services
{
    using Randomizer.Data;
    using Randomizer.Helpers;
    using Microsoft.EntityFrameworkCore;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Randomizer.Models;

    public class RecenzentZavrnitveService
    {
        private readonly ApplicationDbContext _context;
        private Dictionary<int, (int trenutnoSteviloPrijav, int? maksimalnoSteviloPrijav, string vloga)> _recenzentiStanje;
        private List<(int OriginalniRecenzentID, int NadomestniRecenzentID)> _menjaveRecenzentov;
        private List<MenjavaRecenzentaViewModel> _menjaveRecenzentovPrikaz;

        public RecenzentZavrnitveService(ApplicationDbContext context)
        {
            _context = context;
            _menjaveRecenzentov = new List<(int, int)>();
            _menjaveRecenzentovPrikaz = new List<MenjavaRecenzentaViewModel>();
        }

        public async Task ObdelajZavrnitveInDodeliNoveRecenzenteAsync()
        {
            _recenzentiStanje = await RecenzentHelper.InicializirajRecenzenteStanjeAsync(_context);

            var zavrnitve = await _context.RecenzentiZavrnitve.ToListAsync();
            var grozdneZavrnitve = zavrnitve.GroupBy(z => new { z.RecenzentID, z.GrozdID });

            foreach (var skupina in grozdneZavrnitve)
            {
                int recenzentID = skupina.Key.RecenzentID;
                int grozdID = skupina.Key.GrozdID;

                var prijaveVGrozdih = skupina.Select(z => z.PrijavaID).ToList();
                var nadomestniRecenzent = await NajdiNadomestnegaRecenzentaAsync(grozdID, prijaveVGrozdih, recenzentID);

                if (nadomestniRecenzent == null) continue;

                var originalneDodelitve = await _context.GrozdiRecenzenti
                    .Where(gr => gr.GrozdID == grozdID && gr.RecenzentID == recenzentID && prijaveVGrozdih.Contains(gr.PrijavaID))
                    .ToListAsync();

                foreach (var zavrnitev in skupina)
                {
                    if (!originalneDodelitve.Any())
                    {
                        // Če ni originalnih dodelitev, preskočimo to zavrnitev
                        continue;
                    }

                    foreach (var dodelitev in originalneDodelitve)
                    {
                        dodelitev.RecenzentID = nadomestniRecenzent.RecenzentID;
                        _context.GrozdiRecenzenti.Update(dodelitev);

                        PosodobiRecenzentStanje(recenzentID, -1);
                        PosodobiRecenzentStanje(nadomestniRecenzent.RecenzentID, 1, _recenzentiStanje[recenzentID].vloga);

                        _menjaveRecenzentov.Add((recenzentID, nadomestniRecenzent.RecenzentID));
                        _menjaveRecenzentovPrikaz.Add(new MenjavaRecenzentaViewModel
                        {
                            OriginalniRecenzentID = recenzentID,
                            NadomestniRecenzentID = nadomestniRecenzent.RecenzentID,
                            PrijavaID = dodelitev.PrijavaID
                        });
                    }
                }
            }

            await _context.SaveChangesAsync();
        }


        private void PosodobiRecenzentStanje(int recenzentID, int sprememba, string novaVloga = null)
        {
            if (_recenzentiStanje.ContainsKey(recenzentID))
            {
                var trenutno = _recenzentiStanje[recenzentID];
                _recenzentiStanje[recenzentID] = (
                    trenutno.trenutnoSteviloPrijav + sprememba,
                    trenutno.maksimalnoSteviloPrijav,
                    novaVloga ?? trenutno.vloga
                );
            }
        }

        private async Task<Recenzent> NajdiNadomestnegaRecenzentaAsync(int grozdId, List<int> prijaveVGrozdih, int izkljuceniRecenzentId)
        {
            var izkljuceniRecenzenti = await PridobiIzkljuceneRecenzente(grozdId, prijaveVGrozdih, izkljuceniRecenzentId);
            var podpodrocjeId = (await _context.Grozdi.FindAsync(grozdId))?.PodpodrocjeID;
            if (!podpodrocjeId.HasValue) return null;

            var potencialniRecenzenti = await _context.Recenzenti
                .Where(r => !izkljuceniRecenzenti.Contains(r.RecenzentID) &&
                            r.OdpovedPredDolocitvijo != true &&
                            _context.RecenzentiPodrocja.Any(rp => rp.RecenzentID == r.RecenzentID && rp.PodpodrocjeID == podpodrocjeId))
                .ToListAsync();

            var partnerskeAgencijeDrzave = await PridobiPartnerskeAgencijeDrzave(prijaveVGrozdih);
            potencialniRecenzenti = potencialniRecenzenti.Where(r => !partnerskeAgencijeDrzave.Contains(r.Drzava)).ToList();

            var vlogaZavrnjenegaRecenzenta = _recenzentiStanje.ContainsKey(izkljuceniRecenzentId) ? _recenzentiStanje[izkljuceniRecenzentId].vloga : null;
            var skupnoSteviloPrijavVGrozdu = prijaveVGrozdih.Count;

            var recenzentiZDovoljProstoraInVlogo = potencialniRecenzenti
                .Where(r => _recenzentiStanje.ContainsKey(r.RecenzentID) &&
                            _recenzentiStanje[r.RecenzentID].trenutnoSteviloPrijav + skupnoSteviloPrijavVGrozdu <= _recenzentiStanje[r.RecenzentID].maksimalnoSteviloPrijav &&
                            (string.IsNullOrEmpty(_recenzentiStanje[r.RecenzentID].vloga) || _recenzentiStanje[r.RecenzentID].vloga == vlogaZavrnjenegaRecenzenta))
                .ToList();

            return IzberiNakljucnegaRecenzenta(recenzentiZDovoljProstoraInVlogo);
        }

        private async Task<List<int>> PridobiIzkljuceneRecenzente(int grozdId, List<int> prijaveVGrozdih, int izkljuceniRecenzentId)
        {
            var izkljuceniRecenzenti = new List<int> { izkljuceniRecenzentId };

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

            return izkljuceniRecenzenti;
        }

        private async Task<List<string>> PridobiPartnerskeAgencijeDrzave(List<int> prijaveVGrozdih)
        {
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

            return partnerskeAgencijeDrzave;
        }

        private Recenzent IzberiNakljucnegaRecenzenta(List<Recenzent> recenzenti)
        {
            var random = new Random();
            return recenzenti.OrderBy(x => random.Next()).FirstOrDefault();
        }

        public List<MenjavaRecenzentaViewModel> PridobiMenjaveRecenzentov()
        {
            return _menjaveRecenzentovPrikaz;
        }

        public List<(int OriginalniRecenzentID, int NadomestniRecenzentID)> GetMenjaveRecenzentov()
        {
            Console.WriteLine($"Število menjav: {_menjaveRecenzentov.Count}");
            return _menjaveRecenzentov;
        }
    }
}
