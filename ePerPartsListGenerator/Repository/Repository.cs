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

using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using ePerPartsListGenerator.Model;

// ReSharper disable StringLiteralTypo

namespace ePerPartsListGenerator.Repository
{
    internal class Repository
    {
        private SqlConnection _conn;
        private readonly string _languageCode;

        internal Repository(string languageCode)
        {
            _languageCode = languageCode;
        }

        private void Open()
        {
            var cb = new SqlConnectionStringBuilder
            {
                InitialCatalog = "ePer", DataSource = "localhost", IntegratedSecurity = true
            };
            _conn = new SqlConnection(cb.ConnectionString);
            _conn.Open();
        }

        private Catalogue ReadCatalogue(string catCode)
        {
            var sql = $"select CAT_DSC, IMG_NAME from CATALOGUES where cat_cod = '{catCode}'";
            var cmd = new SqlCommand(sql, _conn);
            var dr = cmd.ExecuteReader();
            var cat = new Catalogue();
            if (dr.Read())
            {
                cat.Description = dr.GetString(0);
                cat.ImagePath = @"L_EPERFIG/" + dr.GetString(1);
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
                " SELECT DRAWINGS.DRW_NUM, DRAWINGS.VARIANTE, DRAWINGS.REVISIONE, DRAWINGS.IMG_PATH, " +
                "DRAWINGS.MODIF, ISNULL(Drawings.PATTERN, ''), TABLE_COD, SGS_COD " +
                "FROM DRAWINGS " +
                $"WHERE DRAWINGS.CAT_COD = '{catalogue.CatCode}' AND GRP_COD = {group.Code} AND SGRP_COD = {table.TableCode} " +
                //"and(PATTERN LIKE '%M2%' or PATTERN IS NULL) "+
                "ORDER BY DRAWINGS.DRW_NUM, DRAWINGS.VARIANTE DESC, DRAWINGS.REVISIONE DESC";
            var cmd = new SqlCommand(sql, _conn);
            using (var dr = cmd.ExecuteReader())
            {
                while (dr.Read())
                {
                    var d = new Drawing
                    {
                        DrawingNo = dr.GetInt16(0),
                        ImagePath = dr.GetString(3),
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
                    d.CompatibilityList.AddRange(d.ValidFor.Split(new[] {',', '+', '(', ')', ' ', '!', '\n'},
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
                $" SELECT S.SGRP_COD, SGRP_DSC FROM SUBGROUPS_BY_CAT S JOIN SUBGROUPS_DSC SD ON SD.GRP_COD = S.GRP_COD AND SD.SGRP_COD = S.SGRP_COD AND LNG_COD = '{_languageCode}' WHERE S.CAT_COD = '{catalogue.CatCode}' AND S.GRP_COD = {group.Code} order by SGRP_COD";
            var cmd = new SqlCommand(sql, _conn);
            using (var dr = cmd.ExecuteReader())
            {
                while (dr.Read())
                {
                    var t = new Table {Description = dr.GetString(1), TableCode = dr.GetInt16(0)};
                    t.FullCode = group.Code + t.TableCode.ToString("00");
                    group.Tables.Add(t);
                }

                dr.Close();
            }
        }


        private void GetAllModificationLegendEntries(Catalogue cat)
        {
            var d = new Dictionary<string, string>();
            var sql = "select D.MDF_COD, ISNULL(MDF_DSC, ''), MDFACT_SPEC, A.ACT_COD from modif_DSC D " +
                      "JOIN MDF_ACT A ON A.CAT_COD = D.CAT_COD AND A.MDF_COD = D.MDF_COD " +
                      $"where D.CAT_COD = '{cat.CatCode}' and LNG_COD = '{_languageCode}'	order by mdf_COD, A.MDFACT_PROG";
            var lastCode = "";
            var cmd = new SqlCommand(sql, _conn);
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
                $"SELECT ISNULL(VMK_TYPE, '') + ISNULL(VMK_COD, ''), VMK_DSC FROM VMK_DSC where cat_cod = '{cat.CatCode}' and lng_cod = '{_languageCode}' order by VMK_type";
            var cmd = new SqlCommand(sql, _conn);
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
                $"SELECT G.GRP_COD, ISNULL(IMG_NAME, ''), GD.GRP_DSC FROM GROUPS G JOIN GROUPS_DSC GD ON GD.GRP_COD = G.GRP_COD AND LNG_COD = '{_languageCode}'  where cat_cod = '{cat.CatCode}' order by G.GRP_COD";
            var cmd = new SqlCommand(sql, _conn);
            using (var dr = cmd.ExecuteReader())
            {
                while (dr.Read())
                {
                    var g = new Group
                    {
                        Code = dr.GetString(0),
                        ImageName = @"L_EPERFIG/" + dr.GetString(1),
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
                "select TBD_RIF, PRT_COD, TRIM(C.CDS_DSC + ' ' +ISNULL(DAD.DSC, '')), D.MODIF, D.TBD_QTY, D.TBD_VAL_FORMULA, ISNULL(NTS.NTS_DSC, ''), ISNULL(CL.CLH_COD, ''), ISNULL(CL.IMG_PATH, '') " +
                $"from TBDATA D  JOIN CODES_DSC C ON C.CDS_COD = D.CDS_COD AND LNG_COD = '{_languageCode}' " +
                $"LEFT OUTER JOIN DESC_AGG_DSC DAD ON DAD.COD = D.TBD_AGG_DSC AND DAD.LNG_COD = '{_languageCode}'  " +
                $"LEFT OUTER JOIN [NOTES_DSC] NTS ON NTS.NTS_COD = D.NTS_COD AND NTS.LNG_COD = '{_languageCode}'  " +
                "LEFT OUTER JOIN [CLICHE] CL ON Cl.CPLX_PRT_COD = D.PRT_COD " +
                $"where CAT_COD = '{catCode}' and grp_COD = '{group.Code}' AND SGRP_COD = {table.TableCode} AND SGS_COD = {d.SgsCode} and VARIANTE = {d.Variant}  order by TBD_RIF, TBD_SEQ";
            d.Parts = new List<Part>();
            var cmd = new SqlCommand(sql, _conn);
            using (var dr = cmd.ExecuteReader())
            {
                while (dr.Read())
                {
                    var p = new Part {Description = dr.GetString(2), Modification = new List<string>()};
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
                        p.Compatibility.AddRange(compatibility.Split(new[] {',', '+', '(', ')', ' ', '!', '\n'},
                            System.StringSplitOptions.RemoveEmptyEntries));
                        foreach (var c in p.Compatibility)
                            if (!d.CompatibilityList.Contains(c))
                                d.CompatibilityList.Add(c);
                    }

                    p.Notes = dr.GetString(6);
                    p.ClicheCode = dr.GetString(7);
                    if (p.ClicheCode != "")
                    {
                        if (!d.Cliches.ContainsKey(p.PartNo))
                            d.Cliches.Add(p.PartNo, new Cliche(p.ClicheCode));
                        d.Cliches[p.PartNo].PartNo = p.PartNo;
                        d.Cliches[p.PartNo].Description = p.Description;
                        d.Cliches[p.PartNo].ImagePath = dr.GetString(8);
                    }

                    d.Parts.Add(p);
                }

                dr.Close();
            }

            PopulateCliches(d, catCode);
        }

        private void PopulateCliches(Drawing d, string catCode)
        {
            foreach (var item in d.Cliches)
            {
                // Now get that parts for the cliches;
                var sql =
                    "select CPD_RIF, PRT_COD, TRIM(C.CDS_DSC + ' ' +ISNULL(DAD.DSC, '')), D.MODIF, D.CPD_QTY, '', ISNULL(NTS.NTS_DSC, ''), ISNULL(D.CLH_COD, '') " +
                    $"from CPXDATA D  JOIN CODES_DSC C ON C.CDS_COD = D.PRT_CDS_COD AND LNG_COD = '{_languageCode}' " +
                    $"LEFT OUTER JOIN DESC_AGG_DSC DAD ON DAD.COD = D.CPD_AGG_DSC AND DAD.LNG_COD = '{_languageCode}'  " +
                    $"LEFT OUTER JOIN [NOTES_DSC] NTS ON NTS.NTS_COD = D.NTS_COD AND NTS.LNG_COD = '{_languageCode}'  " +
                    $"where D.CPLX_PRT_COD = '{item.Key}' AND D.CLH_COD = '{item.Value.ClicheCode}'  order by CpD_RIF, CPD_RIF_SEQ";
                var cmd = new SqlCommand(sql, _conn);
                var dr = cmd.ExecuteReader();
                item.Value.Parts = new List<Part>();
                while (dr.Read())
                {
                    var p = new Part {Description = dr.GetString(2), Modification = new List<string>()};
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
                        p.Compatibility.AddRange(compatibility.Split(new[] {',', '+', '(', ')', ' ', '!', '\n'},
                            System.StringSplitOptions.RemoveEmptyEntries));
                        foreach (var c in p.Compatibility)
                            if (!d.CompatibilityList.Contains(c))
                                d.CompatibilityList.Add(c);
                    }

                    p.Notes = dr.GetString(6);
                    p.ClicheCode = "";
                    item.Value.Parts.Add(p);
                }

                dr.Close();
                var starterRif = item.Value.Parts.Max(x => x.Rif) + 1;
                // Now see if there are any KIT entries for this cliche
                sql = "select TBD_RIF, PRT_COD, C.CDS_DSC, '','01', '', '', '' " +
                      $"from KIT D  JOIN CODES_DSC C ON C.CDS_COD = D.CDS_COD AND LNG_COD = '{_languageCode}' " +
                      $"where D.CPLX_PRT_COD = '{item.Key}' AND CAT_COD = '{catCode}' order by TBD_RIF";
                cmd = new SqlCommand(sql, _conn);
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
                        ClicheCode = ""
                    };
                    item.Value.Parts.Add(p);
                }

                dr.Close();
            }
        }

        private void Close()
        {
            _conn.Close();
        }
    }
}