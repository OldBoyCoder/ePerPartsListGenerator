using System.Collections.Generic;

namespace ePerPartsListGenerator
{
    class Part
    {
        public string Description;
        public string PartNo;
        public string Qty;
        public List<string> Modification;
        public List<string> Compatibility;
        public int RIF;
        public string Notes;
        internal string ClicheCode;
    }
}
