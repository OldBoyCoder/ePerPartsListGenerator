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
using System.Linq;

namespace ePerPartsListGenerator.Repository
{
    public class AccessRelease20Repository : IRepository
    {
        private OleDbConnection _conn;
        private readonly string _languageCode;
        private readonly string _basePath;

        public AccessRelease20Repository(string languageCode, string basePath)
        {
            _languageCode = languageCode;
            _basePath = basePath;
        }

        public List<Catalogue> GetAllCatalogues()
        {
            Open();

            var catalogues = new List<Catalogue>();
            var sql = "select CATALOGUES.CAT_COD, MAKES.MK_DSC, COMM_MODGRP.CMG_DSC, CATALOGUES.CAT_DSC, MAKES.MK_COD from ((CATALOGUES INNER JOIN MAKES ON MAKES.MK_COD = CATALOGUES.MK_COD) INNER JOIN COMM_MODGRP ON COMM_MODGRP.MK2_COD = CATALOGUES.MK2_COD AND COMM_MODGRP.CMG_COD = CATALOGUES.CMG_COD) order by MAKES.MK_DSC, COMM_MODGRP.CMG_DSC, CATALOGUES.CAT_DSC";
            var cmd = new OleDbCommand(sql, _conn);
            var dr = cmd.ExecuteReader();
            while (dr.Read())
            {
                var cat = new Catalogue
                {
                    Make = dr.GetString(1),
                    CatCode = dr.GetString(0),
                    Model = dr.GetString(2),
                    Description = dr.GetString(3),
                    MakeCode = dr.GetString(4)
                };
                catalogues.Add(cat);
            }
            Close();
            return catalogues;
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
        private void GetAllGroupEntries(Catalogue cat)
        {
            var d = new List<Group>();
            var sql = $"SELECT DISTINCT A.GRP_COD, A.GRP_DSC FROM GROUPS_DSC A, SGSEQS B WHERE A.GRP_COD = B.GRP_COD AND A.LNG_COD = '{_languageCode}' AND B.CAT_COD = '{cat.CatCode}' ORDER BY A.GRP_COD ";
            var cmd = new OleDbCommand(sql, _conn);
            using (var dr = cmd.ExecuteReader())
            {
                while (dr.Read())
                {
                    var g = new Group();
                    g.Code = dr.GetInt16(0).ToString();
                    var mapName = GetMapNameForGroup(cat, g);

                    g.ImageStream = GetImageForGroup(mapName);
                    g.Description = dr.GetString(1);
                    d.Add(g);
                }
                dr.Close();
            }
            cat.Groups = d;
        }
        private string GetMapNameForGroup(Catalogue catalogue, Group group)
        {
            // First try a vehicle specific table
            var sql = $"SELECT MAP_SGRP FROM MAP_VET WHERE CAT_COD = '{catalogue.CatCode}' AND GRP_COD = {group.Code}";
            var cmd = new OleDbCommand(sql, _conn);
            using (var dr = cmd.ExecuteReader())
            {
                if (dr.Read())
                {
                    return dr.GetString(0);
                }
                dr.Close();
            }
            // if not try the general table
            sql = $"SELECT MAP_NAME FROM MAP_SGRP WHERE GRP_COD = {group.Code} ORDER BY SGRP_COD";
            cmd = new OleDbCommand(sql, _conn);
            using (var dr = cmd.ExecuteReader())
            {
                if (dr.Read())
                {
                    return dr.GetString(0);
                }
                dr.Close();
            }
            return "";

        }

        private void GetGroupTables(Catalogue catalogue, Group group)
        {
            group.Tables = new List<Table>();
            var sql =
                $"SELECT S.SGRP_COD, SGRP_DSC, SGS_COD FROM SGSEQS S, SUBGROUPS_DSC SD WHERE SD.GRP_COD = S.GRP_COD AND SD.SGRP_COD = S.SGRP_COD AND LNG_COD = '{_languageCode}' AND S.CAT_COD = '{catalogue.CatCode}'  AND S.GRP_COD = {group.Code}  order by S.SGRP_COD, SGS_COD";
            var cmd = new OleDbCommand(sql, _conn);
            using (var dr = cmd.ExecuteReader())
            {
                while (dr.Read())
                {
                    var t = new Table();
                    t.Description = dr.GetString(1);
                    t.TableCode = dr.GetByte(0);
                    t.SubGroupCode = dr.GetByte(2);
                    t.FullCode = group.Code + t.TableCode.ToString("00") + "/" + t.SubGroupCode.ToString("00");
                    group.Tables.Add(t);
                }

                dr.Close();
            }
        }

        private void GetAllModificationLegendEntries(Catalogue cat)
        {
            var d = new Dictionary<string, string>();
            var sql = "SELECT DISTINCT TBDATA_MOD.MDF_COD, MDF_DSC,ACT_MASK,  MDFACT_SPEC from " +
                $"(([TBDATA_MOD]  INNER JOIN[MODIF_DSC] ON(TBDATA_MOD.MDF_COD = MODIF_DSC.MDF_COD AND MODIF_DSC.LNG_COD = '{_languageCode}'))" +
                "INNER JOIN MDF_ACT ON (MDF_ACT.CAT_COD = TBDATA_MOD.CAT_COD AND MDF_ACT.MDF_COD = TBDATA_MOD.MDF_COD))" +
                "INNER JOIN ACTIVATIONS ON (MDF_ACT.ACT_COD = ACTIVATIONS.ACT_COD)" +
                $"WHERE TBDATA_MOD.CAT_COD = '{cat.CatCode}'" +
                "order by TBDATA_MOD.MDF_COD;";
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

                    d[mdfCode] += $" [{dr.GetString(2)} {dr.GetString(3)}]";
                }

                dr.Close();
            }

