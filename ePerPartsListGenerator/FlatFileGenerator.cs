using ePerPartsListGenerator.Render;
using ePerPartsListGenerator.Repository;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ePerPartsListGenerator
{
    public class FlatFileGenerator
    {
        private IRepository Rep;
        public FlatFileGenerator(IRepository rep)
        {
            Rep = rep;
        }
        public Stream CreatePartsListFlatFile(string catalogueCode, bool includeDescriptions)
        {
            var cat = Rep.GetCatalogue(catalogueCode);
            var renderer = new CatalogueRendererToFlatFile(cat, includeDescriptions) ;
            renderer.StartDocument();
            return renderer.AddGroups(cat);
        }

    }
}
