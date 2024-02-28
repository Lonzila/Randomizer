using System.ComponentModel.DataAnnotations;

namespace Randomizer.Models
{
    public class PrijavaGrozdi
    {
        [Key]
        public int PrijavaGrozdiID { get; set; }
        public int PrijavaID { get; set; }
        public int GrozdID { get; set; }

        // Navigacijske lastnosti
        public Prijave Prijava { get; set; }
        public Grozdi Grozd { get; set; }
    }
}
