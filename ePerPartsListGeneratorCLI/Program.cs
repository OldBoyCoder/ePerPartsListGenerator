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
            var repository20 = new AccessRelease20Repository("3", @"C:\ePer installs\Release 20");
            var flatFilegen = new ePerPartsListGenerator.FlatFileGenerator(repository20);
            var stream = flatFilegen.CreatePartsListFlatFile("PK");
            var fileName = $"c:\\temp\\parts_PK_20.tsv";
            using (var file = new FileStream(fileName, FileMode.Create, FileAccess.Write))
            {
                stream.CopyTo(file);
            }
            var repository84 = new AccessRelease84Repository("3", @"C:\ePer installs\Release 84");
            flatFilegen = new ePerPartsListGenerator.FlatFileGenerator(repository84);
            stream = flatFilegen.CreatePartsListFlatFile("PK");
            fileName = $"c:\\temp\\parts_PK_84.tsv";
            using (var file = new FileStream(fileName, FileMode.Create, FileAccess.Write))
            {
                stream.CopyTo(file);
            }

            var pdfGen = new ePerPartsListGenerator.PdfGenerator(repository84);
            stream = pdfGen.CreatePartsListPdf("PK"); //2J
            fileName = $"c:\\temp\\parts_PK_84.pdf";
            using (var file = new FileStream(fileName, FileMode.Create, FileAccess.Write))
            {
                stream.CopyTo(file);
            }
            pdfGen = new ePerPartsListGenerator.PdfGenerator(repository20);
            stream = pdfGen.CreatePartsListPdf("PK"); //2J
            fileName = $"c:\\temp\\parts_PK_20.pdf";
            using (var file = new FileStream(fileName, FileMode.Create, FileAccess.Write))
            {
                stream.CopyTo(file);
            }

        }
    }
}