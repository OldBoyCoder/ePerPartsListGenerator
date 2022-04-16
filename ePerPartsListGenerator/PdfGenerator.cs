/*
MIT License

Copyright (c) 2021 Christopher Reynolds

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/
using System.Collections.Generic;
using System.IO;
using ePerPartsListGenerator.Model;
using ePerPartsListGenerator.Render;
using ePerPartsListGenerator.Repository;

namespace ePerPartsListGenerator
{
    public class PdfGenerator
    {
        private IRepository Rep;
        public PdfGenerator(IRepository rep)
        {
            Rep = rep;
        }
        public Stream CreatePartsListPdf(string catalogueCode)
        {
            var cat = Rep.GetCatalogue(catalogueCode);
            var renderer = new CatalogueRendererLandscape(cat) {DocumentPerSection = true};
            renderer.StartDocument();
            return renderer.AddGroups(cat);
        }

        public List<Catalogue> GetAllCatalogues()
        {
            var catalogues = Rep.GetAllCatalogues();
            return catalogues;
        }
    }
}