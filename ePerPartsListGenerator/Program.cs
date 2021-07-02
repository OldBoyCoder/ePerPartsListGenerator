using ePerPartsListGenerator.Model;
using ePerPartsListGenerator.Render;

namespace ePerPartsListGenerator
{
    class Program
    {
        static void Main()
        {
            var cat = new Catalogue();
            cat.PopulateCatalogue("PK", "3");
            var renderer = new CatalogueRendererLandscape(cat) {DocumentPerSection = true};
            renderer.StartDocument();
            renderer.AddGroups(cat);
            //renderer.AddDrawings();
        }


    }
}
