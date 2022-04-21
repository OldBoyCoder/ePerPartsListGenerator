using ePerPartsListGenerator.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
            var catPrefix = WriteLine("CAT", cat.CatCode, cat.MakeCode, cat.Make, includeDescriptions ? cat.Description : "");
            foreach (var group in cat.Groups)
            {
                var groupPrefix = WriteLine("GRP", catPrefix, group.Code, includeDescriptions ? group.Description : "");
                foreach (var table in group.Tables.OrderBy(x => x.FullCode))
                {
                    var tablePrefix = WriteLine("TAB", groupPrefix, table.FullCode, group.Code, table.TableCode.ToString(), table.SubGroupCode.ToString(), includeDescriptions ? table.Description : "");
                    foreach (var drawing in table.Drawings.OrderBy(x => x.DrawingNo))
                    {
                        var drawingPrefix = WriteLine("DRW", tablePrefix, table.FullCode, drawing.DrawingNo.ToString(), drawing.ValidFor, drawing.Modifications);
                        foreach (var part in drawing.Parts)
                        {
                            WriteLine("PRT", drawingPrefix, part.PartNo, includeDescriptions ? part.Description : "", part.Rif.ToString(), part.Qty.Trim(), includeDescriptions ? part.Notes : "", string.Join(",", part.Modification), string.Join(",", part.Compatibility));
                        }
                        foreach (var cliche in drawing.Cliches)
                        {
                            var clichePrefix = WriteLine("CLC", drawingPrefix, cliche.PartNo, includeDescriptions ? cliche.Description : "");
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
            var s = string.Join("|", values);
            writer.WriteLine(lineType + "|" + s);
            return s;
        }
    }
}
