using System.IO;

namespace ePerPartsListGeneratorCLI
{
    class Program
    {
        static void Main(string[] args)
        {
            var pdfGen = new ePerPartsListGenerator.PdfGenerator();
            var stream =pdfGen.CreatePartsListPdf("2J", "3");
            using (FileStream file = new FileStream(@"c:\temp\parts.zip", FileMode.Create, System.IO.FileAccess.Write))
                stream.CopyTo(file);
        }
    }
}
