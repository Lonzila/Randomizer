namespace Randomizer.Helpers
{
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
            return KodaNaDrzavo.TryGetValue(kodaAgencije, out string drzava) ? drzava : null;
        }
    }
}