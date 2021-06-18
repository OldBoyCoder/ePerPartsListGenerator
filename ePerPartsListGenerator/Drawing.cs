using System.Collections.Generic;

namespace ePerPartsListGenerator
{
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
    }
}
