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
using System;
using System.Collections.Generic;
using System.Data.OleDb;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace ePerPartsListGenerator.Repository
{
    public class AccessRelease84Repository : IRepository
    {
        private OleDbConnection _conn;
        private readonly string _languageCode;
        private readonly string _basePath;

        public AccessRelease84Repository(string languageCode, string basePath)
        {
            _languageCode = languageCode;
            _basePath = basePath;
        }

        private void Open()
        {
            var password = "\u0001\u0007\u0014\u0007\u0001\u00f3\u001b\n\n\u00d2\u001e\u00da\u00b1";
            OleDbConnectionStringBuilder builder = new OleDbConnectionStringBuilder();
            builder.ConnectionString = $"Data Source={_basePath}\\data\\SP.DB.04210.FCTLR";
            builder.Add("Provider", "Microsoft.Jet.Oledb.4.0");
            builder.Add("Jet OLEDB:Database Password", password);
            _conn = new OleDbConnection(builder.ConnectionString);
            _conn.Open();

        }

        private Catalogue ReadCatalogue(string catCode)
        {
            var sql = $"select CAT_DSC, IMG_NAME, C.MK_COD, MK_DSC from CATALOGUES C INNER JOIN MAKES M ON (M.MK_COD = C.MK_COD) where cat_cod = '{catCode}'";
            var cmd = new OleDbCommand(sql, _conn);
            var dr = cmd.ExecuteReader();
            var cat = new Catalogue();
            if (dr.Read())
            {
                cat.Description = dr.GetString(0);
                cat.ImageBytes = GetImage(dr.GetString(1));
                cat.MakeCode = dr.GetString(2);
                cat.Make = dr.GetString(3);
            }

            dr.Close();
            cat.CatCode = catCode;
            return cat;
        }

        public Catalogue GetCatalogue(string catCode)
        {
            Open();
            var cat = ReadCatalogue(catCode);
            GetAllModificationLegendEntries(cat);
            GetAllVariantLegendEntries(cat);
            GetAllGroupEntries(cat);
            foreach (var group in cat.Groups)
            {
                GetGroupTables(cat, group);
                foreach (var table in group.Tables)
                    GetTableDrawings(cat, group, table);
            }

            Close();
            return cat;
        }

        private void GetTableDrawings(Catalogue catalogue, Group group, Table table)
        {
            table.Drawings = new List<Drawing>();
            var sql =
                " SELECT DRAWINGS.DRW_NUM, DRAWINGS.VARIANTE, DRAWINGS.REVISIONE, IMG_PATH, " +
                "DRAWINGS.MODIF, IIF(ISNULL(Drawings.PATTERN), '', Drawings.PATTERN), TABLE_COD, SGS_COD " +
                "FROM DRAWINGS " +
                $"WHERE DRAWINGS.CAT_COD = '{catalogue.CatCode}' AND GRP_COD = {group.Code} AND SGRP_COD = {table.TableCode}  AND SGS_COD = {table.SubGroupCode} " +
                "ORDER BY DRAWINGS.DRW_NUM, DRAWINGS.VARIANTE DESC, DRAWINGS.REVISIONE DESC";
            var cmd = new OleDbCommand(sql, _conn);
            using (var dr = cmd.ExecuteReader())
            {
                while (dr.Read())
                {
                    var d = new Drawing
                    {
                        DrawingNo = dr.GetInt16(0),
                        ImageStream = GetImage(dr.GetString(3)),
                        Revision = dr.GetInt16(2),
                        Variant = dr.GetInt16(1),
                        TableCode = dr.GetString(6),
                        SgsCode = dr.GetInt16(7)
                    };
                    if (!dr.IsDBNull(4))
                    {
                        var mods = dr.GetString(4);
                        d.Modifications = mods;
                        var modItems = mods.Split(',');
                        foreach (var item in modItems)
                        {
                            var mod = item.Substring(1);
                            if (!d.ModificationList.Contains(mod))
                                d.ModificationList.Add(mod);
                        }
                    }
                    else
                    {
                        d.Modifications = "";
                    }

                    d.ValidFor = dr.GetString(5);
                    d.CompatibilityList.AddRange(d.ValidFor.Split(new[] { ',', '+', '(', ')', ' ', '!', '\n' },
                        System.StringSplitOptions.RemoveEmptyEntries));

                    table.Drawings.Add(d);
                }

                dr.Close();
            }

            foreach (var d in table.Drawings) AddParts(catalogue.CatCode, group, table, d);
        }

        private void GetGroupTables(Catalogue catalogue, Group group)
        {
            group.Tables = new List<Table>();
            var sql =
                $" SELECT DISTINCT S.SGRP_COD, SGRP_DSC, D.SGS_COD FROM (SUBGROUPS_BY_CAT S INNER JOIN SUBGROUPS_DSC SD ON (SD.GRP_COD = S.GRP_COD AND SD.SGRP_COD = S.SGRP_COD AND LNG_COD = '{_languageCode}')) INNER JOIN DRAWINGS D ON (D.CAT_COD = S.CAT_COD AND D.GRP_COD = S.GRP_COD AND D.SGRP_COD = S.SGRP_COD)   WHERE S.CAT_COD = '{catalogue.CatCode}' AND S.GRP_COD = {group.Code} order by S.SGRP_COD, D.SGS_COD";
            var cmd = new OleDbCommand(sql, _conn);
            using (var dr = cmd.ExecuteReader())
            {
                while (dr.Read())
                {
                    var t = new Table { Description = dr.GetString(1), TableCode = dr.GetInt16(0) };
                    t.SubGroupCode = (byte)dr.GetInt16(2);
                    t.FullCode = group.Code + t.TableCode.ToString("00")+"/"+t.SubGroupCode.ToString("00");
                    group.Tables.Add(t);
                }

                dr.Close();
            }
        }


        private void GetAllModificationLegendEntries(Catalogue cat)
        {
            var d = new Dictionary<string, string>();
            var sql = "select D.MDF_COD, IIF(ISNULL(MDF_DSC), '', MDF_DSC), MDFACT_SPEC, A.ACT_COD from modif_DSC D " +
                      "INNER JOIN MDF_ACT A ON (A.CAT_COD = D.CAT_COD AND A.MDF_COD = D.MDF_COD) " +
                      $"where D.CAT_COD = '{cat.CatCode}' and LNG_COD = '{_languageCode}'	order by D.mdf_COD, A.MDFACT_PROG";
            var lastCode = "";
            var cmd = new OleDbCommand(sql, _conn);
            using (var dr = cmd.ExecuteReader())
            {
                while (dr.Read())
                {
                    var mdfCode = dr.GetInt16(0).ToString();
                    if (mdfCode != lastCode)
                    {
                        lastCode = mdfCode;
                        d.Add(mdfCode, $"{dr.GetString(1)}");
                    }

                    d[mdfCode] += $" [{dr.GetString(3)} {dr.GetString(2)}]";
                }

                dr.Close();
            }

            cat.AllModifications = d;
        }

        private void GetAllVariantLegendEntries(Catalogue cat)
        {
            var d = new Dictionary<string, string>();
            var sql =
                $"SELECT IIF(ISNULL(VMK_TYPE), '', VMK_TYPE) + IIF(ISNULL(VMK_COD), '', VMK_COD), VMK_DSC FROM VMK_DSC where cat_cod = '{cat.CatCode}' and lng_cod = '{_languageCode}' order by VMK_type";
            var cmd = new OleDbCommand(sql, _conn);
            using (var dr = cmd.ExecuteReader())
            {
                while (dr.Read()) d.Add(dr.GetString(0), dr.GetString(1));

                dr.Close();
            }

            cat.AllVariants = d;
        }

        private void GetAllGroupEntries(Catalogue cat)
        {
            var d = new List<Group>();
            var sql =
                $"SELECT G.GRP_COD, IMG_NAME, GD.GRP_DSC FROM GROUPS AS G INNER JOIN  GROUPS_DSC AS GD ON (CStr(GD.GRP_COD) = G.GRP_COD AND LNG_COD = '{_languageCode}') where cat_cod = '{cat.CatCode}' order by G.GRP_COD";
            var cmd = new OleDbCommand(sql, _conn);
            using (var dr = cmd.ExecuteReader())
            {
                while (dr.Read())
                {
                    var g = new Group
                    {
                        Code = dr.GetString(0),
                        ImageStream = GetImage(dr.GetString(1)),
                        Description = dr.GetString(2)
                    };
                    d.Add(g);
                }

                dr.Close();
            }

            cat.Groups = d;
        }

        private void AddParts(string catCode, Group group, Table table, Drawing d)
        {
            var sql =
                "select TBD_RIF, PRT_COD, TRIM(C.CDS_DSC + ' ' +IIF(ISNULL(DAD.DSC), '', DAD.DSC)), D.MODIF, IIF(ISNULL(D.TBD_QTY), '', D.TBD_QTY), D.TBD_VAL_FORMULA, IIF(ISNULL(NTS.NTS_DSC), '', NTS.NTS_DSC), ''" +
                $"from (((TBDATA D INNER JOIN CODES_DSC C ON (C.CDS_COD = D.CDS_COD AND C.LNG_COD = '{_languageCode}')) " +
                $"LEFT OUTER JOIN DESC_AGG_DSC DAD ON (DAD.COD = D.TBD_AGG_DSC AND DAD.LNG_COD = '{_languageCode}'))  " +
                $"LEFT OUTER JOIN [NOTES_DSC] NTS ON (NTS.NTS_COD = D.NTS_COD AND NTS.LNG_COD = '{_languageCode}'))  " +
                $"where CAT_COD = '{catCode}' and grp_COD = {group.Code} AND SGRP_COD = {table.TableCode} AND SGS_COD = {d.SgsCode} and VARIANTE = {d.Variant}  order by TBD_RIF, TBD_SEQ";
            d.Parts = new List<Part>();
            var cmd = new OleDbCommand(sql, _conn);
            using (var dr = cmd.ExecuteReader())
            {
                while (dr.Read())
                {
                    var p = new Part { Description = dr.GetString(2), Modification = new List<string>() };
                    if (!dr.IsDBNull(3))
                    {
                        var mods = dr.GetString(3);
                        p.Modification.AddRange(mods.Split(','));
                        foreach (var c in p.Modification)
                        {
                            var mod = c.Substring(1);
                            if (!d.ModificationList.Contains(mod))
                                d.ModificationList.Add(mod);
                        }
                    }

                    p.PartNo = dr.GetString(1);
                    p.Qty = dr.GetString(4);
                    p.Rif = dr.GetInt16(0);
                    p.Compatibility = new List<string>();
                    if (!dr.IsDBNull(5))
                    {
                        var compatibility = dr.GetString(5);
                        p.Compatibility.AddRange(compatibility.Split(new[] { ',', '+', '(', ')', ' ', '!', '\n' },
                            System.StringSplitOptions.RemoveEmptyEntries));
                        foreach (var c in p.Compatibility)
                            if (!d.CompatibilityList.Contains(c))
                                d.CompatibilityList.Add(c);
                    }

                    p.Notes = dr.GetString(6);
                    GetClicheCodesForPart(d, p);
                    d.Parts.Add(p);
                }

                dr.Close();
            }

            PopulateCliches(d, catCode);
        }
        private void GetClicheCodesForPart(Drawing d, Part p)
        {
            var sql = $"SELECT CLH_COD, IMG_PATH, CPD_NUM FROM [CLICHE] CL WHERE CL.CPLX_PRT_COD = '{p.PartNo}' order by  CPD_NUM";
            using (var cmd = new OleDbCommand(sql, _conn))
            {
                using (var dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        var clicheCode = Int32.Parse(dr.GetString(0));
                        var cliche = d.Cliches.FirstOrDefault(x => x.ClicheCode == clicheCode  && x.PartNo == p.PartNo);
                        if (cliche == null)
                        {
                            cliche = new Cliche(clicheCode);
                            d.Cliches.Add(cliche);
                            cliche.PartNo = p.PartNo;
                            cliche.Description = p.Description;
                            cliche.ImageStream = GetImage(dr.GetString(1));
                            cliche.CpdNum = dr.GetInt16(2);
                        }

                    }
                }
            }
        }
        private void PopulateCliches(Drawing d, string catCode)
        {
            foreach (var item in d.Cliches)
            {
                // Now get that parts for the cliches;
                var sql =
                    "select CPD_RIF, PRT_COD, TRIM(C.CDS_DSC + ' ' +IIF(ISNULL(DAD.DSC), '', DAD.DSC)), D.MODIF, D.CPD_QTY, '', IIF(ISNULL(NTS.NTS_DSC), '', NTS.NTS_DSC), IIF(ISNULL(D.CLH_COD), '', D.CLH_COD) " +
                    $"from ((CPXDATA D INNER JOIN CODES_DSC C ON (C.CDS_COD = D.PRT_CDS_COD AND C.LNG_COD = '{_languageCode}')) " +
                    $"LEFT OUTER JOIN DESC_AGG_DSC DAD ON (DAD.COD = D.CPD_AGG_DSC AND DAD.LNG_COD = '{_languageCode}'))  " +
                    $"LEFT OUTER JOIN [NOTES_DSC] NTS ON (NTS.NTS_COD = D.NTS_COD AND NTS.LNG_COD = '{_languageCode}')  " +
                    $"where D.CLH_COD = {item.ClicheCode} AND D.CPD_NUM = {item.CpdNum} AND D.CPLX_PRT_COD = '{item.PartNo}'  order by CpD_RIF, CPD_RIF_SEQ";
                var cmd = new OleDbCommand(sql, _conn);
                var dr = cmd.ExecuteReader();
                item.Parts = new List<Part>();
                while (dr.Read())
                {
                    var p = new Part { Description = dr.GetString(2), Modification = new List<string>() };
                    if (!dr.IsDBNull(3))
                    {
                        var mods = dr.GetString(3);
                        p.Modification.AddRange(mods.Split(','));
                        foreach (var c in p.Modification)
                        {
                            var mod = c.Substring(1);
                            if (!d.ModificationList.Contains(mod))
                                d.ModificationList.Add(mod);
                        }
                    }

                    p.PartNo = dr.GetString(1);
                    p.Qty = dr.GetString(4);
                    p.Rif = dr.GetInt16(0);
                    p.Compatibility = new List<string>();
                    if (!dr.IsDBNull(5))
                    {
                        var compatibility = dr.GetString(5);
                        p.Compatibility.AddRange(compatibility.Split(new[] { ',', '+', '(', ')', ' ', '!', '\n' },
                            System.StringSplitOptions.RemoveEmptyEntries));
                        foreach (var c in p.Compatibility)
                            if (!d.CompatibilityList.Contains(c))
                                d.CompatibilityList.Add(c);
                    }

                    p.Notes = dr.GetString(6);
                    item.Parts.Add(p);
                }

                dr.Close();
                var starterRif = item.Parts.Max(x => x.Rif) + 1;
                // Now see if there are any KIT entries for this cliche
                sql = "select TBD_RIF, PRT_COD, C.CDS_DSC, '','01', '', '', '' " +
                      $"from KIT D INNER JOIN CODES_DSC C ON (C.CDS_COD = D.CDS_COD AND LNG_COD = '{_languageCode}') " +
                      $"where D.CPLX_PRT_COD = '{item.PartNo}' AND CAT_COD = '{catCode}' order by TBD_RIF";
                cmd = new OleDbCommand(sql, _conn);
                dr = cmd.ExecuteReader();
                while (dr.Read())
                {
                    var p = new Part
                    {
                        Description = dr.GetString(2),
                        Modification = new List<string>(),
                        PartNo = dr.GetString(1),
                        Qty = dr.GetString(4),
                        Rif = starterRif++,
                        Compatibility = new List<string>(),
                        Notes = dr.GetString(6),
                    };
                    item.Parts.Add(p);
                }

                dr.Close();
            }
        }

        public List<Catalogue> GetAllCatalogues()
        {
            Open();

            var catalogues = new List<Catalogue>();
            var sql = "select C.CAT_COD, M.MK_DSC, MG.CMG_DSC, C.CAT_DSC from (CATALOGUES C INNER JOIN MAKES M ON (M.MK_COD = C.MK_COD)) INNER JOIN COMM_MODGRP MG ON (MG.MK2_COD = C.MK2_COD AND MG.CMG_COD = C.CMG_COD) order by M.MK_DSC, MG.CMG_DSC, C.CAT_DSC";
            var cmd = new OleDbCommand(sql, _conn);
            var dr = cmd.ExecuteReader();
            while (dr.Read())
            {
                var cat = new Catalogue
                {
                    Make = dr.GetString(1),
                    CatCode = dr.GetString(0),
                    Model = dr.GetString(2),
                    Description = dr.GetString(3)
                };
                catalogues.Add(cat);
            }

            Close();
            return catalogues;
        }
        private void Close()
        {
            _conn.Close();
        }

        private MemoryStream GetImage(string imagePath)
        {
            var parts = imagePath.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            // Work out filename for zip file
            var fileName = Path.Combine(_basePath, @"data\images", $"{parts[0]}.res");
            using (var file = File.OpenRead(fileName))
            {
                using (var zip = new ZipArchive(file, ZipArchiveMode.Read))
                {
                    var e = zip.GetEntry(parts[1]);
                    if (e == null)
                    {
                        return GetImageFromEperFig(imagePath);
                    }
                    using (var stream = e.Open())
                    {
                        var ms = new MemoryStream();
                        stream.CopyTo(ms);
                        ms.Position = 0;
                        return ms;
                    }
                }
            }
        }
        private MemoryStream GetImageFromEperFig(string imagePath)
        {
            var parts = imagePath.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            // Work out filename for zip file
            var fileName = Path.Combine(_basePath, @"data\images", $"L_EPERFIG.res");
            using (var file = File.OpenRead(fileName))
            {
                using (var zip = new ZipArchive(file, ZipArchiveMode.Read))
                {
                    var e = zip.GetEntry(imagePath);
                    if (e == null)
                    {
                        Console.WriteLine(imagePath);
                        return null;
                    }
                    using (var stream = e.Open())
                    {
                        var ms = new MemoryStream();
                        stream.CopyTo(ms);
                        ms.Position = 0;
                        return ms;
                    }
                }
            }
        }

    }
}
