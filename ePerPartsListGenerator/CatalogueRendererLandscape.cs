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
        PdfDocument doc;
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
            var page = doc.AddPage();
            page.Size = PdfSharp.PageSize.A4;
            page.Orientation = PdfSharp.PageOrientation.Portrait;
            var gfx = XGraphics.FromPdfPage(page);
            AddPageNumber(page, gfx);
            // In terms of vertical space we'll have a 5% top margin, 5% for a title line
            // 90% for the body list of groups, 5% for footer info, 5% bottom margin
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
            DrawDrawing(page, catalogue.ImagePath , gfx, Y+10);

        }
        public void AddPageNumber(PdfPage page,    XGraphics gfx)
        {
            var Y = GetHeight(page, 95);
            var font = new XFont("Verdana", 8, XFontStyle.Regular);
            Y += DrawStringCentre(gfx, $"{PageNumber}", punchMargin, Y, page.Width - punchMargin, font);
            PageNumber++;
        }
        public void AddDrawings()
        {
            var LastGroup = "";
            var GroupY = -0.0;
            foreach (var drawing in catalogue.Drawings)
            {

                var page = doc.AddPage();
                page.Size = PdfSharp.PageSize.A4;
                page.Orientation = PdfSharp.PageOrientation.Portrait;
                XGraphics gfx = XGraphics.FromPdfPage(page);
                AddPageNumber(page, gfx);

                var StartY = 0.0;
                StartY = DrawPageTitle(page, drawing, gfx);
                DrawGroupTags(ref LastGroup, ref GroupY, page, groupsWidth, drawing, gfx);
                StartY = DrawDrawing(page, drawing.ImagePath, gfx, StartY);
                var y = StartY;
                var font = new XFont("Verdana", 8, XFontStyle.Regular);
                var widths = new List<double>();
                var w = GetWidth(page, 100) - groupsWidth - punchMargin - littleGap;
                widths.Add(w * .05 - littleGap);
                widths.Add(w * .10 - littleGap);
                widths.Add(w * .35 - littleGap);
                widths.Add(w * .22 - littleGap);
                widths.Add(w * .23 - littleGap);
                widths.Add(w * .05);
                var tableLeft = punchMargin;
                var Gray = true;
                var LastRif = -1;
                y = DrawPartsTableHeaders(gfx, y, font, widths, w, tableLeft);

                foreach (var p in drawing.Parts)
                {
                    if (y > GetHeight(page, 90))
                    {
                        // We need to start a new page
                        page = doc.AddPage();
                        page.Size = PdfSharp.PageSize.A4;
                        page.Orientation = PdfSharp.PageOrientation.Portrait;
                        gfx = XGraphics.FromPdfPage(page);
                        AddPageNumber(page, gfx);
                        StartY = DrawPageTitle(page, drawing, gfx);
                        DrawGroupTags(ref LastGroup, ref GroupY, page, groupsWidth, drawing, gfx);
                        StartY = DrawDrawing(page, drawing.ImagePath, gfx, StartY);
                        y = StartY;
                        y = DrawPartsTableHeaders(gfx, y, font, widths, w, tableLeft);
                    }
                    if (LastRif != p.RIF)
                    {
                        Gray = !Gray;
                        LastRif = p.RIF;
                    }
                    if (Gray)
                    {
                        gfx.DrawRectangle(XBrushes.LightGray, new XRect(tableLeft + littleGap, y, w, 10));
                    }
                    var x = littleGap;
                    gfx.DrawString(p.RIF.ToString(), font, XBrushes.Black,
                    new XRect(tableLeft + x, y, widths[0], 20), XStringFormats.TopLeft);
                    x += widths[0];
                    x += littleGap;
                    gfx.DrawString(p.PartNo, font, XBrushes.Black,
                    new XRect(tableLeft + x, y, widths[1], 20), XStringFormats.TopRight);
                    x += widths[1];
                    x += littleGap;
                    gfx.DrawString(p.Description, font, XBrushes.Black,
                    new XRect(tableLeft + x, y, widths[2], 20), XStringFormats.TopLeft);
                    x += widths[2];
                    x += littleGap;
                    gfx.DrawString(string.Join(",", p.Modification), font, XBrushes.Black,
                    new XRect(tableLeft + x, y, widths[3], 20), XStringFormats.TopLeft);
                    x += widths[3];
                    x += littleGap;
                    gfx.DrawString(string.Join(",", p.Compatibility), font, XBrushes.Black,
                    new XRect(tableLeft + x, y, widths[4], 20), XStringFormats.TopLeft);
                    x += widths[4];
                    x += littleGap;
                    gfx.DrawString(p.Qty, font, XBrushes.Black,
                    new XRect(tableLeft + x, y, widths[5], 20), XStringFormats.TopLeft);
                    x += littleGap;
                    y += 10;
                }
                widths = new List<double>();
                widths.Add(w * .2 - littleGap);
                widths.Add(w * .8 );
                y = DrawLegendTableHeaders(gfx, y, font, widths, w, tableLeft);
                foreach (var item in drawing.ModificationList)
                {
                    if (y > GetHeight(page, 90))
                    {
                        // We need to start a new page
                        page = doc.AddPage();
                        page.Size = PdfSharp.PageSize.A4;
                        page.Orientation = PdfSharp.PageOrientation.Portrait;
                        gfx = XGraphics.FromPdfPage(page);
                        AddPageNumber(page, gfx);

                        StartY = DrawPageTitle(page, drawing, gfx);
                        DrawGroupTags(ref LastGroup, ref GroupY, page, groupsWidth, drawing, gfx);
                        StartY = DrawDrawing(page, drawing.ImagePath, gfx, StartY);
                        y = StartY;
                        y = DrawLegendTableHeaders(gfx, y, font, widths, w, tableLeft);
                    }
                    var x = littleGap;
                    gfx.DrawString(item, font, XBrushes.Black,
                    new XRect(tableLeft + x, y, widths[0], 20), XStringFormats.TopLeft);
                    x += widths[0];
                    x += littleGap;
                    gfx.DrawString(catalogue.Legend[item], font, XBrushes.Black,
                    new XRect(tableLeft + x, y, widths[1], 20), XStringFormats.TopLeft);
                    y += 10;
                }

            }

            doc.Save(@"c:\temp\parts.pdf");

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
            gfx.DrawString("Qty", font, XBrushes.White,
            new XRect(tableLeft + x, y, widths[5], 20), XStringFormats.TopLeft);
            y += 10;
            return y;
        }
        private double DrawLegendTableHeaders(XGraphics gfx, double y, XFont font, List<double> widths, double w, double tableLeft)
        {
            gfx.DrawRectangle(XBrushes.Black, new XRect(tableLeft + littleGap, y, w, 10));
            var x = littleGap;
            gfx.DrawString("Legend", font, XBrushes.White,
            new XRect(tableLeft + x, y, widths[0]+widths[1]+littleGap, 20), XStringFormats.TopLeft);
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
        private  List<string> BreakGroupIntoLines(double initialWidth, XFont font, XGraphics gfx, string group)
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
        private  double DrawDrawing(PdfPage page, string  imagePath, XGraphics gfx, double StartY)
        {
            XImage image = XImage.FromFile(@"c:\temp\eper\images\" + imagePath);
            var xx = GetWidth(page, 100) - (groupsWidth + littleGap+punchMargin);
            var yy = xx;
            if (image.PixelWidth > image.PixelHeight)
            {
                yy = ((image.PixelHeight * xx) / image.PixelWidth);
            }
            else
            {
                xx = ((image.PixelWidth * yy) / image.PixelHeight);

            }
            gfx.DrawImage(image, punchMargin, StartY + littleGap, xx, yy);
            return StartY + littleGap + yy + littleGap;
        }

        private  XFont DrawGroupTags(ref string LastGroup, ref double GroupY, PdfPage page, double GroupsWidth, Drawing drawing, XGraphics gfx)
        {
            XFont font = new XFont("Verdana", 7, XFontStyle.Regular);
            var GroupLineHeight = gfx.MeasureString(LastGroup, font).Height;
            if (LastGroup != drawing.GroupDesc)
            {
                GroupY += (2 * GroupLineHeight);
                LastGroup = drawing.GroupDesc;
            }
            gfx.DrawRectangle(XBrushes.Black, new XRect(page.Width-groupsWidth, GroupY, GroupsWidth, GroupLineHeight * 2.0));
            var Lines = BreakGroupIntoLines(GroupsWidth, font, gfx, drawing.GroupDesc);
            for (var i = 0; i < Lines.Count(); i++)
            {

                gfx.DrawString(Lines[i], font, XBrushes.White,
                    new XRect(page.Width - groupsWidth, GroupY + i * GroupLineHeight, GroupsWidth, page.Height),
                    XStringFormats.TopLeft);
            }

            return font;
        }
        private double DrawString(XGraphics gfx, string str, double x, double y, XFont font)
        {
            var rect = gfx.MeasureString(str, font);
            gfx.DrawString(str, font, XBrushes.Black, new XRect(x, y, rect.Width, rect.Height), XStringFormats.TopLeft);
            return rect.Height;
        }
        private double DrawStringCentre(XGraphics gfx, string str, double x, double y, double w,XFont font)
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
        private double DrawPageTitle(PdfPage page,  Drawing drawing, XGraphics gfx)
        {
            var StartY = GetHeight(page, 5);
            // Create a font
            XFont font = new XFont("Verdana", 12, XFontStyle.Bold);
            // Draw the text
            var drawString = $"{drawing.TableCode} - {drawing.Description} - {drawing.DrawingNo}";
            StartY += DrawString(gfx, drawString, punchMargin, StartY, font);
            font = new XFont("Verdana", 10, XFontStyle.Regular);
            var width = gfx.MeasureString("Catalogue    ", font).Width;
            DrawStringRight(gfx, "Catalogue", punchMargin, StartY,width, font);
            StartY += DrawString(gfx, $"{catalogue.CatCode} - {catalogue.Description}", punchMargin +width + littleGap, StartY, font);
            DrawStringRight(gfx, "Category", punchMargin, StartY,width, font);
            StartY += DrawString(gfx, $"[{drawing.GroupCode}] {drawing.GroupDesc}", punchMargin  + width + littleGap, StartY, font);
            StartY += DrawString(gfx, $"[{drawing.TableCode}] {drawing.Description}", punchMargin + width + littleGap, StartY, font);
            DrawStringRight(gfx, "Var", punchMargin, StartY,width, font);
            StartY += DrawString(gfx, drawing.Variante.ToString(), punchMargin+ width + littleGap, StartY, font);
            DrawStringRight(gfx, "Rev", punchMargin, StartY, width, font);
            StartY += DrawString(gfx, drawing.Revision.ToString(), punchMargin + width + littleGap, StartY, font);
            DrawStringRight(gfx, "Mods", punchMargin, StartY, width, font);
            StartY += DrawString(gfx, drawing.Modifications, punchMargin + width + littleGap, StartY, font);
            DrawStringRight(gfx, "Valid for", punchMargin, StartY, width, font);
            StartY += DrawString(gfx, drawing.ValidFor, punchMargin + width + littleGap, StartY, font);
            return StartY;
        }

    }
}
