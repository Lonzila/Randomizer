using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace Randomizer.Models
{
    public class Grozdi
    {
        [Key]
        public int GrozdID { get; set; }
        public int PodpodrocjeID { get; set; }

        // Navigacijske lastnosti
        public Podpodrocje Podpodrocje { get; set; }
        public ICollection<PrijavaGrozdi> PrijavaGrozdi { get; set; }
    }
}
