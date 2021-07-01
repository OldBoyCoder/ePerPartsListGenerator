﻿using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using ePerPartsListGenerator.Model;
using PdfSharp.Drawing;
using PdfSharp.Pdf;

namespace ePerPartsListGenerator.Render
{
    class CatalogueRendererLandscape

    {
        private readonly Catalogue _catalogue;
        private double _groupsWidth;
        private double _punchMargin;
        private double _littleGap;
        private long _pageNumber = 1;
        private double _contentsY;
        private string _lastGroup;
        private PdfDocument _doc;
        private PdfPage _page;
        private XGraphics _gfx;
        private double _groupY;
        public Dictionary<string, long> TablesPages = new Dictionary<string, long>();
        public Dictionary<string, long> GroupsPages = new Dictionary<string, long>();
        public bool DocumentPerSection = false;
        private RenderFont _tableFont;
        private RenderFont _titleFont;
        private RenderFont _contentsFont;
        private RenderFont _footerFont;
        private RenderFont _groupFont;
        private double _workingWidth;
        private double _partsListTotalWidth;

        private readonly XStringFormat[] _partsListAlignments = { XStringFormats.TopLeft, XStringFormats.TopRight, XStringFormats.TopLeft, XStringFormats.TopLeft, XStringFormats.TopLeft, XStringFormats.TopLeft, XStringFormats.TopLeft, XStringFormats.TopLeft };
        private readonly XStringFormat[] _legendListAlignments = { XStringFormats.TopLeft, XStringFormats.TopLeft };
        private readonly XStringFormat[] _contentAlignments = { XStringFormats.TopLeft, XStringFormats.TopLeft, XStringFormats.TopRight };

        private List<double> _legendListWidths;
        private List<double> _contentWidths;
        private List<double> _partsListWidths;

        public CatalogueRendererLandscape(Catalogue catalogue)
        {
            _catalogue = catalogue;
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
            _doc = new PdfDocument();

            _page = _doc.AddPage();
            _gfx = XGraphics.FromPdfPage(_page);
            CreateFonts();

            _page.Size = PdfSharp.PageSize.A4;
            _page.Orientation = PdfSharp.PageOrientation.Portrait;

            _punchMargin = GetWidth(_page, 8);
            _groupsWidth = CalculateGroupsWidth();
            _littleGap = GetWidth(_page, 0.5);
            _workingWidth = GetWidth(_page, 100) - _groupsWidth - _punchMargin - _littleGap;
            _partsListWidths = GenerateColumnWidths(0.05, 0.12, 0.31, 0.15, 0.12, 0.15, 0.05, 0.05);
            _legendListWidths = GenerateColumnWidths(0.2, 0.8);
            _contentWidths = GenerateColumnWidths(0.2, 0.7, 0.1);
            _partsListTotalWidth = _partsListWidths.Sum(x => x) + (_partsListWidths.Count - 1) * _littleGap;

            DrawTitlePage();

        }

        private void DrawTitlePage()
        {
            AddPageFooter();

            var y = GetHeight(_page, 10);
            y += DrawStringCentre(_gfx, "Parts list", _punchMargin, y, _page.Width - _punchMargin, _titleFont);
            y += DrawStringCentre(_gfx, $"{_catalogue.CatCode} - {_catalogue.Description}", _punchMargin, y, _page.Width - _punchMargin, _titleFont);
            y += DrawStringCentre(_gfx, $"Produced on {DateTime.Now:G}", _punchMargin, y, _page.Width - _punchMargin, _titleFont);
            y += DrawStringCentre(_gfx, $"Using version {Assembly.GetExecutingAssembly().GetName().Version}", _punchMargin, y, _page.Width - _punchMargin, _titleFont);
            y = DrawDrawing(_catalogue.ImagePath, y + 20);
            _contentsY = y;
        }

        private void CreateFonts()
        {
            var fontName = "Arial";
            _titleFont = new RenderFont(new XFont(fontName, 14, XFontStyle.Regular));
            _contentsFont = new RenderFont(new XFont(fontName, 10, XFontStyle.Regular));
            _footerFont = new RenderFont(new XFont(fontName, 6, XFontStyle.Regular));
            _groupFont = new RenderFont(new XFont(fontName, 7, XFontStyle.Regular));
            _tableFont = new RenderFont(new XFont(fontName, 7, XFontStyle.Regular));
        }

