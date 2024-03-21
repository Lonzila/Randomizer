using System.ComponentModel.DataAnnotations;

namespace Randomizer.Models
{
    public class Podrocje
    {
        [Key]
        public int PodrocjeID { get; set; }
        public string Naziv { get; set; }
        public string Koda { get; set; }
        public string? AngNaziv { get; set; }

        // Navigacijske lastnosti
        public ICollection<Podpodrocje> Podpodrocja { get; set; }
    }
}
