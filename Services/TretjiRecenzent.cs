using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Randomizer.Data;
using Randomizer.Models;
using Randomizer.Helpers;

namespace Randomizer.Services
{
    public class TretjiRecenzentService
    {
        private readonly ApplicationDbContext _context;
        private Dictionary<int, (int trenutnoSteviloPrijav, int? maksimalnoSteviloPrijav, string vloga)> _recenzentiStanje;

        public TretjiRecenzentService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task DodeliTretjegaRecenzentaAsync(List<int> prijavaIDs)
        {
            _recenzentiStanje = await RecenzentHelper.InicializirajRecenzenteStanjeAsync(_context);

            var prijave = await _context.Prijave
                .Where(p => prijavaIDs.Contains(p.PrijavaID))
                .ToListAsync();

            var dodelitve = new List<(int PrijavaID, int StevilkaPrijave, int RecenzentID, int SifraRecenzenta)>();

            foreach (var prijava in prijave)
            {
                var obstojeceDodelitve = await _context.GrozdiRecenzenti
                    .Where(gr => gr.PrijavaID == prijava.PrijavaID)
                    .ToListAsync();

                if (obstojeceDodelitve.Count == 2)
                {
                    var tretjiRecenzent = await IzberiTretjegaRecenzentaAsync(prijava);

                    if (tretjiRecenzent != null)
                    {
                        DodeliRecenzentaPrijava(tretjiRecenzent, obstojeceDodelitve[0].GrozdID, prijava.PrijavaID, "Recenzent", 1);
                        dodelitve.Add((prijava.PrijavaID, prijava.StevilkaPrijave, tretjiRecenzent.RecenzentID, tretjiRecenzent.Sifra));
                    }
                }
            }

            await _context.SaveChangesAsync();
            IzpisiInformacijeODodelitvah(dodelitve);
        }

      
        private async Task<Recenzent> IzberiTretjegaRecenzentaAsync(Prijave prijava)
        {
            var izkljuceniRecenzenti = await PridobiIzkljuceneRecenzente(prijava.PrijavaID);

            var partnerskeAgencijeDrzave = await PridobiPartnerskeAgencijeDrzave(prijava);

            var potencialniRecenzenti = await _context.Recenzenti
                .Where(r => !izkljuceniRecenzenti.Contains(r.RecenzentID) &&
                            r.OdpovedPredDolocitvijo != true &&
                            !partnerskeAgencijeDrzave.Contains(r.Drzava) &&
                            _context.RecenzentiPodrocja.Any(rp => rp.RecenzentID == r.RecenzentID && rp.PodpodrocjeID == prijava.PodpodrocjeID))
                .ToListAsync();

            potencialniRecenzenti = FiltrirajRecenzentePoVlogiInProstoru(potencialniRecenzenti, prijava.PrijavaID);

            return IzberiNakljucnegaRecenzenta(potencialniRecenzenti);
        }

        private async Task<List<int>> PridobiIzkljuceneRecenzente(int prijavaID)
        {
            var izkljuceniRecenzenti = new List<int>();

            var izloceniRecenzentiCOI = await _context.IzloceniCOI
                .Where(coi => coi.PrijavaID == prijavaID)
                .Select(coi => coi.RecenzentID)
                .ToListAsync();
            izkljuceniRecenzenti.AddRange(izloceniRecenzentiCOI);

            var izloceniRecenzentiOsebni = await _context.IzloceniOsebni
                .Where(osebni => osebni.PrijavaID == prijavaID)
                .Select(osebni => osebni.RecenzentID)
                .ToListAsync();
            izkljuceniRecenzenti.AddRange(izloceniRecenzentiOsebni);

            var izloceniRecenzentiZavrnitve = await _context.RecenzentiZavrnitve
                .Where(rz => rz.PrijavaID == prijavaID)
                .Select(rz => rz.RecenzentID)
                .ToListAsync();
            izkljuceniRecenzenti.AddRange(izloceniRecenzentiZavrnitve);

            return izkljuceniRecenzenti;
        }

        private async Task<List<string>> PridobiPartnerskeAgencijeDrzave(Prijave prijava)
        {
            var partnerskeAgencijeKode = new List<string>
            {
                prijava.PartnerskaAgencija1,
                prijava.PartnerskaAgencija2
            }.Where(k => !string.IsNullOrEmpty(k)).Distinct().ToList();

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

        private List<Recenzent> FiltrirajRecenzentePoVlogiInProstoru(List<Recenzent> potencialniRecenzenti, int prijavaID)
        {
            var dodeljeniRecenzenti = _context.GrozdiRecenzenti
                .Where(gr => gr.PrijavaID == prijavaID)
                .Select(gr => gr.RecenzentID)
                .ToList();

            potencialniRecenzenti = potencialniRecenzenti
                .Where(r => !dodeljeniRecenzenti.Contains(r.RecenzentID) &&
                            _recenzentiStanje.ContainsKey(r.RecenzentID) &&
                            _recenzentiStanje[r.RecenzentID].vloga != "Poročevalec" &&
                            _recenzentiStanje[r.RecenzentID].trenutnoSteviloPrijav + 1 <= _recenzentiStanje[r.RecenzentID].maksimalnoSteviloPrijav)
                .ToList();

            return potencialniRecenzenti;
        }

        private Recenzent IzberiNakljucnegaRecenzenta(List<Recenzent> recenzenti)
        {
            var random = new Random();
            return recenzenti.OrderBy(x => random.Next()).FirstOrDefault();
        }

        private void DodeliRecenzentaPrijava(Recenzent recenzent, int grozdID, int prijavaID, string vloga, int steviloPrijav)
        {
            var dodelitev = new GrozdiRecenzenti
            {
                GrozdID = grozdID,
                PrijavaID = prijavaID,
                RecenzentID = recenzent.RecenzentID,
                Vloga = vloga
            };
            _context.GrozdiRecenzenti.Add(dodelitev);

            if (_recenzentiStanje.ContainsKey(recenzent.RecenzentID))
            {
                var trenutno = _recenzentiStanje[recenzent.RecenzentID];
                _recenzentiStanje[recenzent.RecenzentID] = (trenutno.trenutnoSteviloPrijav + steviloPrijav, trenutno.maksimalnoSteviloPrijav, vloga);
            }
        }

        private void IzpisiInformacijeODodelitvah(List<(int PrijavaID, int StevilkaPrijave, int RecenzentID, int SifraRecenzenta)> dodelitve)
        {
            foreach (var dodelitev in dodelitve)
            {
                Console.WriteLine($"Recenzent ID: {dodelitev.RecenzentID}, Šifra: {dodelitev.SifraRecenzenta} je bil dodeljen prijavi ID: {dodelitev.PrijavaID}, Številka prijave: {dodelitev.StevilkaPrijave}");
            }
        }
    }
}
