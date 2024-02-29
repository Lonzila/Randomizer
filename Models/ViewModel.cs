namespace Randomizer.Models
{
        public class GrozdiViewModel
    {
        public int GrozdID { get; set; }
        public string PodpodrocjeNaziv { get; set; }
        public List<PrijavaInfo> Prijave { get; set; }
        public List<RecenzentInfo> Recenzenti { get; set; }
    }

    public class PrijavaInfo
    {
        public int PrijavaID { get; set; }
        public string StevilkaPrijave { get; set; }
        public string Naslov { get; set; }
        public bool Interdisc { get; set; } // Dodano
        public int SteviloRecenzentov { get; set; } // Dodano
    }

    public class RecenzentInfo
    {
        public int RecenzentID { get; set; }
        public string Ime { get; set; }
        public string Priimek { get; set; }
        public List<string> Podpodrocja { get; set; } // Nazivi podpodročij
    }
}