            cat.AllModifications = d;

        }

        private void Open()
        {
            var password = "\u0001\u0007\u0014\u0007\u0001\u00f3\u001b\n\n\u00d2\u001e\u00da\u00b1";
            OleDbConnectionStringBuilder builder = new OleDbConnectionStringBuilder();
            builder.ConnectionString = $"Data Source={_basePath}\\SP.DB.00900.FCTLR";
            builder.Add("Provider", "Microsoft.Jet.Oledb.4.0");
            builder.Add("Jet OLEDB:Database Password", password);
            _conn = new OleDbConnection(builder.ConnectionString);
            _conn.Open();

        }
        private void Close()

        {
            _conn.Close();
        }

        private Catalogue ReadCatalogue(string catCode)
        {
            var sql = $"select C.MK_COD, CAT_DSC, CMG_COD, MK_DSC from CATALOGUES C INNER JOIN MAKES M ON (M.MK_COD = C.MK_COD) where cat_cod = '{catCode}'";
            var cmd = new OleDbCommand(sql, _conn);
            var dr = cmd.ExecuteReader();
            var cat = new Catalogue();
            if (dr.Read())
            {
                cat.MakeCode = dr.GetString(0);
                cat.Description = dr.GetString(1);
                cat.Make = dr.GetString(3);
                cat.ImageBytes = GetImageForCatalogue(dr.GetString(2));
            }

            dr.Close();
            cat.CatCode = catCode;
            return cat;
        }
        private void GetTableDrawings(Catalogue catalogue, Group group, Table table)
        {
            table.Drawings = new List<Drawing>();
            var sql =
                " SELECT DRAWINGS.DRW_NUM, SGRP_COD, SGS_COD " +
                "FROM DRAWINGS " +
                $"WHERE CAT_COD = @P1 AND GRP_COD = @P2 AND SGRP_COD = @P3 AND SGS_COD = @P4 " +
                "ORDER BY SGRP_COD, SGS_COD, DRAWINGS.DRW_NUM";
            var cmd = new OleDbCommand(sql, _conn);
            cmd.Parameters.AddWithValue("@P1", catalogue.CatCode);
            cmd.Parameters.AddWithValue("@P2", group.Code);
            cmd.Parameters.AddWithValue("@P3", table.TableCode);
            cmd.Parameters.AddWithValue("@P4", table.SubGroupCode);

            using (var dr = cmd.ExecuteReader())
            {
                while (dr.Read())
                {
                    var d = new Drawing();

                    d.DrawingNo = dr.GetByte(0);
                    d.ImageStream = GetImageForDrawing(catalogue, group, table, d);
                    d.TableCode = group.Code + table.TableCode.ToString("00") + "/" + dr.GetByte(2).ToString("00");
                    d.SgsCode = dr.GetByte(2);
                    d.Modifications = GetModificationsForSubGroup(catalogue, group, table);
                    if (d.Modifications != "")
                    {
                        var modItems = d.Modifications.Split(',');
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
                    d.ValidFor = GetVariationsAndOptionsForSubGroup(catalogue, group, table);
                    d.CompatibilityList.AddRange(d.ValidFor.Split(new[] { ',', '+', '(', ')', ' ', '!', '\n' },
                        System.StringSplitOptions.RemoveEmptyEntries));

                    table.Drawings.Add(d);
                }

                dr.Close();
            }

            foreach (var d in table.Drawings) AddParts(catalogue.CatCode, group, table, d);
        }
        private string GetModificationsForSubGroup(Catalogue cat, Group group, Table table)
        {

            var sql =
                " SELECT SGSMOD_CD, MDF_COD " +
                "FROM SGS_MOD " +
                $"WHERE CAT_COD = '{cat.CatCode}' AND GRP_COD = {group.Code} AND SGRP_COD = {table.TableCode} AND SGS_COD = {table.SubGroupCode} " +
                "ORDER BY SGSMOD_SEQ";
            var cmd = new OleDbCommand(sql, _conn);
            var mods = new List<string>();
            using (var dr = cmd.ExecuteReader())
            {
                while (dr.Read())
                {
                    mods.Add(dr.GetString(0) + dr.GetInt16(1).ToString("0000"));
                }
            }
            return string.Join(",", mods);

        }
        private string GetVariationsAndOptionsForSubGroup(Catalogue cat, Group group, Table table)
        {
            var sql =
                " SELECT VMK_TYPE, VMK_COD " +
                "FROM SGS_VAL " +
                $"WHERE CAT_COD = '{cat.CatCode}' AND GRP_COD = {group.Code} AND SGRP_COD = {table.TableCode} AND SGS_COD = {table.SubGroupCode} " +
                "ORDER BY VMK_COD";
            var cmd = new OleDbCommand(sql, _conn);
            cmd.Parameters.AddWithValue("@P1", cat.CatCode);
            cmd.Parameters.AddWithValue("@P2", group.Code);
            cmd.Parameters.AddWithValue("@P3", table.TableCode);
            cmd.Parameters.AddWithValue("@P4", table.SubGroupCode);
            var vars = new List<string>();
            using (var dr = cmd.ExecuteReader())
            {
                while (dr.Read())
                {
                    vars.Add(dr.GetString(0) + dr.GetString(1));
                }
            }
            sql =
                " SELECT OPTK_TYPE, OPTK_COD " +
                "FROM SGS_OPT " +
                $"WHERE CAT_COD = @P1 AND GRP_COD = @P2 AND SGRP_COD = @P3 AND SGS_COD = @P4;";
            cmd = new OleDbCommand(sql, _conn);
            cmd.Parameters.AddWithValue("@P1", cat.CatCode);
            cmd.Parameters.AddWithValue("@P2", group.Code);
            cmd.Parameters.AddWithValue("@P3", table.TableCode);
            cmd.Parameters.AddWithValue("@P4", table.SubGroupCode);
            using (var dr = cmd.ExecuteReader())
            {
                while (dr.Read())
                {
                    vars.Add(dr.GetString(0) + dr.GetString(1));
                }
            }
            return string.Join(",", vars);

        }
        private MemoryStream GetImageForDrawing(Catalogue cat, Group group, Table table, Drawing drawing)
        {
            // Generate file name
            var fileName = Path.Combine(_basePath, @"SP.NA.00900.FCTLR", $"{cat.MakeCode}{cat.CatCode}.na");
            var imageName = $"{group.Code}{table.TableCode.ToString("00")}{table.SubGroupCode.ToString("00")}{drawing.DrawingNo.ToString("000")}";
            return GetImageFromNaFile(fileName, imageName);
        }

        private MemoryStream GetImageForGroup(string mapName)
        {
            // Generate file name
            var fileName = Path.Combine(_basePath, "SP.MP.00900.FCTLR", $"{mapName}.jpg");
            var fileBytes = File.ReadAllBytes(fileName);
            return new MemoryStream(fileBytes);
        }
        private MemoryStream GetImageForCatalogue(string cmgCode)
        {
            // Generate file name
            var lines = File.ReadAllLines(Path.Combine(_basePath, @"SP.IM.00900.FXXXX\img.conf"));
            var matches = lines.Where(x => x.Contains($",{cmgCode},"));
            var line = matches.FirstOrDefault(x => x.Contains("s2"));
            if (line == null)
            {
                line = matches.FirstOrDefault(x => x.Contains("s1"));
                if (line == null) return null;
            }
            var fileName = line.Split(new[] { ',' })[3];
            return new MemoryStream(File.ReadAllBytes(Path.Combine(_basePath, @"SP.IM.00900.FXXXX", fileName)));
        }

        private static MemoryStream GetImageFromNaFile(string fileName, string imageName)
        {
            var reader = new BinaryReader(File.OpenRead(fileName));
            reader.ReadInt16();
            Int32 numberOfEntries = reader.ReadInt16();
            for (int i = 0; i < numberOfEntries; i++)
            {
                reader.ReadInt16(); // image index
                byte[] imageNameBytes = reader.ReadBytes(10);
                string entry = System.Text.Encoding.ASCII.GetString(imageNameBytes);
                Int32 mainImageStart = reader.ReadInt32();
                Int32 mainImageLength = reader.ReadInt32();
                reader.ReadInt32();  // thumbnail start
                reader.ReadInt32(); // thumbnail size
                if (entry == imageName)
                {
                    reader.BaseStream.Seek(mainImageStart, SeekOrigin.Begin);
                    var imageBytes = reader.ReadBytes(mainImageLength);
                    return new MemoryStream(imageBytes);
                }
            }
            return null;
        }

        private MemoryStream GetImageForCliche(Int32 clicheCode)
        {
            // Generate file name
            var fileName = Path.Combine(_basePath, @"SP.NA.00900.FCTLR", $"cliche.na");
            var imageName = clicheCode.ToString("0000000000");
            return GetImageFromNaFile(fileName, imageName);
        }
        private void AddParts(string catCode, Group group, Table table, Drawing drawing)
        {
            var sql =
                "select TBD_RIF, PRT_COD, TRIM(CODES_DSC.CDS_DSC +' '+IIF(ISNULL(TBDATA.TBD_AGG_DSC), '', TBDATA.TBD_AGG_DSC)), NULL, TBDATA.TBD_QTY, TBDATA.TBD_VAL_FORMULA, NOTES_DSC.NTS_DSC, NULL, GRP_COD, TBD_SEQ " +
                $"from (TBDATA INNER JOIN CODES_DSC ON (TBDATA.CDS_COD = CODES_DSC.CDS_COD AND CODES_DSC.LNG_COD = '{_languageCode}')) " +
                $"LEFT JOIN NOTES_DSC ON (TBDATA.NTS_COD = NOTES_DSC.NTS_COD AND NOTES_DSC.LNG_COD = '{_languageCode}')  " +
                            $"where CAT_COD = '{catCode}' and grp_COD = {group.Code} AND SGRP_COD = {table.TableCode} AND SGS_COD = {drawing.SgsCode} AND DRW_NUM = {drawing.DrawingNo} order by TBD_RIF, TBD_SEQ";
            drawing.Parts = new List<Part>();
            using (var cmd = new OleDbCommand(sql, _conn))
            {

                using (var dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        var p = new Part { Description = dr.GetString(2), Modification = new List<string>() };

                        p.PartNo = dr.GetDouble(1).ToString();
                        p.Qty = dr.IsDBNull(4) ? "" : dr.GetString(4);
                        p.Rif = dr.GetByte(0);
                        p.Compatibility = new List<string>();
                        if (!dr.IsDBNull(5))
                        {
                            var compatibility = dr.GetString(5);
                            p.Compatibility.AddRange(compatibility.Split(new[] { ',', '+', '(', ')', ' ', '!', '\n' },
                                System.StringSplitOptions.RemoveEmptyEntries));
                            foreach (var c in p.Compatibility)
                                if (!drawing.CompatibilityList.Contains(c))
                                    drawing.CompatibilityList.Add(c);
                        }

                        p.Notes = dr.IsDBNull(6) ? "" : dr.GetString(6);
                        p.Sequence = dr.GetString(9);
                        drawing.Parts.Add(p);
                    }
                    dr.Close();
                }
            }
            // Now get modifications for all parts just retrieved
            foreach (var part in drawing.Parts)
            {
                sql = "SELECT TBDM_CD,MDF_COD FROM TBDATA_MOD WHERE ";
                sql += $"CAT_COD = '{catCode}' AND GRP_COD = {group.Code} AND SGRP_COD = {table.TableCode} AND SGS_COD = {drawing.SgsCode} AND TBD_RIF = {part.Rif} AND TBD_SEQ = '{part.Sequence}' AND DRW_NUM = {drawing.DrawingNo}";
                using (var cmd = new OleDbCommand(sql, _conn))
                {
                    using (var dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            part.Modification.Add(dr.GetString(0) + dr.GetInt16(1).ToString("0000"));
                        }
                    }
                }
                foreach (var c in part.Modification)
                {
                    var mod = c.Substring(1);
                    if (!drawing.ModificationList.Contains(mod))
                        drawing.ModificationList.Add(mod);
                }
                // see if there is a cliche
                sql = $"SELECT DISTINCT CLH_COD, CPD_NUM FROM CPXDATA WHERE CPLX_PRT_COD = {part.PartNo} order by CPD_NUM";
                using (var cmd = new OleDbCommand(sql, _conn))
                {
                    using (var dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            part.HasCliche = true;
                            var clicheCode = dr.GetInt32(0);
                            var cliche = drawing.Cliches.FirstOrDefault(x => x.ClicheCode == clicheCode && x.PartNo == part.PartNo);
                            if (cliche == null)
                            {
                                cliche = new Cliche(clicheCode);
                                drawing.Cliches.Add(cliche);
                                cliche.PartNo = part.PartNo;
                                cliche.Description = part.Description;
                                cliche.CpdNum = dr.GetInt16(1);
                            }
                        }
                    }
                }
            }
            PopulateCliches(drawing, catCode);
        }
        private void GetAllVariantLegendEntries(Catalogue cat)
        {
            var d = new Dictionary<string, string>();
            var sql =
                $"SELECT VMK_TYPE + VMK_COD, VMK_DSC FROM VMK_DSC where cat_cod = '{cat.CatCode}' and lng_cod = '{_languageCode}' order by VMK_type";
            var cmd = new OleDbCommand(sql, _conn);
            using (var dr = cmd.ExecuteReader())
            {
                while (dr.Read()) d.Add(dr.GetString(0), dr.GetString(1));

                dr.Close();
            }
            sql =
                $"SELECT OPTK_TYPE + OPTK_COD, OPTK_DSC FROM OPTKEYS_DSC where lng_cod = '{_languageCode}' order by OPTK_type";
            cmd = new OleDbCommand(sql, _conn);
            using (var dr = cmd.ExecuteReader())
            {
                while (dr.Read()) d.Add(dr.GetString(0), dr.GetString(1));

                dr.Close();
            }

            cat.AllVariants = d;
        }
        private void PopulateCliches(Drawing d, string catCode)
        {
            foreach (var item in d.Cliches)
            {
                // use cliche code to get the image
                item.ImageStream = GetImageForCliche(item.ClicheCode);
                // Now get that parts for the cliches;
                var sql = "SELECT CPD_RIF, CPXDATA.PRT_COD, TRIM(CDS_DSC + ' ' +IIF(ISNULL(CPD_AGG_DSC), '', CPD_AGG_DSC)), NULL, CPD_QTY, NULL, '', CLH_COD ";
                sql += " FROM((CPXDATA";
                sql += " INNER JOIN PARTS ON(PARTS.PRT_COD = CPXDATA.PRT_COD))";
                sql += $" INNER JOIN CODES_DSC ON(PARTS.CDS_COD = CODES_DSC.CDS_COD AND LNG_COD = '{_languageCode}'))";
                sql += $" WHERE CPXDATA.CLH_COD = {item.ClicheCode} AND CPXDATA.CPD_NUM = {item.CpdNum} AND CPXDATA.CPLX_PRT_COD = {item.PartNo}";
                sql += " ORDER BY CPD_RIF, CPD_RIF_SEQ";

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

                    p.PartNo = dr.GetDouble(1).ToString();
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
                sql = "select TBD_RIF, D.PRT_COD, C.CDS_DSC, '','01', '', '', '' " +
                      $"from (KIT AS D " +
                        " INNER JOIN PARTS ON(PARTS.PRT_COD = D.PRT_COD)) " +
                    $" INNER JOIN CODES_DSC AS C ON (C.CDS_COD = PARTS.CDS_COD AND LNG_COD = '{_languageCode}') " +
                $"where D.CPLX_PRT_COD = {item.PartNo} AND CAT_COD = '{catCode}' order by TBD_RIF";
                cmd = new OleDbCommand(sql, _conn);
                dr = cmd.ExecuteReader();
                while (dr.Read())
                {
                    var p = new Part
                    {
                        Description = dr.GetString(2),
                        Modification = new List<string>(),
                        PartNo = dr.GetDouble(1).ToString(),
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
    }
}
