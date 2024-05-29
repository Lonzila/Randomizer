using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Randomizer.Models
{
    public class GrozdiRecenzentiZavrnitve
    {
        [Key]
        public int ID { get; set; }

        [Required]
        [ForeignKey("Grozd")]
        public int GrozdID { get; set; }

        [Required]
        [ForeignKey("Recenzent")]
        public int RecenzentID { get; set; }

        // Navigacijske lastnosti
        public virtual Grozdi Grozd { get; set; }
        public virtual Recenzent Recenzent { get; set; }
    }
}
