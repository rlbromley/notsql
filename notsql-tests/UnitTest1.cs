﻿using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Data.SqlClient;

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

        [TestInitialize]
        public void init()
        {
            using (SqlConnection conn = new SqlConnection(this.ConnectionString))
            {
                using (SqlCommand cmd = new SqlCommand("DELETE FROM docs", conn))
                {
                    conn.Open();
                    cmd.ExecuteNonQuery();
                    conn.Close();
                }
            }
        }

        [TestMethod]
        public void DataTypes()
        {
            string tp1 = System.IO.File.ReadAllText("json/datatypes.json");
            var db = new notsql.Database(this.ConnectionString);
            string result = db.Table("test").Insert(tp1);
            var doc = Newtonsoft.Json.Linq.JObject.Parse(result);
            Guid id = Guid.Parse(doc["_id"].ToString());
            var oi = db.Table("test").Find("{ x : 16}");
        }
    }
}