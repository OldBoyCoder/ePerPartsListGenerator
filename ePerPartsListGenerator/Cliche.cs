using System.Collections.Generic;

namespace ePerPartsListGenerator
{
    /// <summary>
    /// A cliche is the drawing of a part that is made up of other parts
    /// One example is the brake caliper which is shown as a part in 
    /// the drawings for the brake system as a single part but is then expanded in
    /// subsequent cliches.
    /// </summary>
    class Cliche
    {
        public string PartNo;
        public string Description; // The name of the parent part actually
        public string ClicheCode;
        public string ImagePath;
        public List<Part> Parts; // The sub-parts that make up the parent part

        public Cliche(string clicheCode)
        {
            ClicheCode = clicheCode;
        }
    }
}
