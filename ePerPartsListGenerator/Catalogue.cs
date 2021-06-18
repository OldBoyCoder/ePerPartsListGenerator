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
        public Dictionary<string, string> Legend = new Dictionary<string, string>();
        internal string ImagePath;

        public void PopulateCatalogue(string CatalogueCode)
        {
            CatCode = CatalogueCode;
            var rep = new Repository();
            rep.Open();
            rep.GetCatalogue(this, CatCode);
            Drawings = rep.GetDrawings(this, CatalogueCode);
            Groups = Drawings.Select(x => x.GroupDesc).Distinct().ToList();
        }
    }
}
