using System.Collections.Generic;
using System.Linq;

namespace ePerPartsListGenerator
{
    class Catalogue
    {
        public List<Drawing> Drawings;
        public List<string> Groups;
        public string Description;
        public string CatCode;
        public Dictionary<string, string> AllModifications;
        public Dictionary<string, string> AllVariants;
        internal string ImagePath;

        public void PopulateCatalogue(string CatalogueCode)
        {
            CatCode = CatalogueCode;
            var rep = new Repository();
            rep.Open();
            rep.GetCatalogue(this, CatCode);
            AllModifications = rep.GetAllModificationLegendEntries(this);
            AllVariants = rep.GetAllVariants(this);
            Drawings = rep.GetDrawings(this, CatalogueCode);
            Groups = Drawings.Select(x => x.GroupDesc).Distinct().ToList();
        }
    }
}
