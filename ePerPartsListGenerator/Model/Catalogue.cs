using System.Collections.Generic;

namespace ePerPartsListGenerator.Model
{
    /// <summary>
    /// Catalogue represents a single model in the ePer system.  Here it is used as the root to 
    /// the class hierarchy.  We also hold the details of all the modifications and variants
    /// used across the whole catalogue to save having to get them for each drawing
    /// </summary>
    class Catalogue
    {
        //public List<Drawing> Drawings;
        /// <summary>
        /// Maintain a list of the distinct groups used in the catalogue.  It is used to
        /// draw the quick access tabs down the side of the page
        /// </summary>
        public List<Group> Groups;
        public string Description;
        public string CatCode;
        public Dictionary<string, string> AllModifications;
        public Dictionary<string, string> AllVariants;
        internal string ImagePath;

        /// <summary>
        /// Pull back everything from the database for this catalogue
        /// </summary>
        /// <param name="catalogueCode">The code for the car of interest e.g. PK for Barchetta</param>
        /// <param name="languageCode"></param>
        public void PopulateCatalogue(string catalogueCode, string languageCode)
        {
            CatCode = catalogueCode;
            var rep = new Repository.Repository(languageCode);
            rep.Open();
            rep.GetCatalogue(this, CatCode);
            AllModifications = rep.GetAllModificationLegendEntries(this);
            AllVariants = rep.GetAllVariantLegendEntries(this);
            //Drawings = rep.GetDrawings(this, CatalogueCode);
            Groups = rep.GetAllGroupEntries(this);
            foreach (var group in Groups)
            {
                rep.GetGroupTables(this, group);
                foreach (var table in group.Tables)
                {
                    rep.GetTableDrawings(this, group, table);
                }
            }
            rep.Close();
        }
    }
}
