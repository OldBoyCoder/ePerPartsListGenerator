using System.Collections.Generic;

namespace ePerPartsListGenerator
{
    /// <summary>
    /// Details about an individual part.  Includes a list of any modification or variant comments
    /// </summary>
    class Part
    {
        public string Description;
        public string PartNo;
        public string Qty;
        public List<string> Modification;
        public List<string> Compatibility;
        public int Rif;
        public string Notes;
        internal string ClicheCode;
    }
}