        public void AddPageFooter()
        {
            var y = GetHeight(_page, 95);
            DrawStringCentre(_gfx, "Source data is Copyright(c) 2011, Fiat Group Automobiles.  Code to produce this PDF is Copyright(c) 2021, Chris Reynolds.", _punchMargin, y, _page.Width - _punchMargin * 2, _footerFont);
            DrawStringRight(_gfx, $"Page: {_pageNumber}", _punchMargin, y, _page.Width - _punchMargin * 2, _footerFont);
            _pageNumber++;
        }
        public void AddGroups(Catalogue catalogue)
        {
            _groupY = 0.0;
            _lastGroup = "";
            var titleDoc = _doc;
            FileStream zipToOpen = null;
            ZipArchive archive = null;
            if (DocumentPerSection)
            {
                var zipPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                    $"Parts_{catalogue.CatCode}.zip");
                zipToOpen = new FileStream(zipPath, FileMode.Create);
                archive = new ZipArchive(zipToOpen, ZipArchiveMode.Create);
            }

            foreach (var group in catalogue.Groups)
            {
                if (DocumentPerSection)
                {
                    _doc = new PdfDocument();
                }
                GroupsPages.Add(group.Code, _pageNumber);
                // Add section header page
                DrawSectionHeaderPageHolder(group);
                var headerPage = _page;
                AddTables(catalogue, group);
                // we can go back and add the page numbers to the header page
                DrawSectionHeaderPage(headerPage, group);
                if (DocumentPerSection && archive != null)
                {
                    // we need to save the document
                    //_doc.Save($"c:\\temp\\Parts_{catalogue.CatCode}_{group.Description}.pdf");
                    var entry = archive.CreateEntry($"Parts_{group.Code}_{group.Description}.pdf", CompressionLevel.Optimal);
                    _doc.Save(entry.Open(), true);

                }
            }
            // Go back and add contents to main page
            AddMainPageContents(catalogue, titleDoc);

