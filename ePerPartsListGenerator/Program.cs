namespace ePerPartsListGenerator
{
    class Program
    {
        static void Main()
        {
            var rep = new Repository();
            rep.Open();
            var cat = new Catalogue();
            cat.PopulateCatalogue("PK");
            rep.Close();
            var renderer = new CatalogueRendererLandscape(cat);
            renderer.DocumentPerSection = true;
            renderer.StartDocument();
            renderer.AddGroups(cat);
            //renderer.AddDrawings();
        }


    }
}
