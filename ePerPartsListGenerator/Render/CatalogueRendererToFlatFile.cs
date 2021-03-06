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
using ePerPartsListGenerator.Model;
using System.IO;
using System.Linq;

namespace ePerPartsListGenerator.Render
{
    public class CatalogueRendererToFlatFile
    {
        private Catalogue cat;
        private bool includeDescriptions;
        private MemoryStream stream;
        private StreamWriter writer;

        public CatalogueRendererToFlatFile(Catalogue cat, bool includeDescriptions)
        {
            this.cat = cat;
            this.includeDescriptions = includeDescriptions;
        }

        internal void StartDocument()
        {
            stream = new MemoryStream();
        }

        internal Stream AddGroups(Catalogue cat)
        {
            writer = new StreamWriter(stream);
            // Write catalogue entry
            WriteLine("Line type", "Catalogue code", "Make code", "Make", "Catalogue description",
                                "Group code", "Group description",
                                "Full table code", "Table code", "Sub group code", "Table description",
                                "Drawing number", "Drawing valid for", "Drawing modifications",
                                "Part number", "Part description", "Part order in table", "Part quantity", "Part notes", "Part modification", "Part compatibility",
                                "Part number", "Cliche description", "Cliche part number","Cliche part description", "Cliche order in table", "Cliche part quantity", "Cliche part notes", "Cliche part modification", "Cliche part compatibility");
            var catPrefix = WriteLine("CAT", cat.CatCode, cat.MakeCode, cat.Make, includeDescriptions ? cat.Description : "");
            foreach (var group in cat.Groups)
            {
                var groupPrefix = WriteLine("GRP", catPrefix, group.Code, includeDescriptions ? group.Description : "");
                foreach (var table in group.Tables.OrderBy(x => x.FullCode))
                {
                    var tablePrefix = WriteLine("TAB", groupPrefix, table.FullCode, table.TableCode.ToString(), table.SubGroupCode.ToString(), includeDescriptions ? table.Description : "");
                    foreach (var drawing in table.Drawings.OrderBy(x => x.DrawingNo))
                    {
                        var drawingPrefix = WriteLine("DRW", tablePrefix, drawing.DrawingNo.ToString(), drawing.ValidFor, drawing.Modifications);
                        foreach (var part in drawing.Parts)
                        {
                            WriteLine("PRT", drawingPrefix, part.PartNo, includeDescriptions ? part.Description : "", part.Rif.ToString(), part.Qty.Trim(), includeDescriptions ? part.Notes : "", string.Join(",", part.Modification), string.Join(",", part.Compatibility));
                        }
                        foreach (var cliche in drawing.Cliches)
                        {
                            // add blanks for part columns
                            var clichePrefix = WriteLine("CLC", drawingPrefix, "", "" ,"", "", "", "", "",cliche.PartNo, includeDescriptions ? cliche.Description : "");
                            foreach (var part in cliche.Parts)
                            {
                                WriteLine("CLP", clichePrefix, part.PartNo, includeDescriptions ? part.Description : "", part.Rif.ToString(), part.Qty.Trim(), part.Notes, string.Join(",", part.Modification), string.Join(",", part.Compatibility));

                            }
                        }
                    }
                }
            }


            writer.Flush();
            stream.Seek(0, SeekOrigin.Begin);
            return stream;

        }

        private string WriteLine(string lineType, params string[] values)
        {
            var s = string.Join("\t", values);
            writer.WriteLine(lineType + "\t" + s);
            return s;
        }
    }
}
