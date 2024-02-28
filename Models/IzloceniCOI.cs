using System.ComponentModel.DataAnnotations;

namespace Randomizer.Models
{
    public class IzloceniCOI
    {
        [Key]
        public int ID { get; set; }
        public int PrijavaID { get; set; }
        public int RecenzentID { get; set; }
        public string? Razlog { get; set; }

        // Navigacijske lastnosti
        public Prijave Prijava { get; set; }
        public Recenzent Recenzent { get; set; }
    }
}
