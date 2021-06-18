using PdfSharp.Drawing;
using PdfSharp.Pdf;
using System.Collections.Generic;
using System.Linq;

namespace ePerPartsListGenerator
{
    class CatalogueRendererPortrait
    {
        Catalogue catalogue;
        double spaceForGroups;
        double groupsWidth;
        double littleGap;
        PdfDocument doc;
        public CatalogueRendererPortrait(Catalogue Catalogue)
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
            // In terms of vertical space we'll have a 5% top margin, 5% for a title line
            // 90% for the body list of groups, 5% for footer info, 5% bottom margin
            spaceForGroups = GetHeight(page, 90.0);
            groupsWidth = CalculateGroupsWidth(page, spaceForGroups);
            littleGap = GetWidth(page, 0.5);

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
                var StartY = 0.0;
                StartY = DrawPageTitle(page, groupsWidth, drawing, gfx);
                DrawGroupTags(ref LastGroup, ref GroupY, page, groupsWidth, drawing, gfx);
                DrawDrawing(page, groupsWidth, littleGap, drawing, gfx, StartY);
                var y = StartY;
                var font = new XFont("Verdana", 8, XFontStyle.Regular);
                var widths = new List<double>();
                var w = (page.Width / 2) - littleGap;
                widths.Add(w * .05 - littleGap);
                widths.Add(w * .10 - littleGap);
                widths.Add(w * .35 - littleGap);
                widths.Add(w * .22 - littleGap);
                widths.Add(w * .23 - littleGap);
                widths.Add(w * .05 );
                var Gray = true;
                var LastRif = -1;
                foreach (var p in drawing.Parts)
                {
                    if (LastRif != p.RIF)
                    {
                        Gray = !Gray;
                        LastRif = p.RIF;
                    }
                    if (Gray)
                    {
                        gfx.DrawRectangle(XBrushes.LightGray, new XRect(page.Width / 2 + littleGap, y, w, 10));
                    }
                    var x = littleGap;
                    gfx.DrawString(p.RIF.ToString(), font, XBrushes.Black,
                    new XRect(page.Width / 2 + x, y, widths[0], 20), XStringFormats.TopLeft);
                    x += widths[0];
                    x += littleGap;
                    gfx.DrawString(p.PartNo, font, XBrushes.Black,
                    new XRect(page.Width / 2 + x, y, widths[1], 20), XStringFormats.TopRight);
                    x += widths[1];
                    x += littleGap;
                    gfx.DrawString(p.Description, font, XBrushes.Black,
                    new XRect(page.Width / 2 + x, y, widths[2], 20), XStringFormats.TopLeft);
                    x += widths[2];
                    x += littleGap;
                    gfx.DrawString(string.Join(",", p.Modification), font, XBrushes.Black,
                    new XRect(page.Width / 2 + x, y, widths[3], 20), XStringFormats.TopLeft);
                    x += widths[3];
                    x += littleGap;
                    gfx.DrawString(string.Join(",", p.Compatibility), font, XBrushes.Black,
                    new XRect(page.Width / 2 + x, y, widths[4], 20), XStringFormats.TopLeft);
                    x += widths[4];
                    x += littleGap;
                    gfx.DrawString(p.Qty, font, XBrushes.Black,
                    new XRect(page.Width / 2 + x, y, widths[5], 20), XStringFormats.TopLeft);
                    x += littleGap;
                    y += 10;
                    if (y > GetHeight(page, 80))
                    {
                        // We need to start a new page
                        page = doc.AddPage();
                        page.Size = PdfSharp.PageSize.A4;
                        page.Orientation = PdfSharp.PageOrientation.Landscape;
                        gfx = XGraphics.FromPdfPage(page);
                        DrawPageTitle(page, groupsWidth, drawing, gfx);
                        DrawGroupTags(ref LastGroup, ref GroupY, page, groupsWidth, drawing, gfx);
                        DrawDrawing(page, groupsWidth, littleGap, drawing, gfx, StartY);
                        y = 75;
                    }
                }
            }

            doc.Save(@"c:\temp\parts.pdf");

        }
        private double CalculateGroupsWidth(PdfPage page, double spaceForGroups)
        {
            var AllGroupsInTwoLines = false;
            var initialWidth = GetWidth(page, 8);
            var font = new XFont("Verdana", 7, XFontStyle.Regular);
            XGraphics gfx = XGraphics.FromPdfPage(page);

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
        private  void DrawDrawing(PdfPage page, double GroupsWidth, double LittleGap, Drawing drawing, XGraphics gfx, double StartY)
        {
            XImage image = XImage.FromFile(@"c:\temp\eper\images\" + drawing.ImagePath);
            var xx = GetWidth(page, 50) - (GroupsWidth + LittleGap);
            var yy = xx;
            if (image.PixelWidth > image.PixelHeight)
            {
                yy = ((image.PixelHeight * xx) / image.PixelWidth);
            }
            else
            {
                xx = ((image.PixelWidth * yy) / image.PixelHeight);

            }
            gfx.DrawImage(image, GroupsWidth + LittleGap, StartY + LittleGap, xx, yy);
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
            gfx.DrawRectangle(XBrushes.Black, new XRect(0, GroupY, GroupsWidth, GroupLineHeight * 2.0));
            var Lines = BreakGroupIntoLines(GroupsWidth, font, gfx, drawing.GroupDesc);
            for (var i = 0; i < Lines.Count(); i++)
            {

                gfx.DrawString(Lines[i], font, XBrushes.White,
                    new XRect(0, GroupY + i * GroupLineHeight, GroupsWidth, page.Height),
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
        private double DrawStringRight(XGraphics gfx, string str, double x, double y, double w, XFont font)
        {
            var rect = gfx.MeasureString(str, font);
            gfx.DrawString(str, font, XBrushes.Black, new XRect(x, y, w, rect.Height), XStringFormats.TopRight);
            return rect.Height;
        }
        private double DrawPageTitle(PdfPage page, double GroupsWidth, Drawing drawing, XGraphics gfx)
        {
            var StartY = GetHeight(page, 5);
            // Create a font
            XFont font = new XFont("Verdana", 12, XFontStyle.Bold);
            // Draw the text
            var drawString = $"{drawing.TableCode} - {drawing.Description} - {drawing.DrawingNo}";
            StartY += DrawString(gfx, drawString, GroupsWidth + 10, StartY, font);
            font = new XFont("Verdana", 10, XFontStyle.Regular);
            var width = gfx.MeasureString("Catalogue    ", font).Width;
            DrawStringRight(gfx, "Catalogue", GroupsWidth + 10, StartY,width, font);
            StartY += DrawString(gfx, $"{catalogue.CatCode} - {catalogue.Description}", GroupsWidth + 10+width + littleGap, StartY, font);
            DrawStringRight(gfx, "Category", GroupsWidth + 10, StartY,width, font);
            StartY += DrawString(gfx, $"[{drawing.GroupCode}] {drawing.GroupDesc} / [{drawing.TableCode}] {drawing.Description}", GroupsWidth + 10 + width + littleGap, StartY, font);
            DrawStringRight(gfx, "Var", GroupsWidth + 10, StartY,width, font);
            StartY += DrawString(gfx, drawing.Variante.ToString(), GroupsWidth + 10 + width + littleGap, StartY, font);
            DrawStringRight(gfx, "Rev", GroupsWidth + 10, StartY,width, font);
            StartY += DrawString(gfx, drawing.Revision.ToString(), GroupsWidth + 10 + width + littleGap, StartY, font);
            return StartY;
        }

    }
}
