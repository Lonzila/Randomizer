using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Randomizer.Data;
using Randomizer.Models;

namespace Randomizer.Services
{
    public class TretjiRecenzentService
    {
        private readonly ApplicationDbContext _context;
        private Dictionary<int, (int TrenutnoSteviloPrijav, int? MaksimalnoSteviloPrijav, string Vloga)> recenzentiStanje;

        public TretjiRecenzentService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task DodeliTretjegaRecenzentaAsync(List<int> prijavaIDs)
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

            // Pridobi vse prijave, ki ustrezajo podanim ID-jem prijav
            var prijave = await _context.Prijave
                .Where(p => prijavaIDs.Contains(p.PrijavaID))
                .ToListAsync();

            var dodelitve = new List<(int PrijavaID, int StevilkaPrijave, int RecenzentID, int SifraRecenzenta)>();

            foreach (var prijava in prijave)
            {
                // Preveri, ali prijava že ima dva recenzenta
                var obstojeceDodelitve = await _context.GrozdiRecenzenti
                    .Where(gr => gr.PrijavaID == prijava.PrijavaID)
                    .ToListAsync();

                if (obstojeceDodelitve.Count == 2)
                {
                    // Izberi tretjega recenzenta
                    var tretjiRecenzent = await IzberiTretjegaRecenzentaAsync(prijava);

                    // Dodeli tretjega recenzenta prijavi
                    if (tretjiRecenzent != null)
                    {
                        DodeliRecenzentaPrijava(tretjiRecenzent, obstojeceDodelitve[0].GrozdID, prijava.PrijavaID, "Recenzent", 1);

                        // Dodaj informacije o dodelitvi v seznam
                        dodelitve.Add((prijava.PrijavaID, prijava.StevilkaPrijave, tretjiRecenzent.RecenzentID, tretjiRecenzent.Sifra));
                    }
                }
            }

            await _context.SaveChangesAsync();

            // Izpis informacij o dodelitvah
            foreach (var dodelitev in dodelitve)
            {
                Console.WriteLine($"Recenzent ID: {dodelitev.RecenzentID}, Šifra: {dodelitev.SifraRecenzenta} je bil dodeljen prijavi ID: {dodelitev.PrijavaID}, Številka prijave: {dodelitev.StevilkaPrijave}");
            }
        }
        private async Task<Recenzent> IzberiTretjegaRecenzentaAsync(Prijave prijava)
        {
            var izkljuceniRecenzenti = new List<int>();

            // Pridobivanje izključenih recenzentov zaradi konflikta interesov
            var izloceniRecenzentiCOI = await _context.IzloceniCOI
                .Where(coi => coi.PrijavaID == prijava.PrijavaID)
                .Select(coi => coi.RecenzentID)
                .ToListAsync();
            izkljuceniRecenzenti.AddRange(izloceniRecenzentiCOI);

            // Pridobivanje izključenih recenzentov zaradi osebnih razlogov
            var izloceniRecenzentiOsebni = await _context.IzloceniOsebni
                .Where(osebni => osebni.PrijavaID == prijava.PrijavaID)
                .Select(osebni => osebni.RecenzentID)
                .ToListAsync();
            izkljuceniRecenzenti.AddRange(izloceniRecenzentiOsebni);

            // Pridobivanje recenzentov, ki so že zavrnili prijavo
            var izloceniRecenzentiZavrnitve = await _context.RecenzentiZavrnitve
                .Where(rz => rz.PrijavaID == prijava.PrijavaID)
                .Select(rz => rz.RecenzentID)
                .ToListAsync();
            izkljuceniRecenzenti.AddRange(izloceniRecenzentiZavrnitve);

            // Pridobivanje partnerskih agencij iz prijave
            var partnerskeAgencijeKode = new List<string>
            {
                prijava.PartnerskaAgencija1,
                prijava.PartnerskaAgencija2
            }.Where(k => !string.IsNullOrEmpty(k)).Distinct().ToList();

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

            // Pridobivanje podpodročja prijave
            var podpodrocjeId = prijava.PodpodrocjeID;

            // Filtriranje potencialnih recenzentov glede na podpodročje, izključitve in države partnerskih agencij
            var potencialniRecenzenti = await _context.Recenzenti
                .Where(r => !izkljuceniRecenzenti.Contains(r.RecenzentID) &&
                            r.OdpovedPredDolocitvijo != true &&
                            !partnerskeAgencijeDrzave.Contains(r.Drzava) &&
                            _context.RecenzentiPodrocja.Any(rp => rp.RecenzentID == r.RecenzentID && rp.PodpodrocjeID == podpodrocjeId))
                .ToListAsync();

            // Izločitev recenzentov, ki že imajo dodeljeno vlogo "Poročevalec"
            potencialniRecenzenti = potencialniRecenzenti
                .Where(r => recenzentiStanje.ContainsKey(r.RecenzentID) && recenzentiStanje[r.RecenzentID].Vloga != "Poročevalec")
                .ToList();

            // Izločitev recenzentov, ki so že dodeljeni tej prijavi
            var dodeljeniRecenzenti = await _context.GrozdiRecenzenti
                .Where(gr => gr.PrijavaID == prijava.PrijavaID)
                .Select(gr => gr.RecenzentID)
                .ToListAsync();

            potencialniRecenzenti = potencialniRecenzenti
                .Where(r => !dodeljeniRecenzenti.Contains(r.RecenzentID))
                .ToList();

            // Preverjanje, ali imajo potencialni recenzenti dovolj prostora za dodatno prijavo
            potencialniRecenzenti = potencialniRecenzenti
                .Where(r => recenzentiStanje.ContainsKey(r.RecenzentID) &&
                            recenzentiStanje[r.RecenzentID].TrenutnoSteviloPrijav + 1 <= recenzentiStanje[r.RecenzentID].MaksimalnoSteviloPrijav)
                .ToList();

            // Izberi recenzenta, ki še ni dodeljen tej prijavi in ustreza pogojem
            var random = new Random();
            var izbranRecenzent = potencialniRecenzenti.OrderBy(x => random.Next()).FirstOrDefault();

            return izbranRecenzent;
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

            // Posodobite število dodeljenih prijav za recenzenta v slovarju
            if (recenzentiStanje.ContainsKey(recenzent.RecenzentID))
            {
                var trenutno = recenzentiStanje[recenzent.RecenzentID];
                recenzentiStanje[recenzent.RecenzentID] = (trenutno.TrenutnoSteviloPrijav + steviloPrijav, trenutno.MaksimalnoSteviloPrijav, vloga);
            }
        }
    }
}
