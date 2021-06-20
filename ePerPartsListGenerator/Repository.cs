using System.Collections.Generic;
using System.Data.SqlClient;

namespace ePerPartsListGenerator
{
    class Repository
    {
        SqlConnection conn;
        public void Open()
        {
            var cb = new SqlConnectionStringBuilder();
            cb.InitialCatalog = "ePer";
            cb.DataSource = "localhost";
            cb.IntegratedSecurity = true;
            conn = new SqlConnection(cb.ConnectionString);
            conn.Open();

        }
        public void GetCatalogue(Catalogue cat, string CatCode)
        {
            var sql = $"select CAT_DSC, IMG_NAME from CATALOGUES where cat_cod = '{CatCode}'";
            var cmd = new SqlCommand(sql, conn);
            var dr = cmd.ExecuteReader();
            if (dr.Read())
            {
                cat.Description = dr.GetString(0);
                cat.ImagePath = @"L_EPERFIG\" + dr.GetString(1);
            }
            dr.Close();

        }
        public List<Drawing> GetDrawings(Catalogue cat, string CatCode)
        {
            var drawings = new List<Drawing>();
            var sql = $" SELECT DRAWINGS.TABLE_COD, TABLES_DSC.DSC, DRAWINGS.DRW_NUM, DRAWINGS.VARIANTE, DRAWINGS.REVISIONE, DRAWINGS.IMG_PATH, DRAWINGS.PATTERN,DRAWINGS.MODIF, GD.GRP_DSC, GD.GRP_COD, DRAWINGS.MODIF, ISNULL(Drawings.PATTERN, '')   FROM DRAWINGS INNER JOIN TABLES_DSC ON DRAWINGS.TABLE_DSC_COD = TABLES_DSC.COD inner join GROUPS_DSC GD ON GD.GRP_COD = drawings.GRP_COD and gd.LNG_COD = '3' WHERE DRAWINGS.CAT_COD = '{CatCode}' AND TABLES_DSC.LNG_COD = '3' /*and(PATTERN LIKE '%M1%' or PATTERN IS NULL)*/ ORDER BY DRAWINGS.TABLE_COD, DRAWINGS.DRW_NUM, DRAWINGS.VARIANTE DESC, DRAWINGS.REVISIONE DESC";
            var cmd = new SqlCommand(sql, conn);
            var dr = cmd.ExecuteReader();
            while (dr.Read())
            {
                var d = new Drawing();
                d.Description = dr.GetString(1);
                d.DrawingNo = dr.GetInt16(2);
                d.ImagePath = dr.GetString(5);
                d.Revision = dr.GetInt16(4);
                d.Variante = dr.GetInt16(3);
                d.TableCode = dr.GetString(0);
                d.GroupDesc = dr.GetString(8);
                d.GroupCode = dr.GetInt16(9).ToString();
                if (!dr.IsDBNull(10))
                {
                    string mods = dr.GetString(10);
                    d.Modifications = mods;
                    var modItems = mods.Split(new[] { ',' });
                    foreach (var item in modItems)
                    {
                        var mod = item.Substring(1);
                        if (!d.ModificationList.Contains(mod))
                            d.ModificationList.Add(mod);
                    }
                }
                else
                    d.Modifications = "";
                d.ValidFor = dr.GetString(11);
                d.CompatibilityList.AddRange(d.ValidFor.Split(new[] { ',', '+', '(', ')', ' ', '!', '\n' }, System.StringSplitOptions.RemoveEmptyEntries));

                drawings.Add(d);
            }
            dr.Close();
            foreach (var d in drawings)
            {
                AddParts(CatCode, d);
                //foreach (var item in d.ModificationList)
                //{
                //    AddLegendEntryForModification(cat, item);
                //}

            }
            return drawings;
        }
        public Dictionary<string, string> GetAllModificationLegendEntries(Catalogue cat)
        {
            var d = new Dictionary<string, string>();
            var sql = $"select D.MDF_COD, MDF_DSC, MDFACT_SPEC, A.ACT_COD from modif_DSC D " +
                "JOIN MDF_ACT A ON A.CAT_COD = D.CAT_COD AND A.MDF_COD = D.MDF_COD " +
                $"where D.CAT_COD = '{cat.CatCode}' and LNG_COD = '3'	order by mdf_COD, A.MDFACT_PROG";
            var lastCode = "";
            var cmd = new SqlCommand(sql, conn);
            var dr = cmd.ExecuteReader();
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
            return d;

        }
        public Dictionary<string, string> GetAllVariants(Catalogue cat)
        {
            var d = new Dictionary<string, string>();
            var sql = $"SELECT ISNULL(VMK_TYPE, '') + ISNULL(VMK_COD, ''), VMK_DSC FROM VMK_DSC where cat_cod = '{cat.CatCode}' and lng_cod = '3' order by VMK_type";
            var cmd = new SqlCommand(sql, conn);
            var dr = cmd.ExecuteReader();
            while (dr.Read())
            {
                d.Add(dr.GetString(0), dr.GetString(1));
            }
            dr.Close();
            return d;

        }

