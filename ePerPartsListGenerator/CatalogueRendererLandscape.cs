using PdfSharp.Drawing;
using PdfSharp.Pdf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace ePerPartsListGenerator
{
    class CatalogueRendererLandscape

    {
        Catalogue catalogue;
        double spaceForGroups;
        double groupsWidth;
        double punchMargin;
        double littleGap;
        long PageNumber = 1;
        private string LastGroup;
        PdfDocument doc;
        PdfPage page;
        XGraphics gfx;
        private double GroupY;
        public CatalogueRendererLandscape(Catalogue Catalogue)
        {
            catalogue = Catalogue;

        }
        static double GetWidth(PdfPage page, double portion)
        {
            return (portion / 100.0) * page.Width;
        }
        static double GetHeight(PdfPage page, double portion)
        {
            return (portion / 100.0) * page.Height;
        }
        public void StartDocument()
        {
            doc = new PdfDocument();

            // use this dummy first page to work out some metrics
            // To avoid deleting it, we'll use it as a title page somehow.
            page = doc.AddPage();
            gfx = XGraphics.FromPdfPage(page);

            page.Size = PdfSharp.PageSize.A4;
            page.Orientation = PdfSharp.PageOrientation.Portrait;
            AddPageNumber();

            spaceForGroups = GetHeight(page, 90.0);
            punchMargin = GetWidth(page, 8);
            groupsWidth = CalculateGroupsWidth(page, spaceForGroups, gfx);
            littleGap = GetWidth(page, 0.5);

            var font = new XFont("Verdana", 14, XFontStyle.Regular);
            var Y = GetHeight(page, 10);
            Y += DrawStringCentre(gfx, $"Parts list", punchMargin, Y, page.Width - punchMargin, font);
            Y += DrawStringCentre(gfx, $"{catalogue.CatCode} - {catalogue.Description}", punchMargin, Y, page.Width - punchMargin, font);
            Y += DrawStringCentre(gfx, $"Produced on " + DateTime.Now.ToString("G"), punchMargin, Y, page.Width - punchMargin, font);
            Y += DrawStringCentre(gfx, $"Using version " + Assembly.GetExecutingAssembly().GetName().Version.ToString(), punchMargin, Y, page.Width - punchMargin, font);
            DrawDrawing(catalogue.ImagePath, Y + 20);

        }
        public void AddPageNumber()
        {
            var Y = GetHeight(page, 95);
            var font = new XFont("Verdana", 6, XFontStyle.Regular);
            DrawStringCentre(gfx, $"Source data is copyright(c) 2011, Fiat Group Automobiles.  Code to produce this PDF is copyright(c) 2021, Chris Reynolds.  Page: {PageNumber}", punchMargin, Y, page.Width - punchMargin * 2, font);

            PageNumber++;
        }
        public void AddDrawings()
        {
            double y;
            LastGroup = "";
            GroupY = 0.0;
            foreach (var drawing in catalogue.Drawings)
            {
                y = StartNewPage(drawing);

                var font = new XFont("Verdana", 7, XFontStyle.Regular);
                var w = GetWidth(page, 100) - groupsWidth - punchMargin - littleGap;
                List<double> widths = new List<double> { w * .05 - littleGap,
                    w * .12 - littleGap,
                    w * .30 - littleGap,
                    w * .15 - littleGap,
                    w * .12 - littleGap,
                    w * .15 - littleGap,
                    w * .05 - littleGap,
                    w * .05};
                var tableLeft = punchMargin;
                var Gray = true;
                var LastRif = -1;
                y = DrawPartsTableHeaders(gfx, y, font, widths, w, tableLeft);
                var Alignments = new XStringFormat[] { XStringFormats.TopLeft, XStringFormats.TopRight, XStringFormats.TopLeft, XStringFormats.TopLeft, XStringFormats.TopLeft, XStringFormats.TopLeft, XStringFormats.TopLeft, XStringFormats.TopLeft };

                foreach (var p in drawing.Parts)
                {
                    var Fields = new string[] { p.RIF.ToString(), p.PartNo, p.Description,
                        string.Join(", ", p.Modification), string.Join(", ", p.Compatibility),p.Notes, p.Qty, p.ClicheCode != ""?"*": "" };
                    var DrawItems = CalculateHeightForItems(widths, Fields, font, gfx);
                    if (y + (DrawItems.Count * 10) > GetHeight(page, 90))
                    {
                        y = StartNewPage(drawing);
                        y = DrawPartsTableHeaders(gfx, y, font, widths, w, tableLeft);
                    }
                    if (LastRif != p.RIF)
                    {
                        Gray = !Gray;
                        LastRif = p.RIF;
                    }
                    if (Gray)
                    {
                        gfx.DrawRectangle(XBrushes.LightGray, new XRect(tableLeft + littleGap, y, w, 10 * DrawItems.Count));
                    }
                    y = DrawAllItems(gfx, y, font, widths, tableLeft, DrawItems, Alignments);
                }

                y = AddModificationsToLegend(y, drawing, font, w, tableLeft);

                y = AddCompatibilityToLegend(y, drawing, font, w, tableLeft);

                // There's chance that there are are sub-assemblies to draw
                foreach (var cliche in drawing.Cliches.Values)
                {
                    y = StartNewClichePage(drawing, cliche);
                    widths = new List<double> { w * .05 - littleGap,
                    w * .12 - littleGap,
                    w * .30 - littleGap,
                    w * .15 - littleGap,
                    w * .12 - littleGap,
                    w * .15 - littleGap,
                    w * .05 - littleGap,
                    w * .05};
                    Gray = true;
                    LastRif = -1;
                    y = DrawPartsTableHeaders(gfx, y, font, widths, w, tableLeft);
                    Alignments = new XStringFormat[] { XStringFormats.TopLeft, XStringFormats.TopRight, XStringFormats.TopLeft, XStringFormats.TopLeft, XStringFormats.TopLeft, XStringFormats.TopLeft, XStringFormats.TopLeft, XStringFormats.TopLeft };

                    foreach (var p in cliche.Parts)
                    {
                        var Fields = new string[] { p.RIF.ToString(), p.PartNo, p.Description,
                        string.Join(", ", p.Modification), string.Join(", ", p.Compatibility),p.Notes, p.Qty, p.ClicheCode != ""?"*": "" };
                        var DrawItems = CalculateHeightForItems(widths, Fields, font, gfx);
                        if (y + (DrawItems.Count * 10) > GetHeight(page, 90))
                        {
                            y = StartNewClichePage(drawing, cliche);
                            y = DrawPartsTableHeaders(gfx, y, font, widths, w, tableLeft);
                        }
                        if (LastRif != p.RIF)
                        {
                            Gray = !Gray;
                            LastRif = p.RIF;
                        }
                        if (Gray)
                        {
                            gfx.DrawRectangle(XBrushes.LightGray, new XRect(tableLeft + littleGap, y, w, 10 * DrawItems.Count));
                        }
                        y = DrawAllItems(gfx, y, font, widths, tableLeft, DrawItems, Alignments);
                    }


                }

            }

            doc.Save(@"c:\temp\parts.pdf");

        }

        private double AddCompatibilityToLegend(double y, Drawing drawing, XFont font, double w, double tableLeft)
        {
            List<double> widths = new List<double> { w * .2 - littleGap, w * .8 };
            var Alignments = new XStringFormat[] { XStringFormats.TopLeft, XStringFormats.TopLeft };
            foreach (var item in drawing.CompatibilityList.OrderBy(x => x))
            {
                var Fields = new string[] { item.ToString(), catalogue.AllVariants.ContainsKey(item) ? catalogue.AllVariants[item] : "" };
                var DrawItems = CalculateHeightForItems(widths, Fields, font, gfx);
                if (y + (DrawItems.Count * 10) > GetHeight(page, 90))
                {
                    y = StartNewPage(drawing);
                    y = DrawLegendTableHeaders(gfx, y, font, widths, w, tableLeft);
                }
                y = DrawAllItems(gfx, y, font, widths, tableLeft, DrawItems, Alignments);
            }

            return y;
        }

        private double AddModificationsToLegend(double y, Drawing drawing, XFont font, double w, double tableLeft)
        {
            List<double> widths = new List<double> { w * .2 - littleGap, w * .8 };
            var Alignments = new XStringFormat[] { XStringFormats.TopLeft, XStringFormats.TopLeft };

            y = DrawLegendTableHeaders(gfx, y, font, widths, w, tableLeft);
            foreach (var item in drawing.ModificationList.OrderBy(x => x))
            {
                var Fields = new string[] { item, catalogue.AllModifications.ContainsKey(item) ? catalogue.AllModifications[item] : item };
                var DrawItems = CalculateHeightForItems(widths, Fields, font, gfx);

                if (y + (DrawItems.Count * 10) > GetHeight(page, 90))
                {
                    y = StartNewPage(drawing);
                    y = DrawLegendTableHeaders(gfx, y, font, widths, w, tableLeft);
                }
                y = DrawAllItems(gfx, y, font, widths, tableLeft, DrawItems, Alignments);
            }

            return y;
        }

        private double StartNewPage(Drawing drawing)
        {
            double y;
            page = doc.AddPage();
            gfx = XGraphics.FromPdfPage(page);
            page.Size = PdfSharp.PageSize.A4;
            page.Orientation = PdfSharp.PageOrientation.Portrait;
            AddPageNumber();
            DrawGroupTags(drawing);
            y = DrawPageTitle(drawing);
            y = DrawDrawing(drawing.ImagePath, y);
            return y;
        }
        private double StartNewClichePage(Drawing drawing, Cliche cliche)
        {
            double y;
            page = doc.AddPage();
            gfx = XGraphics.FromPdfPage(page);
            page.Size = PdfSharp.PageSize.A4;
            page.Orientation = PdfSharp.PageOrientation.Portrait;
            AddPageNumber();
            DrawGroupTags(drawing);
            y = DrawClichePageTitle(drawing, cliche);
            y = DrawDrawing(cliche.ImagePath, y);
            return y;
        }

        private double DrawAllItems(XGraphics gfx, double y, XFont font, List<double> widths, double tableLeft, List<List<string>> DrawItems, XStringFormat[] alignments)
        {
            foreach (var line in DrawItems)
            {
                var x = littleGap;
                for (int i = 0; i < line.Count; i++)
                {
                    gfx.DrawString(line[i], font, XBrushes.Black,
                    new XRect(tableLeft + x, y, widths[i], 10), alignments[i]);
                    x += widths[i];
                    x += littleGap;
                }
                y += 10;
            }
            return y;
        }

        private List<List<string>> CalculateHeightForItems(List<double> widths, string[] fields, XFont font, XGraphics gfx)
        {
            var MaxLines = 1;
            var rc = new List<List<string>>();
            // so work out if each column fits in its width, if not work out how many rows are needed
            // then return a 2D array of the split items
            for (int i = 0; i < widths.Count; i++)
            {
                var lines = BreakGroupIntoLines(widths[i], font, gfx, fields[i]);
                if (lines.Count > MaxLines)
                    MaxLines = lines.Count;
            }
            for (int i = 0; i < MaxLines; i++)
            {
                rc.Add(new List<string>());
                for (int j = 0; j < widths.Count; j++)
                {
                    var lines = BreakGroupIntoLines(widths[j], font, gfx, fields[j]);
                    if (lines.Count - 1 < i)
                        rc[i].Add("");
                    else
                        rc[i].Add(lines[i]);
                }
            }
            return rc;
        }

        private double DrawPartsTableHeaders(XGraphics gfx, double y, XFont font, List<double> widths, double w, double tableLeft)
        {
            gfx.DrawRectangle(XBrushes.Black, new XRect(tableLeft + littleGap, y, w, 10));
            var x = littleGap;
            gfx.DrawString("", font, XBrushes.White,
            new XRect(tableLeft + x, y, widths[0], 20), XStringFormats.TopLeft);
            x += widths[0];
            x += littleGap;
            gfx.DrawString("Part #", font, XBrushes.White,
            new XRect(tableLeft + x, y, widths[1], 20), XStringFormats.TopRight);
            x += widths[1];
            x += littleGap;
            gfx.DrawString("Description", font, XBrushes.White,
            new XRect(tableLeft + x, y, widths[2], 20), XStringFormats.TopLeft);
            x += widths[2];
            x += littleGap;
            gfx.DrawString("Modif.", font, XBrushes.White,
            new XRect(tableLeft + x, y, widths[3], 20), XStringFormats.TopLeft);
            x += widths[3];
            x += littleGap;
            gfx.DrawString("Compatibility", font, XBrushes.White,
            new XRect(tableLeft + x, y, widths[4], 20), XStringFormats.TopLeft);
            x += widths[4];
            x += littleGap;
            gfx.DrawString("Notes", font, XBrushes.White,
            new XRect(tableLeft + x, y, widths[5], 20), XStringFormats.TopLeft);
            x += widths[5];
            x += littleGap;
            gfx.DrawString("Qty", font, XBrushes.White,
            new XRect(tableLeft + x, y, widths[6], 20), XStringFormats.TopLeft);
            x += widths[6];
            x += littleGap;
            gfx.DrawString("Sub", font, XBrushes.White,
            new XRect(tableLeft + x, y, widths[7], 20), XStringFormats.TopLeft);
            y += 10;
            return y;
        }
        private double DrawLegendTableHeaders(XGraphics gfx, double y, XFont font, List<double> widths, double w, double tableLeft)
        {
            gfx.DrawRectangle(XBrushes.Black, new XRect(tableLeft + littleGap, y, w, 10));
            var x = littleGap;
            gfx.DrawString("Legend", font, XBrushes.White,
            new XRect(tableLeft + x, y, widths[0] + widths[1] + littleGap, 20), XStringFormats.TopLeft);
            y += 10;
            return y;
        }

        private double CalculateGroupsWidth(PdfPage page, double spaceForGroups, XGraphics gfx)
        {
            var AllGroupsInTwoLines = false;
            var initialWidth = GetWidth(page, 8);
            var font = new XFont("Verdana", 7, XFontStyle.Regular);

            while (!AllGroupsInTwoLines)
            {
                AllGroupsInTwoLines = true;
                foreach (var group in catalogue.Groups)
                {
                    List<string> Lines = BreakGroupIntoLines(initialWidth, font, gfx, group);
                    if (Lines.Count > 2)
                    {
                        AllGroupsInTwoLines = false;
                        break;
                    }
                }
                if (!AllGroupsInTwoLines)
                    initialWidth += GetWidth(page, 0.5);
            }
            return initialWidth;
        }
        private List<string> BreakGroupIntoLines(double initialWidth, XFont font, XGraphics gfx, string group)
        {
            var parts = group.Split(new[] { ' ' }).ToList();
            var Lines = new List<string>();
            var text = "";
            while (parts.Count > 0)
            {
                var oldText = text;
                text = text + " " + parts[0];
                text = text.Trim();
                var textWidth = gfx.MeasureString(text, font);
                if (textWidth.Width > initialWidth)
                {
                    Lines.Add(oldText);
                    // Need to write out old text
                    text = parts[0];
                }
                parts.RemoveAt(0);
            }
            Lines.Add(text);
            return Lines;
        }
        private double DrawDrawing(string imagePath, double StartY)
        {
            XImage image = XImage.FromFile(@"c:\temp\eper\images\" + imagePath);
            var xx = GetWidth(page, 90) - (groupsWidth + littleGap + punchMargin);
            var yy = xx;
            if (image.PixelWidth > image.PixelHeight)
            {
                yy = ((image.PixelHeight * xx) / image.PixelWidth);
            }
            else
            {
                xx = ((image.PixelWidth * yy) / image.PixelHeight);

            }
            gfx.DrawImage(image, GetWidth(page, 50) - xx / 2, StartY + littleGap, xx, yy);
            return StartY + littleGap + yy + littleGap;
        }

        private void DrawGroupTags(Drawing drawing)
        {
            XFont font = new XFont("Verdana", 7, XFontStyle.Regular);
            var GroupLineHeight = gfx.MeasureString(LastGroup, font).Height;
            if (LastGroup != drawing.GroupDesc)
            {
                GroupY += (2 * GroupLineHeight);
                LastGroup = drawing.GroupDesc;
            }
            gfx.DrawRectangle(XBrushes.Black, new XRect(page.Width - groupsWidth, GroupY, groupsWidth, GroupLineHeight * 2.0));
            var Lines = BreakGroupIntoLines(groupsWidth, font, gfx, drawing.GroupDesc);
            for (var i = 0; i < Lines.Count(); i++)
            {

                gfx.DrawString(Lines[i], font, XBrushes.White,
                    new XRect(page.Width - groupsWidth, GroupY + i * GroupLineHeight, groupsWidth, page.Height),
                    XStringFormats.TopLeft);
            }
        }
        private double DrawString(XGraphics gfx, string str, double x, double y, XFont font)
        {
            var rect = gfx.MeasureString(str, font);
            gfx.DrawString(str, font, XBrushes.Black, new XRect(x, y, rect.Width, rect.Height), XStringFormats.TopLeft);
            return rect.Height;
        }
        private double DrawStringCentre(XGraphics gfx, string str, double x, double y, double w, XFont font)
        {
            var rect = gfx.MeasureString(str, font);
            gfx.DrawString(str, font, XBrushes.Black, new XRect(x, y, w, rect.Height), XStringFormats.TopCenter);
            return rect.Height;
        }
        private double DrawStringRight(XGraphics gfx, string str, double x, double y, double w, XFont font)
        {
            var rect = gfx.MeasureString(str, font);
            gfx.DrawString(str, font, XBrushes.Black, new XRect(x, y, w, rect.Height), XStringFormats.TopRight);
            return rect.Height;
        }
        private double DrawPageTitle(Drawing drawing)
        {
            var StartY = GetHeight(page, 5);
            // Create a font
            XFont font = new XFont("Verdana", 11, XFontStyle.Bold);
            // Draw the text
            var drawString = $"{drawing.TableCode} - {drawing.Description} - {drawing.DrawingNo}";
            StartY += DrawString(gfx, drawString, punchMargin, StartY, font);
            font = new XFont("Verdana", 10, XFontStyle.Regular);
            var width = gfx.MeasureString("Catalogue    ", font).Width;
            DrawStringRight(gfx, "Catalogue", punchMargin, StartY, width, font);
            StartY += DrawString(gfx, $"{catalogue.CatCode} - {catalogue.Description}", punchMargin + width + littleGap, StartY, font);
            DrawStringRight(gfx, "Category", punchMargin, StartY, width, font);
            StartY += DrawString(gfx, $"[{drawing.GroupCode}] {drawing.GroupDesc}", punchMargin + width + littleGap, StartY, font);
            DrawStringRight(gfx, "Category", punchMargin, StartY, width, font);
            StartY += DrawString(gfx, $"[{drawing.TableCode}] {drawing.Description}", punchMargin + width + littleGap, StartY, font);
            DrawStringRight(gfx, "Var", punchMargin, StartY, width, font);
            StartY += DrawString(gfx, drawing.Variante.ToString(), punchMargin + width + littleGap, StartY, font);
            DrawStringRight(gfx, "Rev", punchMargin, StartY, width, font);
            StartY += DrawString(gfx, drawing.Revision.ToString(), punchMargin + width + littleGap, StartY, font);
            DrawStringRight(gfx, "Mods", punchMargin, StartY, width, font);
            StartY += DrawString(gfx, drawing.Modifications, punchMargin + width + littleGap, StartY, font);
            DrawStringRight(gfx, "Valid for", punchMargin, StartY, width, font);
            StartY += DrawString(gfx, drawing.ValidFor, punchMargin + width + littleGap, StartY, font);
            return StartY;
        }
        private double DrawClichePageTitle(Drawing drawing, Cliche cliche)
        {
            var StartY = GetHeight(page, 5);
            // Create a font
            XFont font = new XFont("Verdana", 11, XFontStyle.Bold);
            // Draw the text
            var drawString = $"{cliche.PartNo} - {cliche.Description} ";
            StartY += DrawString(gfx, drawString, punchMargin, StartY, font);
            font = new XFont("Verdana", 10, XFontStyle.Regular);
            var width = gfx.MeasureString("Catalogue    ", font).Width;
            DrawStringRight(gfx, "Catalogue", punchMargin, StartY, width, font);
            StartY += DrawString(gfx, $"{catalogue.CatCode} - {catalogue.Description}", punchMargin + width + littleGap, StartY, font);
            DrawStringRight(gfx, "Category", punchMargin, StartY, width, font);
            StartY += DrawString(gfx, $"[{drawing.GroupCode}] {drawing.GroupDesc}", punchMargin + width + littleGap, StartY, font);
            DrawStringRight(gfx, "Table", punchMargin, StartY, width, font);
            StartY += DrawString(gfx, $"[{drawing.TableCode}] {drawing.Description}", punchMargin + width + littleGap, StartY, font);
            return StartY;
        }

    }
}
