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
using ePerPartsListGenerator.Repository;
using System;
using System.IO;
using System.Linq;

namespace ePerPartsListGeneratorCLI
{
    internal class Program
    {
        private static void Main()
        {
            var html = "<html>";
            var repository = new AccessRelease20Repository("3");
            var pdfGen = new ePerPartsListGenerator.PdfGenerator(repository);
            var cats = pdfGen.GetAllCatalogues();
            var lastMake = "";
            var lastModel = "";
            var stream = pdfGen.CreatePartsListPdf("PK"); //2J
            var fileName = $"c:\\temp\\parts_PK.pdf";
            using (var file = new FileStream(fileName, FileMode.Create, FileAccess.Write))
            {
                stream.CopyTo(file);
            }

            //foreach (var cat in cats.OrderBy(x=>x.Make).ThenBy(x=>x.Model).ThenBy(x=>x.Description))
            //{
            //    if (cat.Make != lastMake)
            //    {
            //        if (lastMake != "") html += "</details></details>";
            //        html += $"<details><summary>{cat.Make}</summary><details><summary>{cat.Model}</summary>";
            //        lastMake = cat.Make;
            //        lastModel = cat.Model;
            //    }
            //    else
            //    {
            //        if (cat.Model != lastModel)
            //        {
            //            if (lastMake != "") html += "</details>";
            //            html += $"<details><summary>{cat.Model}</summary>";
            //            lastModel = cat.Model;
            //        }
            //    }

            //    var fileName = $"c:\\temp\\parts_{cat.CatCode}.zip";
            //    html += $"<p>{cat.Description} - <a href=\"{fileName}\">ZIP</a></p>";
            //    Console.WriteLine($"{DateTime.Now}: {cat.Make} {cat.Model} {cat.Description} {cat.CatCode}");
            //    var stream = pdfGen.CreatePartsListPdf(cat.CatCode); //2J
            //    using (var file = new FileStream(fileName, FileMode.Create, FileAccess.Write))
            //    {
            //        stream.CopyTo(file);
            //    }
            //}
            //html += "</details></details></html>";
            //File.WriteAllText("c:\\temp\\summary.html", html);
        }
    }
}