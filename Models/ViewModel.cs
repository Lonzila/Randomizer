namespace Randomizer.Models
{
    public class GrozdiViewModel
    {
        public int GrozdID { get; set; }
        public string PodpodrocjeNaziv { get; set; }
        public List<PrijavaViewModel> Prijave { get; set; }
    }

    public class PrijavaViewModel
    {
        public int PrijavaID { get; set; }
        public string StevilkaPrijave { get; set; }
        public string Naslov { get; set; }
        public bool Interdisc { get; set; }
        public int SteviloRecenzentov { get; set; }
        public string Podpodrocje { get; set; } 
        public string DodatnoPodpodrocje { get; set; }
        public List<RecenzentInfo> Recenzenti { get; set; }
    }

    public class RecenzentInfo
    {
        public int RecenzentID { get; set; }
        public string Ime { get; set; }
        public string Priimek { get; set; }
        public string Vloga { get; set; } 
    }

}

