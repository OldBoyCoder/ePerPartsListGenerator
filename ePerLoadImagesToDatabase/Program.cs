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
using System.Data;
using System.Data.SqlClient;
using System.IO;

namespace ePerLoadImagesToDatabase
{
    class Program
    {
        private static string _basePath = @"c:\temp\ePer\images\";
        static void Main()
        {
            var pngFiles = Directory.GetFiles(_basePath, "*.png", SearchOption.AllDirectories);
            var cb = new SqlConnectionStringBuilder
            {
                InitialCatalog = "ePer",
                DataSource = "localhost",
                IntegratedSecurity = true
            };
            var conn = new SqlConnection(cb.ConnectionString);
            conn.Open();
            foreach (var file in pngFiles)
            {
                if (!file.Contains(".th") && !file.Contains(".TH"))
                    DatabaseFilePut(conn, file);
            }
            conn.Close();
        }

        private static void DatabaseFilePut(SqlConnection conn,  string imgPath)
        {
            byte[] file;
            using (var stream = new FileStream(imgPath, FileMode.Open, FileAccess.Read))
            {
                using (var reader = new BinaryReader(stream))
                {
                    file = reader.ReadBytes((int)stream.Length);
                }
            }
            using (var sqlWrite = new SqlCommand("INSERT INTO IMG_DATA (IMG_PATH, IMG_BYTES) Values(@Path, @File)", conn))
            {
                sqlWrite.Parameters.Add("@Path", SqlDbType.NVarChar).Value = imgPath.Replace(_basePath, "");
                sqlWrite.Parameters.Add("@File", SqlDbType.VarBinary, file.Length).Value = file;
                sqlWrite.ExecuteNonQuery();
            }
        }
    }
}
