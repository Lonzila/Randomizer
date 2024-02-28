using System.ComponentModel.DataAnnotations;

namespace Randomizer.Models
{
    public class Podpodrocje
    {
        [Key]
        public int PodpodrocjeID { get; set; }
        public int PodrocjeID { get; set; }
        public string Naziv { get; set; }
        public string Koda { get; set; }

        // Navigacijske lastnosti
        public Podrocje Podrocje { get; set; }
        public ICollection<Grozdi> Grozdi { get; set; }
    }
}
