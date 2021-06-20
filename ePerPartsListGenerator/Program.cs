namespace ePerPartsListGenerator
{
    class Program
    {
        static void Main(string[] args)
        {
            var rep = new Repository();
            rep.Open();
            var cat = new Catalogue();
            cat.PopulateCatalogue("PK");
            rep.Close();
            var renderer = new CatalogueRendererLandscape(cat);
            renderer.StartDocument();
            renderer.AddDrawings();
        }


    }
}
