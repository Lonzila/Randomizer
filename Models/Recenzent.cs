using System.ComponentModel.DataAnnotations;

namespace Randomizer.Models
{
    public class Recenzent
    {
        [Key]
        public int RecenzentID { get; set; }
        public int Sifra { get; set; }
        public string Ime { get; set; }
        public string Priimek { get; set; }
        public string EPosta { get; set; }
        public int? SteviloProjektov { get; set; }
        public string Drzava { get; set; }

        // Navigacijske lastnosti
        public ICollection<GrozdiRecenzenti> GrozdiRecenzenti { get; set; }
    }
}
