using ePerPartsListGenerator.Model;
using System.Collections.Generic;
using System.Data.OleDb;


namespace ePerPartsListGenerator.Repository
{
    public class AccessRelease20Repository : IRepository
    {
        private OleDbConnection _conn;
        private readonly string _languageCode;

        public AccessRelease20Repository(string languageCode)
        {
            _languageCode = languageCode;
        }

        public List<Catalogue> GetAllCatalogues()
        {
            Open();

            var catalogues = new List<Catalogue>();
            var sql = "select CATALOGUES.CAT_COD, MAKES.MK_DSC, COMM_MODGRP.CMG_DSC, CATALOGUES.CAT_DSC from ((CATALOGUES INNER JOIN MAKES ON MAKES.MK_COD = CATALOGUES.MK_COD) INNER JOIN COMM_MODGRP ON COMM_MODGRP.MK2_COD = CATALOGUES.MK2_COD AND COMM_MODGRP.CMG_COD = CATALOGUES.CMG_COD) order by MAKES.MK_DSC, COMM_MODGRP.CMG_DSC, CATALOGUES.CAT_DSC";
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

            return catalogues;
        }

        public Catalogue GetCatalogue(string catCode)
        {
            Open();
            var cat = ReadCatalogue(catCode);
            //GetAllModificationLegendEntries(cat);
            //GetAllVariantLegendEntries(cat);
            GetAllGroupEntries(cat);
            foreach (var group in cat.Groups)
            {
                GetGroupTables(cat, group);
                foreach (var table in group.Tables)
                    GetTableDrawings(cat, group, table);
            }

            //Close();
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
                    //ImageStream = GetColumnAsStream(dr, 1),
                    g.Description = dr.GetString(1);
                    d.Add(g);
                }

                dr.Close();
            }

            cat.Groups = d;
        }

        private void GetGroupTables(Catalogue catalogue, Group group)
        {
            group.Tables = new List<Table>();
            var sql =
                $"SELECT DISTINCT S.SGRP_COD, SGRP_DSC FROM SGSEQS S, SUBGROUPS_DSC SD WHERE SD.GRP_COD = S.GRP_COD AND SD.SGRP_COD = S.SGRP_COD AND LNG_COD = '{_languageCode}' AND S.CAT_COD = '{catalogue.CatCode}'  AND S.GRP_COD = {group.Code}  order by S.SGRP_COD";
            var cmd = new OleDbCommand(sql, _conn);
            using (var dr = cmd.ExecuteReader())
            {
                while (dr.Read())
                {
                    var t = new Table();
                    t.Description = dr.GetString(1);
                    t.TableCode = dr.GetByte(0);
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

        private void Open()
        {
            var password = "\u0001\u0007\u0014\u0007\u0001\u00f3\u001b\n\n\u00d2\u001e\u00da\u00b1";
            var ePerPath = @"C:\ePer installs\Release 20";
            OleDbConnectionStringBuilder builder = new OleDbConnectionStringBuilder();
            builder.ConnectionString = $"Data Source={ePerPath}\\SP.DB.00900.FCTLR";
            builder.Add("Provider", "Microsoft.Jet.Oledb.4.0");
            builder.Add("Jet OLEDB:Database Password", password);
            _conn = new OleDbConnection(builder.ConnectionString);
            _conn.Open();

        }

        private Catalogue ReadCatalogue(string catCode)
        {
            var sql = $"select MK_COD, CAT_DSC from CATALOGUES C where cat_cod = '{catCode}'";
            var cmd = new OleDbCommand(sql, _conn);
            var dr = cmd.ExecuteReader();
            var cat = new Catalogue();
            if (dr.Read())
            {
                cat.Description = dr.GetString(0);
                //cat.ImageBytes = GetColumnAsStream(dr, 1);
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
                $"WHERE CAT_COD = @P1 AND GRP_COD = @P2 AND SGRP_COD = @P3 " +
                "ORDER BY DRAWINGS.DRW_NUM";
            var cmd = new OleDbCommand(sql, _conn);
            cmd.Parameters.AddWithValue("@P1", catalogue.CatCode);
            cmd.Parameters.AddWithValue("@P2", group.Code);
            cmd.Parameters.AddWithValue("@P3", table.TableCode);

            using (var dr = cmd.ExecuteReader())
            {
                while (dr.Read())
                {
                    var d = new Drawing();

                    d.DrawingNo = dr.GetByte(0);
                    //ImageStream = GetColumnAsStream(dr, 3),
                    //Revision = dr.GetInt16(2),
                    //Variant = dr.GetInt16(1),
                    d.TableCode = dr.GetByte(1).ToString();
                    d.SgsCode = dr.GetByte(2);
                    //if (!dr.IsDBNull(4))
                    //{
                    //    var mods = dr.GetString(4);
                    //    d.Modifications = mods;
                    //    var modItems = mods.Split(',');
                    //    foreach (var item in modItems)
                    //    {
                    //        var mod = item.Substring(1);
                    //        if (!d.ModificationList.Contains(mod))
                    //            d.ModificationList.Add(mod);
                    //    }
                    //}
                    //else
                    //{
                    //    d.Modifications = "";
                    //}

                    //d.ValidFor = dr.GetString(5);
                    //d.CompatibilityList.AddRange(d.ValidFor.Split(new[] { ',', '+', '(', ')', ' ', '!', '\n' },
                    //    System.StringSplitOptions.RemoveEmptyEntries));

                    table.Drawings.Add(d);
                }

                dr.Close();
            }

            //foreach (var d in table.Drawings) AddParts(catalogue.CatCode, group, table, d);
        }
    }
}
