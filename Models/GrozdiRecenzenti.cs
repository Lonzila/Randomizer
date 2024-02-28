using System.ComponentModel.DataAnnotations;

namespace Randomizer.Models
{
    public class GrozdiRecenzenti
    {
        [Key]
        public int GrozdRecenzentID { get; set; }
        public int GrozdID { get; set; }
        public int RecenzentID { get; set; }
        public string Vloga { get; set; }

        // Navigacijske lastnosti
        public Grozdi Grozd { get; set; }
        public Recenzent Recenzent { get; set; }
    }
}
