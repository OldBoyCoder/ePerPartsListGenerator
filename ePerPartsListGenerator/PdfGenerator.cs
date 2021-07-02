using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ePerPartsListGenerator.Model;
using ePerPartsListGenerator.Render;

namespace ePerPartsListGenerator
{
    public class PdfGenerator
    {
        public Stream CreatePartsListPdf(string catalogueCode, string languageCode)
        {
            var cat = new Catalogue();
            cat.PopulateCatalogue(catalogueCode, languageCode);
            var renderer = new CatalogueRendererLandscape(cat) { DocumentPerSection = true };
            renderer.StartDocument();
            return renderer.AddGroups(cat);

        }
    }
}
