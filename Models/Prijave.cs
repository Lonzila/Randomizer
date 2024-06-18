using System.ComponentModel.DataAnnotations;

namespace Randomizer.Models
{
    public class Prijave
    {
        [Key]
        public int PrijavaID { get; set; }
        public int StevilkaPrijave { get; set; }
        public string VrstaProjekta { get; set; }
        public int? PodpodrocjeID { get; set; }
        public int? DodatnoPodpodrocjeID { get; set; }
        public string Naslov { get; set; }
        public int SteviloRecenzentov { get; set; }
        public bool? Interdisc { get; set; }
        public string? PartnerskaAgencija1 { get; set; }
        public string? PartnerskaAgencija2 { get; set; }
        public string? AngNaslov { get; set; }
        public string? Vodja { get; set; }
        public string? SifraVodje { get; set; }
        public string? NazivRO { get; set; }
        public string? AngNazivRO { get; set; }
        public string? SifraRO { get; set; }
            
         
        // Navigacijske lastnosti
        public Podpodrocje Podpodrocje { get; set; }
        public Podpodrocje? DodatnoPodpodrocje { get; set; }
    }
}
