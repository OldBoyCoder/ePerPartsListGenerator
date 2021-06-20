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
}
