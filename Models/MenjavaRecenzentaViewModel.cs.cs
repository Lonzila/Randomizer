namespace Randomizer.Models
{
    public class MenjavaRecenzentaViewModel
    {
        public int OriginalniRecenzentID { get; set; }
        public int NadomestniRecenzentID { get; set; }
        public int PrijavaID { get; set; }
        public int OriginalniRecenzentSifra { get; set; }
        public int NadomestniRecenzentSifra { get; set; }
        public int StevilkaPrijave { get; set; }
        public string NadomestniRecenzentImePriimek { get; set; }

        public override bool Equals(object obj)
        {
            return obj is MenjavaRecenzentaViewModel model &&
                   OriginalniRecenzentID == model.OriginalniRecenzentID &&
                   NadomestniRecenzentID == model.NadomestniRecenzentID &&
                   PrijavaID == model.PrijavaID;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(OriginalniRecenzentID, NadomestniRecenzentID, PrijavaID);
        }
    }
}
