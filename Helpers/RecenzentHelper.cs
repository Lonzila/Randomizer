using Microsoft.EntityFrameworkCore;
using Randomizer.Data;

namespace Randomizer.Helpers
{
    public static class RecenzentHelper
    {
        public static async Task<Dictionary<int, (int trenutnoSteviloPrijav, int? maksimalnoSteviloPrijav, string vloga)>> InicializirajRecenzenteStanjeAsync(ApplicationDbContext context)
        {
            var recenzentiMaxPrijav = await context.Recenzenti.ToDictionaryAsync(r => r.RecenzentID, r => r.SteviloProjektov);
            var trenutnoSteviloPrijav = await context.GrozdiRecenzenti
                .GroupBy(gr => gr.RecenzentID)
                .Select(g => new { RecenzentID = g.Key, Stevilo = g.Count() })
                .ToDictionaryAsync(g => g.RecenzentID, g => g.Stevilo);

            var trenutneVlogeRecenzentov = await context.GrozdiRecenzenti
                .Where(gr => recenzentiMaxPrijav.Keys.Contains(gr.RecenzentID))
                .GroupBy(gr => gr.RecenzentID)
                .Select(g => new { RecenzentID = g.Key, Vloga = g.FirstOrDefault().Vloga })
                .ToDictionaryAsync(g => g.RecenzentID, g => g.Vloga ?? "");

            return recenzentiMaxPrijav.Keys.ToDictionary(
                recenzentID => recenzentID,
                recenzentID => (
                    trenutnoSteviloPrijav.ContainsKey(recenzentID) ? trenutnoSteviloPrijav[recenzentID] : 0,
                    recenzentiMaxPrijav[recenzentID],
                    trenutneVlogeRecenzentov.ContainsKey(recenzentID) ? trenutneVlogeRecenzentov[recenzentID] : ""
                )
            );
        }
    }
}

