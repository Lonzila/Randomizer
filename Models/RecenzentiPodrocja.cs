using System.ComponentModel.DataAnnotations;

namespace Randomizer.Models
{
    public class RecenzentiPodrocja
    {
        [Key]
        public int ID { get; set; }
        public int RecenzentID { get; set; }
        public int PodrocjeID { get; set; }
        public int PodpodrocjeID { get; set; }

        // Navigacijske lastnosti
        public Recenzent Recenzent { get; set; }
        public Podrocje Podrocje { get; set; }
        public Podpodrocje Podpodrocje { get; set; }
    }
}
