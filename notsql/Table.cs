using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace notsql
{
    public class Table
    {
        private Database _d;
        private string _name;
        public string name
        {
            get
            {
                return (_name);
            }
        }

        public Table(Database d, string table)
        {
            _d = d;
            _name = table;
        }

        public string Delete(string json)
        {
            return (process(json, this.delete));
        }

        public string Insert(string json)
        {
            return (process(json, this.save));
        }

        public string Update(string json)
        {
            return (process(json, this.update));
        }

        public string Get(string json)
        {
            return (process(json, this.fetch));
        }

        private string queryBuilder(string key, Newtonsoft.Json.Linq.JToken token)
        {
            string result = "";
            switch (token.Type)
            {
                case Newtonsoft.Json.Linq.JTokenType.Array:
                    {
                        foreach(var val in token) {
                            result = string.Format("{0} AND {1}", result, queryBuilder(key, result));
                        }
                        break;
                    }
                case Newtonsoft.Json.Linq.JTokenType.String:
                    {
                        result = String.Format(" AND (([key] = '{0}') AND ([value] = '{1}'))", key, token.ToString());
                        break;
                    }
            }
            return (result);
        }

        public string Find(string json)
        {
            var query = Newtonsoft.Json.Linq.JObject.Parse(json);
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("SELECT * FROM [docs] WHERE [tablename] = '{0}' ", _name);
            foreach (var entry in query)
            {
                sb.Append(queryBuilder(entry.Key, entry.Value));
            }

            Newtonsoft.Json.Linq.JArray result = new Newtonsoft.Json.Linq.JArray();
            using (SqlConnection conn = new SqlConnection(_d.cs))
            {
                using (SqlCommand cmd = new SqlCommand(sb.ToString(), conn))
                {
                    using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                    {
                        DataTable dt = new DataTable();
                        conn.Open();
                        da.Fill(dt);

                        var docs = dt.AsEnumerable();
                        var ids = docs.Select(x => x["_id"].ToString()).Distinct();
                        foreach (var id in ids)
                        {
                            result.Add(this.fetch(id));
                        }
                    }
                }
            }
            
            return (result.ToString());
        }

        private Newtonsoft.Json.Linq.JObject fetch(Newtonsoft.Json.Linq.JObject doc)
        {
            return (this.fetch(doc["_id"].ToString()));
        }

        private Newtonsoft.Json.Linq.JObject fetch(string id)
        {
            Newtonsoft.Json.Linq.JObject o = new Newtonsoft.Json.Linq.JObject();
            var q = String.Format("SELECT * FROM [docs] WHERE [tablename] = '{0}' AND _id = '{1}' ORDER BY [key]", _name, id);
            using (SqlConnection conn = new SqlConnection(_d.cs))
            {
                using (SqlCommand cinner = new SqlCommand(q, conn))
                {
                    var dinner = new DataTable();
                    using (SqlDataAdapter dainner = new SqlDataAdapter(cinner))
                    {
                        dainner.Fill(dinner);
                        o.Add("_id", id);
                        foreach (var row in dinner.AsEnumerable())
                        {
                            switch (row["type"].ToString())
                            {
                                case "String":
                                    {
                                        o.Add(row["key"].ToString(), Newtonsoft.Json.Linq.JToken.FromObject(row["value"]));
                                        break;
                                    }
                                case "Integer":
                                    {
                                        o.Add(row["key"].ToString(), Newtonsoft.Json.Linq.JToken.FromObject(Convert.ToInt32(row["value"])));
                                        break;
                                    }
                                case "Float":
                                    {
                                        o.Add(row["key"].ToString(), Newtonsoft.Json.Linq.JToken.FromObject(Convert.ToDouble(row["value"])));
                                        break;
                                    }
                                default:
                                    {
                                        o.Add(row["key"].ToString(), Newtonsoft.Json.Linq.JToken.Parse(row["value"].ToString()));
                                        break;
                                    }
                            }
                        }
                    }
                }
            }
            return (o);
        }

        private Newtonsoft.Json.Linq.JObject update(Newtonsoft.Json.Linq.JObject doc)
        {
            string _id = doc["_id"].ToString();
            string sql = String.Format("DELETE FROM [docs] WHERE [_id] = '{0}'\r\n{1}", _id, BuildSaveSQL(doc, _id));
            using (SqlConnection conn = new SqlConnection(_d.cs))
            {
                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
            }
            return (doc);
        }

        private Newtonsoft.Json.Linq.JObject delete(Newtonsoft.Json.Linq.JObject doc)
        {
            string _id = doc["_id"].ToString();
            string sql = String.Format("DELETE FROM [docs] WHERE [_id] = '{0}'", _id);
            using (SqlConnection conn = new SqlConnection(_d.cs))
            {
                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
            }
            return (doc);
        }

        private Newtonsoft.Json.Linq.JObject save(Newtonsoft.Json.Linq.JObject doc)
        {
            Guid _id = Guid.NewGuid();
            using (SqlConnection conn = new SqlConnection(_d.cs))
            {
                using (SqlCommand cmd = new SqlCommand(BuildSaveSQL(doc, _id.ToString()), conn))
                {
                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
            }
            doc.Add("_id", Newtonsoft.Json.Linq.JToken.FromObject(_id));
            return (doc);
        }

        private string process(string json, Func<Newtonsoft.Json.Linq.JObject, Newtonsoft.Json.Linq.JObject> op)
        {
            if (Tools.IsArray(json))
            {
                var arr = Newtonsoft.Json.Linq.JArray.Parse(json);
                var result = new Newtonsoft.Json.Linq.JArray();
                foreach (var doc in arr)
                {
                    result.Add(op(new Newtonsoft.Json.Linq.JObject(doc)));
                }
                return (result.ToString());
            }
            else
            {
                var doc = Newtonsoft.Json.Linq.JObject.Parse(json);
                return (op(doc).ToString());
            }
        }

        private string BuildSaveSQL(Newtonsoft.Json.Linq.JObject doc, string id)
        {
            StringBuilder sb = new StringBuilder();
            foreach (var entry in doc)
            {
                if (entry.Key != "_id")
                {
                    sb.AppendFormat("INSERT INTO [docs] ([_id], [tablename], [key], [value], [type]) VALUES ('{0}', '{1}', '{2}', '{3}', '{4}')", id, _name, entry.Key, entry.Value, entry.Value.Type);
                }
            }
            return (sb.ToString());
        }
    }
}
