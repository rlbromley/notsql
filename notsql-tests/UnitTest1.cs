﻿using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Data.SqlClient;
using System.Collections.Generic;
using notsql;
using Newtonsoft.Json.Linq;
using System.Text;

namespace notsql_tests
{
    [TestClass]
    public class UnitTest1
    {
        private string ConnectionString
        {
            get
            {
                // return (string.Format("Data Source=(LocalDB)\\v11.0;AttachDbFilename=\"{0}\\TestDatabase.mdf\";Integrated Security=True;Connect Timeout=30", Environment.CurrentDirectory));
                return ("Data Source=.\\SQL2012EXPRESS;Database=notsql;Integrated Security=True;Connect Timeout=30");
            }
        }

        [TestInitialize]
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
                        cmd.CommandText = "DELETE FROM keys";
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

        [TestCleanup]
        public void cleanup()
        {
            // init();
        }

        [TestMethod]
        public void write()
        {
            var tp1 = new JObject();
            tp1["a"] = Guid.NewGuid();
            tp1["b"] = Guid.NewGuid();
            tp1["c"] = Guid.NewGuid();
            var db = new notsql.Database(this.ConnectionString);
            var result = db.Table("test").write(tp1);
            var id = Guid.Parse(result["_id"].ToString());
            var revid = Guid.Parse(result["_rev"].ToString());
        }

        [TestMethod]
        public void demo()
        {
            var tp1 = new JObject();
            tp1["Source"] = "ASA Charity";
            tp1["Charity"] = "Charity A";
            tp1["Donation"] = 350;
            tp1["Memoriam"] = "";
            tp1["DonatedAt"] = DateTime.Now.ToString();

            var tp2 = new JObject();
            tp2["Source"] = "ASA Charity";
            tp2["Charity"] = "Charity B";
            tp2["Donation"] = 5.50;
            tp2["Memoriam"] = "";
            tp2["DonatedAt"] = DateTime.Now.AddMinutes(9342).ToString(); 
            
            var db = new notsql.Database(this.ConnectionString);
            var result = db.Table("test").write(tp1);
            db.Table("test").write(tp2);
            var id = Guid.Parse(result["_id"].ToString());
            var revid = Guid.Parse(result["_rev"].ToString());
        }

        [TestMethod]
        public void _rev_changes()
        {
            var tp1 = new JObject();
            tp1["a"] = Guid.NewGuid();
            tp1["b"] = Guid.NewGuid();
            tp1["c"] = Guid.NewGuid();
            var db = new notsql.Database(this.ConnectionString);
            var result = db.Table("test").write(tp1);
            var id = Guid.Parse(result["_id"].ToString());
            var revid = Guid.Parse(result["_rev"].ToString());
            var q = new JObject();
            q["_id"] = id;
            var repeat = db.Table("test").read(q);
            var r2 = db.Table("test").read(q);
            var r3 = db.Table("test").read(q);
            Assert.AreNotEqual(result["_rev"].ToString(), r3["_rev"].ToString());
        }

        [TestMethod]
        public void find()
        {
            var tp1 = new JObject();
            tp1["a"] = Guid.NewGuid();
            tp1["b"] = Guid.NewGuid();
            tp1["c"] = Guid.NewGuid();
            var db = new notsql.Database(this.ConnectionString);
            var result = db.Table("test").write(tp1);
            var id = Guid.Parse(result["_id"].ToString());
            var revid = Guid.Parse(result["_rev"].ToString());
            for (int x = 0; x < 10; x++)
            {
                write();
            }
            var q = new JObject();
            var t = new JObject();
            t["$eq"] = tp1["b"];
            q["b"] = t;
            var r = db.Table("test").find(q);
        }

        [TestMethod]
        public void speedTests()
        {
            List<JObject> docs = new List<JObject>();
            for (int x = 0; x < 1000; x++)
            {
                var tp1 = new JObject();
                tp1["a"] = Guid.NewGuid();
                tp1["b"] = Guid.NewGuid();
                tp1["c"] = Guid.NewGuid();
                docs.Add(tp1);
            }
            var db = new notsql.Database(this.ConnectionString);
            docs.Each(x => db.Table("test").write(x));
        }
    }
}
