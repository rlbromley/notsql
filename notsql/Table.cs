using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace notsql
{
    public class queryToken
    {
        private int _index;

        public string keyParm
        {
            get
            {
                return (string.Format("@kp{0}", _index));
            }
        }

        public string valParm
        {
            get
            {
                return (string.Format("@vp{0}", _index));
            }
        }

        public string key { get; set; }
        public string val { get; set; }
        public string op { get; set; }

        public queryToken(int index)
        {
            _index = index;
        }
    }

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

        public JObject write(JObject doc)
        {
            Guid _id = Guid.NewGuid();
            Guid _rev = Guid.NewGuid();
            if (doc["_id"] != null)
            {
                _id = Guid.Parse(doc["_id"].ToString());
            }
            if (doc["_rev"] != null)
            {
                _rev = Guid.Parse(doc["_rev"].ToString());
            }
            using (SqlConnection conn = new SqlConnection(_d.cs))
            {
                using (SqlCommand cmd = new SqlCommand("sp_storedoc", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.Add(new SqlParameter("id", _id.ToString()));
                    cmd.Parameters.Add(new SqlParameter("rev", _rev.ToString()));
                    cmd.Parameters.Add(new SqlParameter("tablename", _name));
                    cmd.Parameters.Add(new SqlParameter("doc", doc.ToString()));
                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
                var parsed = doc.ToTuple();
                // write parsed out key/vals

                using (SqlConnection clocal = new SqlConnection(_d.cs))
                {
                    clocal.Open();
                    parsed.Each(t =>
                    {
                        using (SqlCommand c = new SqlCommand("INSERT INTO [keys] ([tablename], [_docid], [key], [val]) VALUES (@t, @d, @k, @v)", clocal))
                        {
                            c.Parameters.Add(new SqlParameter("t", _name));
                            c.Parameters.Add(new SqlParameter("d", _id.ToString()));
                            c.Parameters.Add(new SqlParameter("k", t.Item1));
                            c.Parameters.Add(new SqlParameter("v", t.Item2));
                            c.ExecuteNonQuery();
                        }
                    });
                }
            }
            doc["_id"] = _id;
            doc["_rev"] = _rev;
            return (doc);
        }

        public JObject read(JObject doc)
        {
            return (read(doc["_id"].ToString()));
        }

        public JObject[] find(JObject query)
        {
            var parms = query.ToTuple();
            var result = new List<JObject>();
            List<queryToken> tokens = new List<queryToken>();
            int c = 0;
            parms.Each(x =>
            {
                if (x.Item1.Contains(".$"))
                {
                    string[] pair = x.Item1.Split(new string[1] { ".$" }, StringSplitOptions.None);
                    var q = new queryToken(c++);
                    q.key = pair[0];
                    q.val = x.Item2;
                    switch (pair[1])
                    {
                        case "eq": { q.op = "="; break; }
                        case "lt": { q.op = "<"; break; }
                        case "gt": { q.op = ">"; break; }
                        case "lte": { q.op = "=<"; break; }
                        case "gte": { q.op = ">="; break; }
                    }
                    tokens.Add(q);
                }
            });
            if (tokens.Count > 0)
            {
                var sb = new StringBuilder(String.Format("SELECT [_docid] FROM [keys] WHERE [tablename] = '{0}' ", _name));
                tokens.Each(x =>
                {
                    sb.Append(String.Format(" AND (([key] = {0}) AND ([val] {1} {2}))", x.keyParm, x.op, x.valParm));
                });
                using (SqlConnection conn = new SqlConnection(_d.cs))
                {
                    using (SqlCommand cmd = new SqlCommand(sb.ToString(), conn))
                    {
                        tokens.Each(x =>
                        {
                            cmd.Parameters.Add(new SqlParameter(x.keyParm, x.key));
                            cmd.Parameters.Add(new SqlParameter(x.valParm, x.val));
                        });
                        conn.Open();
                        List<string> ids = new List<string>();
                        using (SqlDataReader rdr = cmd.ExecuteReader())
                        {
                            while (rdr.Read())
                            {
                                ids.Add(rdr["_docid"].ToString());
                            }
                        }
                        ids.Each(x => result.Add(read(x, conn)));
                    }
                }
            }
            return (result.ToArray());
        }

        private JObject read(string id)
        {
            JObject result = null;
            using (SqlConnection conn = new SqlConnection(_d.cs))
            {
                conn.Open();
                result = read(id, conn);
            }
            if (result == null) result = new JObject();
            return (result);
        }

        private JObject read(string id, SqlConnection conn)
        {
            JObject result = null;

            using (SqlCommand cmd = new SqlCommand("sp_retdoc", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add(new SqlParameter("id", id));
                cmd.Parameters.Add(new SqlParameter("tablename", _name));
                using (SqlDataReader rdr = cmd.ExecuteReader(CommandBehavior.SingleRow))
                {
                    if (rdr.Read())
                    {
                        result = JObject.Parse(rdr["doc"].ToString());
                        result["_id"] = rdr["_id"].ToString();
                        result["_rev"] = rdr["_rev"].ToString();
                    }
                }
            }

            return (result);
        }

        /*
        private string queryBuilder(string key, JToken token)
        {
            string result = "";
            switch (token.Type)
            {
                case JTokenType.Array:
                    {
                        foreach(var val in token) {
                            result = string.Format("{0} AND {1}", result, queryBuilder(key, result));
                        }
                        break;
                    }
                default:
                    {
                        result = String.Format(" AND (([key] = '{0}') AND ([value] = '{1}'))", key, token.ToString());
                        break;
                    }
            }
            return (result);
        }

        public string Find(string json)
        {
            var query = JObject.Parse(json);
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("SELECT * FROM [docs] WHERE [tablename] = '{0}' ", _name);
            foreach (var entry in query)
            {
                sb.Append(queryBuilder(entry.Key, entry.Value));
            }

            JArray result = new JArray();
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

        public JObject fetch(JObject doc)
        {
            JObject o = new JObject();
            var q = String.Format("SELECT * FROM [docs] WHERE [tablename] = '{0}' AND _id = '{1}' ORDER BY [key]", _name, doc["_id"]);
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
                                        o.Add(row["key"].ToString(), JToken.FromObject(row["value"]));
                                        break;
                                    }
                                case "Integer":
                                    {
                                        o.Add(row["key"].ToString(), JToken.FromObject(Convert.ToInt32(row["value"])));
                                        break;
                                    }
                                case "Float":
                                    {
                                        o.Add(row["key"].ToString(), JToken.FromObject(Convert.ToDouble(row["value"])));
                                        break;
                                    }
                                default:
                                    {
                                        o.Add(row["key"].ToString(), JToken.Parse(row["value"].ToString()));
                                        break;
                                    }
                            }
                        }
                    }
                }
            }
            return (o);
        }

        public JObject update(JObject doc)
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

        public JObject delete(JObject doc)
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

        public JObject save(JObject doc)
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
            doc.Add("_id", JToken.FromObject(_id));
            return (doc);
        }

        private string process(string json, Func<JObject, JObject> op)
        {
            if (Tools.IsArray(json))
            {
                var arr = JArray.Parse(json);
                var result = new JArray();
                foreach (var doc in arr)
                {
                    result.Add(op(new JObject(doc)));
                }
                return (result.ToString());
            }
            else
            {
                var doc = JObject.Parse(json);
                return (op(doc).ToString());
            }
        }

        private List<Tuple<string, string>> parsedoc(JObject doc)
        {
            var inserts = new List<Tuple<string, string>>();
            foreach (var p in doc)
            {
                switch (p.Value.Type)
                {
                    case JTokenType.Array:
                        {
                            foreach (var x in (p.Value as JArray))
                            {
                                inserts.Add(new Tuple<string, string>(p.Key, p.Value.ToString()));
                            }
                            break;
                        }
                    case JTokenType.Object:
                        {
                            inserts.AddRange(parsedoc(p.Value as JObject));
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
         */
    }
}