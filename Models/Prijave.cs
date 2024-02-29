using System.ComponentModel.DataAnnotations;

namespace Randomizer.Models
{
    public class Prijave
    {
        [Key]
        public int PrijavaID { get; set; }
        public string StevilkaPrijave { get; set; }
        public string VrstaProjekta { get; set; }
        public string Drzava { get; set; }
        public int? PodrocjeID { get; set; }
        public int? PodpodrocjeID { get; set; }
        public int? DodatnoPodrocjeID { get; set; }
        public int? DodatnoPodpodrocjeID { get; set; }
        public string Naslov { get; set; }
        public int SteviloRecenzentov { get; set; }
        public bool? Interdisc { get; set; }
        public string? PartnerskaAgencija1 { get; set; }
        public string? PartnerskaAgencija2 { get; set; }
         
        // Navigacijske lastnosti
        public Podrocje Podrocje { get; set; }
        public Podpodrocje Podpodrocje { get; set; }
        public Podrocje? DodatnoPodrocje { get; set; }
        public Podpodrocje? DodatnoPodpodrocje { get; set; }
    }
}
