namespace Randomizer.Models
{
    public class RecenzentiZavrnitve
    {
        public int ID { get; set; }
        public int RecenzentID { get; set; }
        public Recenzent Recenzent { get; set; }
        public int PrijavaID { get; set; }
        public Prijave Prijava { get; set; }
        public string Razlog { get; set; }
    }
}
