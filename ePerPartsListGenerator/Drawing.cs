using System.Collections.Generic;

namespace ePerPartsListGenerator
{
    /// <summary>
    /// A Drawings is the main entity in the parts book.
    /// It represents a single drawing of a section of the car
    /// </summary>
    class Drawing
    {
        public int DrawingNo;
        public int Revision;
        public int Variante;
        public string ImagePath;
        public string TableCode;
        public List<Part> Parts;
        internal List<string> CompatibilityList = new List<string>();
        internal List<string> ModificationList = new List<string>();
        internal string Modifications;
        internal string ValidFor;
        /// <summary>
        /// A list of any parts that have further expansion available.
        /// </summary>
        internal Dictionary<string, Cliche> Cliches = new Dictionary<string, Cliche>();
    }
}