            if (DocumentPerSection)
            {
                var entry = archive?.CreateEntry($"Parts_{catalogue.CatCode}_title.pdf", CompressionLevel.Optimal);

                titleDoc.Save(entry?.Open(), true);
                archive?.Dispose();
                zipToOpen?.Close();
                zipToOpen?.Dispose();
            }
            else
            {
                var pdfPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                    $"Parts_{catalogue.CatCode}.pdf");
                _doc.Save(pdfPath);
            }

        }

        private void AddMainPageContents(Catalogue catalogue, PdfDocument titleDoc)
        {
            _gfx.Dispose();
            _page = titleDoc.Pages[0];
            _gfx = XGraphics.FromPdfPage(_page);
            var y = DrawGroupListHeaders(_contentsY, _contentWidths, _punchMargin);
            foreach (var group in catalogue.Groups)
            {
                var fields = new[] { group.Code, group.Description, GroupsPages[group.Code].ToString() };
                var drawItems = FitTableItemsIntoColumns(_contentWidths, fields, _contentsFont);
                y = DrawAllItems(y, _contentsFont, _contentWidths, _punchMargin, drawItems, _contentAlignments);
            }
        }

        public void AddTables(Catalogue catalogue, Group group)
        {
            foreach (var table in group.Tables)
            {
                TablesPages.Add(table.FullCode, _pageNumber);
                AddDrawings(catalogue, group, table);
            }
        }

        private List<double> GenerateColumnWidths(params double[] widths)
        {
            var returnList = new List<double>();
            for (int i = 0; i < widths.Length; i++)
            {
                if (i < widths.Length - 1)
                    returnList.Add(_workingWidth * widths[i] - _littleGap);
                else
                    returnList.Add(_workingWidth * widths[i] - _littleGap);
            }

            return returnList;
        }
        public void AddDrawings(Catalogue catalogue, Group group, Table table)
        {
            foreach (var drawing in table.Drawings)
            {
                var y = StartNewDrawingPage(group, table, drawing);
                var h = _tableFont.Height(_gfx);
                h += h / 8;
                var tableLeft = _punchMargin;
                var gray = true;
                var lastRif = -1;
                y = DrawPartsTableHeaders(y, tableLeft);

                foreach (var p in drawing.Parts)
                {
                    var drawItems = CreatePartsListRowData(p);
                    if (y + (drawItems.Count * h) > GetHeight(_page, 90))
                    {
                        AddContinuedMarker(y);
                        y = StartNewDrawingPage(group, table, drawing);
                        y = DrawPartsTableHeaders(y, tableLeft);
                    }
                    if (lastRif != p.Rif)
                    {
                        gray = !gray;
                        lastRif = p.Rif;
                    }
                    if (gray)
                    {
                        _gfx.DrawRectangle(XBrushes.LightGray, new XRect(tableLeft, y, _partsListTotalWidth, h * drawItems.Count));
                    }
                    y = DrawAllItems(y, _tableFont, _partsListWidths, tableLeft, drawItems, _partsListAlignments);
                }

                if (drawing.ModificationList.Count > 0 || drawing.CompatibilityList.Count > 0)
                {
                    y = DrawLegendTableHeaders(y, tableLeft);

                    y = AddListToLegend(drawing.ModificationList, catalogue.AllModifications, y, table, group, drawing, _tableFont, tableLeft);

                    AddListToLegend(drawing.CompatibilityList, catalogue.AllVariants, y, table, group, drawing, _tableFont, tableLeft);
                }
                // There's chance that there are are sub-assemblies to draw
                AddCliches(group, table, drawing, tableLeft);
            }
        }

        private List<List<string>> CreatePartsListRowData(Part p)
        {
            var fields = new[] { p.Rif.ToString(), p.PartNo, p.Description,
                        string.Join(", ", p.Modification), string.Join(", ", p.Compatibility),p.Notes, p.Qty, p.ClicheCode != ""?"*": "" };
            var drawItems = FitTableItemsIntoColumns(_partsListWidths, fields, _tableFont);
            return drawItems;
        }

        private void AddCliches(Group group, Table table, Drawing drawing, double tableLeft)
        {
            var h = _tableFont.Height(_gfx);
            h += h / 8;

            foreach (var cliche in drawing.Cliches.Values)
            {
                var y = StartNewClichePage(group, table, drawing, cliche);
                var gray = true;
                var lastRif = -1;
                y = DrawPartsTableHeaders(y, tableLeft);

                foreach (var p in cliche.Parts)
                {
                    var drawItems = CreatePartsListRowData(p);
                    if (y + (drawItems.Count * h) > GetHeight(_page, 90))
                    {
                        AddContinuedMarker(y);
                        y = StartNewClichePage(group, table, drawing, cliche);
                        y = DrawPartsTableHeaders(y, tableLeft);
                    }
                    if (lastRif != p.Rif)
                    {
                        gray = !gray;
                        lastRif = p.Rif;
                    }
                    if (gray)
                    {
                        _gfx.DrawRectangle(XBrushes.LightGray, new XRect(tableLeft, y, _partsListTotalWidth, h * drawItems.Count));
                    }
                    y = DrawAllItems(y, _tableFont, _partsListWidths, tableLeft, drawItems, _partsListAlignments);
                }
            }
        }

        private void AddContinuedMarker(double y)
        {
            DrawStringRight(_gfx, "Continued...", _punchMargin, y, _partsListTotalWidth, _tableFont);
        }

        private double AddListToLegend(List<string> data, Dictionary<string, string> lookUps, double y, Table table, Group group, Drawing drawing, RenderFont font,
            double tableLeft)
        {
            var h = font.Height(_gfx);
            foreach (var item in data.OrderBy(x => x))
            {
                var fields = new[] { item, lookUps.ContainsKey(item) ? lookUps[item] : "" };
                var drawItems = FitTableItemsIntoColumns(_legendListWidths, fields, font);
                if (y + (drawItems.Count * h) > GetHeight(_page, 90))
                {
                    AddContinuedMarker(y);
                    y = StartNewDrawingPage(group, table, drawing);
                    y = DrawLegendTableHeaders(y, tableLeft);
                }
                y = DrawAllItems(y, font, _legendListWidths, tableLeft, drawItems, _legendListAlignments);
            }
            return y;
        }

        private double StartNewDrawingPage(Group group, Table table, Drawing drawing)
        {
            if (_gfx != null)
            {
                _gfx.Dispose();
                _gfx = null;
            }
            _page = _doc.AddPage();
            _gfx = XGraphics.FromPdfPage(_page);
            _page.Size = PdfSharp.PageSize.A4;
            _page.Orientation = PdfSharp.PageOrientation.Portrait;
            AddPageFooter();
            DrawGroupTags(group);
            var y = DrawDrawingPageTitle(group, table, drawing);
            y = DrawDrawing(drawing.ImagePath, y);
            return y;
        }
        private double StartNewClichePage(Group group, Table table, Drawing drawing, Cliche cliche)
        {
            if (_gfx != null)
            {
                _gfx.Dispose();
                _gfx = null;
            }
            _page = _doc.AddPage();
            _gfx = XGraphics.FromPdfPage(_page);
            _page.Size = PdfSharp.PageSize.A4;
            _page.Orientation = PdfSharp.PageOrientation.Portrait;
            AddPageFooter();
            DrawGroupTags(group);
            var y = DrawClichePageTitle(group, table, drawing, cliche);
            y = DrawDrawing(cliche.ImagePath, y);
            return y;
        }

        private double DrawAllItems(double y, RenderFont font, List<double> widths, double tableLeft, List<List<string>> drawItems, XStringFormat[] alignments)
        {
            var h = font.Height(_gfx);
            foreach (var line in drawItems)
            {
                var x = tableLeft;
                for (int i = 0; i < line.Count; i++)
                {
                    _gfx.DrawString(line[i], font.Font, XBrushes.Black,
                        new XRect(x, y + h / 16.0, widths[i], h + h / 8.0), alignments[i]);
                    x += widths[i];
                    x += _littleGap;
                }
                y += (h + h / 8.0);
            }
            return y;
        }
        private double FitAndDrawAllItems(double y, RenderFont font, List<double> widths, double tableLeft, string[] drawItems, XStringFormat[] alignments)
        {
            var items = FitTableItemsIntoColumns(widths, drawItems, font);
            return DrawAllItems(y, font, widths, tableLeft, items, alignments);
        }

        private List<List<string>> FitTableItemsIntoColumns(List<double> widths, string[] fields, RenderFont font)
        {
            var maxLines = 1;
            var rc = new List<List<string>>();
            // so work out if each column fits in its width, if not work out how many rows are needed
            // then return a 2D array of the split items
            for (int i = 0; i < widths.Count; i++)
            {
                var lines = BreakGroupIntoLines(widths[i], font, _gfx, fields[i]);
                if (lines.Count > maxLines)
                    maxLines = lines.Count;
            }
            for (int i = 0; i < maxLines; i++)
            {
                rc.Add(new List<string>());
                for (int j = 0; j < widths.Count; j++)
                {
                    var lines = BreakGroupIntoLines(widths[j], font, _gfx, fields[j]);
                    rc[i].Add(lines.Count - 1 < i ? "" : lines[i]);
                }
            }
            return rc;
        }

        private double DrawPartsTableHeaders(double y, double tableLeft)
        {
            var headings = new List<string> { "", "Part #", "Description", "Modification", "Compatibility", "Notes", "Qty", "Sub" };
            return DrawTableHeaders(y, _partsListWidths, headings, tableLeft, _tableFont);
        }
        private double DrawLegendTableHeaders(double y, double tableLeft)
        {
            var headings = new List<string> { "Legend", "Notes" };
            return DrawTableHeaders(y, _legendListWidths, headings, tableLeft, _tableFont);
        }

        private double DrawTableHeaders(double y, List<double> widths, List<string> headers, double tableLeft, RenderFont font)
        {
            var h = font.Height(_gfx);
            h += h / 8;
            var x = tableLeft;
            for (int i = 0; i < widths.Count; i++)
            {
                _gfx.DrawRectangle(XBrushes.Black, new XRect(x, y, widths[i], h));
                _gfx.DrawString(headers[i], font.Font, XBrushes.White,
                    new XRect(x, y, widths[i], h), XStringFormats.Center);
                x = x + widths[i] + _littleGap;
            }

            return y + h;
        }

        private double DrawTableListHeaders(double y, List<double> widths, double tableLeft)
        {
            var headings = new List<string> { "Table", "Description", "Page" };
            return DrawTableHeaders(y, widths, headings, tableLeft, _contentsFont);
        }

        private double DrawGroupListHeaders(double y, List<double> widths, double tableLeft)
        {
            var headings = new List<string> { "Group", "Description", "Page" };
            return DrawTableHeaders(y, widths, headings, tableLeft, _contentsFont);
        }

        private double CalculateGroupsWidth()
        {
            var allGroupsInTwoLines = false;
            var initialWidth = GetWidth(_page, 8);

            while (!allGroupsInTwoLines)
            {
                allGroupsInTwoLines = true;
                foreach (var group in _catalogue.Groups)
                {
                    List<string> lines = BreakGroupIntoLines(initialWidth, _groupFont, _gfx, group.Description);
                    if (lines.Count > 2)
                    {
                        allGroupsInTwoLines = false;
                        break;
                    }
                }
                if (!allGroupsInTwoLines)
                    initialWidth += GetWidth(_page, 0.5);
            }
            return initialWidth;
        }
        private List<string> BreakGroupIntoLines(double initialWidth, RenderFont font, XGraphics gfx, string group)
        {
            var parts = group.Split(' ').ToList();
            var lines = new List<string>();
            var text = "";
            while (parts.Count > 0)
            {
                var oldText = text;
                text = text + " " + parts[0];
                text = text.Trim();
                var textWidth = gfx.MeasureString(text, font.Font);
                if (textWidth.Width > initialWidth)
                {
                    lines.Add(oldText);
                    // Need to write out old text
                    text = parts[0];
                }
                parts.RemoveAt(0);
            }
            lines.Add(text);
            return lines;
        }
        private double DrawDrawing(string imagePath, double startY)
        {
            XImage image = XImage.FromFile(@"c:\temp\ePer\images\" + imagePath);
            var xx = GetWidth(_page, 80) - (_groupsWidth + _littleGap + _punchMargin);
            var yy = xx;
            if (image.PixelWidth > image.PixelHeight)
            {
                yy = ((image.PixelHeight * xx) / image.PixelWidth);
            }
            else
            {
                xx = ((image.PixelWidth * yy) / image.PixelHeight);

            }

            var centreX = _punchMargin + GetWidth(_page, 80) / 2.0;
            _gfx.DrawImage(image, centreX - xx / 2, startY + _littleGap, xx, yy);
            return startY + _littleGap + yy + _littleGap;
        }

        private void DrawGroupTags(Group group)
        {
            var groupLineHeight = _groupFont.Height(_gfx);
            if (_lastGroup != group.Description)
            {
                _groupY += (2 * groupLineHeight);
                _lastGroup = group.Description;
            }
            _gfx.DrawRectangle(XBrushes.Black, new XRect(_page.Width - _groupsWidth, _groupY, _groupsWidth, groupLineHeight * 2.0));
            var lines = BreakGroupIntoLines(_groupsWidth, _groupFont, _gfx, group.Description);
            for (var i = 0; i < lines.Count; i++)
            {

                _gfx.DrawString(lines[i], _groupFont.Font, XBrushes.White,
                    new XRect(_page.Width - _groupsWidth, _groupY + i * groupLineHeight, _groupsWidth, _page.Height),
                    XStringFormats.TopLeft);
            }
        }
        private double DrawString(XGraphics gfx, string str, double x, double y, RenderFont font)
        {
            var rect = gfx.MeasureString(str, font.Font);
            gfx.DrawString(str, font.Font, XBrushes.Black, new XRect(x, y, rect.Width, rect.Height), XStringFormats.TopLeft);
            return rect.Height;
        }
        private double DrawStringCentre(XGraphics gfx, string str, double x, double y, double w, RenderFont font)
        {
            var rect = gfx.MeasureString(str, font.Font);
            gfx.DrawString(str, font.Font, XBrushes.Black, new XRect(x, y, w, rect.Height), XStringFormats.TopCenter);
            return rect.Height;
        }
        // ReSharper disable once UnusedMethodReturnValue.Local
        private double DrawStringRight(XGraphics gfx, string str, double x, double y, double w, RenderFont font)
        {
            var rect = gfx.MeasureString(str, font.Font);
            gfx.DrawString(str, font.Font, XBrushes.Black, new XRect(x, y, w, rect.Height), XStringFormats.TopRight);
            return rect.Height;
        }
        private double DrawDrawingPageTitle(Group group, Table table, Drawing drawing)
        {
            var startY = GetHeight(_page, 5);
            // Create a font
            // Draw the text
            var drawString = $"{drawing.TableCode} - {table.Description} - {drawing.DrawingNo}";
            startY += DrawString(_gfx, drawString, _punchMargin, startY, _titleFont);
            var di = new[] { "Catalogue", $"{_catalogue.CatCode} - {_catalogue.Description}" };
            startY = FitAndDrawAllItems(startY, _contentsFont, _legendListWidths, _punchMargin, di, _legendListAlignments);
            di = new[] { "Group", $"[{group.Code}] {group.Description}" };
            startY = FitAndDrawAllItems(startY, _contentsFont, _legendListWidths, _punchMargin, di, _legendListAlignments);
            di = new[] { "Table", $"[{drawing.TableCode}] {table.Description}" };
            startY = FitAndDrawAllItems(startY, _contentsFont, _legendListWidths, _punchMargin, di, _legendListAlignments);
            di = new[] { "Variant", drawing.Variant.ToString() };
            startY = FitAndDrawAllItems(startY, _contentsFont, _legendListWidths, _punchMargin, di, _legendListAlignments);
            di = new[] { "Revision", drawing.Revision.ToString() };
            startY = FitAndDrawAllItems(startY, _contentsFont, _legendListWidths, _punchMargin, di, _legendListAlignments);
            di = new[] { "Modifications", drawing.Modifications };
            startY = FitAndDrawAllItems(startY, _contentsFont, _legendListWidths, _punchMargin, di, _legendListAlignments);
            di = new[] { "Valid for", drawing.ValidFor };
            startY = FitAndDrawAllItems(startY, _contentsFont, _legendListWidths, _punchMargin, di, _legendListAlignments);
            return startY;
        }
        private double DrawClichePageTitle(Group group, Table table, Drawing drawing, Cliche cliche)
        {
            var startY = GetHeight(_page, 5);
            // Create a font
            // Draw the text
            var drawString = $"{cliche.PartNo} - {cliche.Description} ";
            startY += DrawString(_gfx, drawString, _punchMargin, startY, _titleFont);
            var di = new[] { "Catalogue", $"{_catalogue.CatCode} - {_catalogue.Description}" };
            startY = FitAndDrawAllItems(startY, _contentsFont, _legendListWidths, _punchMargin, di, _legendListAlignments);
            di = new[] { "Group", $"[{group.Code}] {group.Description}" };
            startY = FitAndDrawAllItems(startY, _contentsFont, _legendListWidths, _punchMargin, di, _legendListAlignments);
            di = new[] { "Table", $"[{drawing.TableCode}] {table.Description}" };
            startY = FitAndDrawAllItems(startY, _contentsFont, _legendListWidths, _punchMargin, di, _legendListAlignments);
            return startY;
        }
        public void DrawSectionHeaderPage(PdfPage page, Group group)
        {
            _gfx = XGraphics.FromPdfPage(page);
            DrawGroupTags(group);
            var y = GetHeight(page, 10);
            y += DrawStringCentre(_gfx, $"{group.Code} - {group.Description}", _punchMargin, y, page.Width - _punchMargin - _groupsWidth -_littleGap, _titleFont);
            y = DrawDrawing(group.ImageName, y + 20);

            y = DrawTableListHeaders(y, _contentWidths, _punchMargin);

            foreach (var table in group.Tables)
            {
                var fields = new[] { table.FullCode, table.Description, TablesPages[table.FullCode].ToString() };
                var drawItems = FitTableItemsIntoColumns(_contentWidths, fields, _contentsFont);
                y = DrawAllItems(y, _contentsFont, _contentWidths, _punchMargin, drawItems, _contentAlignments);
            }

        }
        public void DrawSectionHeaderPageHolder(Group group)
        {
            if (_gfx != null)
            {
                _gfx.Dispose();
                _gfx = null;
            }
            _page = _doc.AddPage();
            _gfx = XGraphics.FromPdfPage(_page);
            _page.Size = PdfSharp.PageSize.A4;
            _page.Orientation = PdfSharp.PageOrientation.Portrait;
            AddPageFooter();
        }

    }
}