        private void AddParts(string CatCode, Drawing d)
        {
            var sql = $"select TBD_RIF, PRT_COD, TRIM(C.CDS_DSC + ' ' +ISNULL(DAD.DSC, '')), D.MODIF, D.TBD_QTY, D.TBD_VAL_FORMULA, ISNULL(NTS.NTS_DSC, ''), ISNULL(CL.CLH_COD, ''), ISNULL(CL.IMG_PATH, '') " +
                $"from TBDATA D  JOIN CODES_DSC C ON C.CDS_COD = D.CDS_COD AND LNG_COD = '3' " +
                $"LEFT OUTER JOIN DESC_AGG_DSC DAD ON DAD.COD = D.TBD_AGG_DSC AND DAD.LNG_COD = '3'  " +
                $"LEFT OUTER JOIN [NOTES_DSC] NTS ON NTS.NTS_COD = D.NTS_COD AND NTS.LNG_COD = '3'  " +
                $"LEFT OUTER JOIN [CLICHE] CL ON Cl.CPLX_PRT_COD = D.PRT_COD " +
                $"where CAT_COD = '{CatCode}' and TABLE_COD = '{d.TableCode}' and VARIANTE = {d.Variante}  order by TBD_RIF, TBD_SEQ";
            d.Parts = new List<Part>();
            d.CompatibilityList = new List<string>();
            var cmd = new SqlCommand(sql, conn);
            var dr = cmd.ExecuteReader();
            while (dr.Read())
            {
                var p = new Part();
                p.Description = dr.GetString(2);
                p.Modification = new List<string>();
                if (!dr.IsDBNull(3))
                {
                    string mods = dr.GetString(3);
                    p.Modification.AddRange(mods.Split(new[] { ',' }));
                    foreach (var c in p.Modification)
                    {
                        var mod = c.Substring(1);
                        if (!d.ModificationList.Contains(mod))
                            d.ModificationList.Add(mod);
                    }
                }
                p.PartNo = dr.GetString(1);
                p.Qty = dr.GetString(4);
                p.RIF = dr.GetInt16(0);
                p.Compatibility = new List<string>();
                if (!dr.IsDBNull(5))
                {
                    string compatibility = dr.GetString(5);
                    p.Compatibility.AddRange(compatibility.Split(new[] { ',', '+', '(', ')', ' ', '!', '\n' }, System.StringSplitOptions.RemoveEmptyEntries));
                    foreach (var c in p.Compatibility)
                    {
                        if (!d.CompatibilityList.Contains(c))
                            d.CompatibilityList.Add(c);
                    }
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
            foreach (var item in d.Cliches)
            {

                // Now get that parts for the cliches;
                sql = $"select CPD_RIF, PRT_COD, TRIM(C.CDS_DSC + ' ' +ISNULL(DAD.DSC, '')), D.MODIF, D.CPD_QTY, '', ISNULL(NTS.NTS_DSC, ''), ISNULL(D.CLH_COD, '') " +
                    $"from CPXDATA D  JOIN CODES_DSC C ON C.CDS_COD = D.PRT_CDS_COD AND LNG_COD = '3' " +
                    $"LEFT OUTER JOIN DESC_AGG_DSC DAD ON DAD.COD = D.CPD_AGG_DSC AND DAD.LNG_COD = '3'  " +
                    $"LEFT OUTER JOIN [NOTES_DSC] NTS ON NTS.NTS_COD = D.NTS_COD AND NTS.LNG_COD = '3'  " +
                    $"where D.CPLX_PRT_COD = '{item.Key}' AND D.CLH_COD = '{item.Value.ClicheCode}'  order by CpD_RIF, CPD_RIF_SEQ";
                cmd = new SqlCommand(sql, conn);
                dr = cmd.ExecuteReader();
                item.Value.Parts = new List<Part>();
                while (dr.Read())
                {
                    var p = new Part();
                    p.Description = dr.GetString(2);
                    p.Modification = new List<string>();
                    if (!dr.IsDBNull(3))
                    {
                        string mods = dr.GetString(3);
                        p.Modification.AddRange(mods.Split(new[] { ',' }));
                        foreach (var c in p.Modification)
                        {
                            var mod = c.Substring(1);
                            if (!d.ModificationList.Contains(mod))
                                d.ModificationList.Add(mod);
                        }
                    }
                    p.PartNo = dr.GetString(1);
                    p.Qty = dr.GetString(4);
                    p.RIF = dr.GetInt16(0);
                    p.Compatibility = new List<string>();
                    if (!dr.IsDBNull(5))
                    {
                        string compatibility = dr.GetString(5);
                        p.Compatibility.AddRange(compatibility.Split(new[] { ',', '+', '(', ')', ' ', '!', '\n' }, System.StringSplitOptions.RemoveEmptyEntries));
                        foreach (var c in p.Compatibility)
                        {
                            if (!d.CompatibilityList.Contains(c))
                                d.CompatibilityList.Add(c);
                        }
                    }
                    p.Notes = dr.GetString(6);
                    item.Value.Parts.Add(p);

                }
                dr.Close();
            }



        }
        public void Close()
        {
            conn.Close();
        }

    }
}
