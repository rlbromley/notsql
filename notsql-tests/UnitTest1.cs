using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Data.SqlClient;
using System.Collections.Generic;

namespace notsql_tests
{
    [TestClass]
    public class UnitTest1
    {
        private string ConnectionString
        {
            get
            {
                return (string.Format("Data Source=(LocalDB)\\v11.0;AttachDbFilename=\"{0}\\TestDatabase.mdf\";Integrated Security=True;Connect Timeout=30", Environment.CurrentDirectory));
            }
        }

        // [TestInitialize]
        public void init()
        {
            using (SqlConnection conn = new SqlConnection(this.ConnectionString))
            {
                try
                {
                    using (SqlCommand cmd = new SqlCommand("DELETE FROM docs", conn))
                    {
                        conn.Open();
                        cmd.ExecuteNonQuery();
                    }
                }
                catch (Exception ex)
                {
                    conn.Close();
                    throw (ex);
                }
            }
        }

        [TestMethod]
        public void BasicInsert()
        {
            string tp1 = System.IO.File.ReadAllText("json/object-simple.json");
            var db = new notsql.Database(this.ConnectionString);
            string result = db.Table("test").Insert(tp1);
            var doc = Newtonsoft.Json.Linq.JObject.Parse(result);
            Guid id = Guid.Parse(doc["_id"].ToString());
            result = db.Table("test").Find("{ x : 16 }");
            var find = Newtonsoft.Json.Linq.JArray.Parse(result);
            var res = find[0];
            Assert.AreEqual(id.ToString(), res["_id"].ToString());
        }
        
        [TestMethod]
        public void DataTypes()
        {
            string tp1 = System.IO.File.ReadAllText("json/datatypes.json");
            var db = new notsql.Database(this.ConnectionString);
            string result = db.Table("test").Insert(tp1);
            var doc = Newtonsoft.Json.Linq.JObject.Parse(result);
            Guid id = Guid.Parse(doc["_id"].ToString());
            var oi = db.Table("test").Find("{ x : 16 }");
        }

        [TestMethod]
        public void adsf()
        {
            string tp1 = System.IO.File.ReadAllText("json/datatypes.json");
            var doc = Newtonsoft.Json.Linq.JObject.Parse(tp1);
            var r = parsedoc(doc);
        }

        private List<Tuple<string, string>> parsedoc(Newtonsoft.Json.Linq.JObject doc)
        {
            var inserts = new List<Tuple<string, string>>();
            foreach (var p in doc)
            {
                switch (p.Value.Type)
                {
                    case Newtonsoft.Json.Linq.JTokenType.Array:
                        {
                            foreach (var x in (p.Value as Newtonsoft.Json.Linq.JArray))
                            {
                                inserts.Add(new Tuple<string, string>(p.Key, p.Value.ToString()));
                            }
                            break;
                        }
                    case Newtonsoft.Json.Linq.JTokenType.Object:
                        {
                            inserts.AddRange(parsedoc(p.Value as Newtonsoft.Json.Linq.JObject));
                            break;
                        }
                    default:
                        {
                            inserts.Add(new Tuple<string, string>(p.Key, p.Value.ToString()));
                            break;
                        }
                }
            }
            return (inserts);
        }
    }
}
