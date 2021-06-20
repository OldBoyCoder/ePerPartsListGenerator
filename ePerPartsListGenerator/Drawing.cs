using System.Collections.Generic;

namespace ePerPartsListGenerator
{
    class Cliche
    {
        public string PartNo;
        public string Description; // The name of the parent part actually
        public string ClicheCode;
        public string ImagePath;
        public List<Part> Parts;

        public Cliche(string clicheCode)
        {
            ClicheCode = clicheCode;
        }
    }
    class Drawing
    {
        public string Description;
        public string TableCode;
        public int DrawingNo;
        public int Revision;
        public int Variante;
        public string ImagePath;
        public string GroupDesc;
        public string GroupCode;
        public List<Part> Parts;
        internal List<string> CompatibilityList = new List<string>();
        internal List<string> ModificationList = new List<string>();
        internal string Modifications;
        internal string ValidFor;
        internal Dictionary<string, Cliche> Cliches = new Dictionary<string, Cliche>();
    }
}
