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

        public JObject write(string doc)
        {
            return(write(JObject.Parse(doc)));
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
                switch (_d.im)
                {
                    case Database.IndexModes.Inline:
                        {
                            indexer(doc);
                            break;
                        }
                    case Database.IndexModes.Async:
                        {
                            Task.Factory.StartNew(() => indexer(doc));
                            break;
                        }
                }
            }
            doc["_id"] = _id;
            doc["_rev"] = _rev;
            return (doc);
        }

        private void indexer(JObject doc)
        {
            var parsed = doc.ToTuple();
            string _id = doc["_id"].ToString();
            using (SqlConnection clocal = new SqlConnection(_d.cs))
            {
                clocal.Open();
                using (var tran = clocal.BeginTransaction())
                {
                    bool indexed = false;
                    try
                    {
                        parsed.Each(t =>
                        {
                            using (SqlCommand c = new SqlCommand("INSERT INTO [keys] ([tablename], [_docid], [key], [val]) VALUES (@t, @d, @k, @v)", clocal))
                            {
                                c.Parameters.Add(new SqlParameter("t", _name));
                                c.Parameters.Add(new SqlParameter("d", _id));
                                c.Parameters.Add(new SqlParameter("k", t.Item1));
                                c.Parameters.Add(new SqlParameter("v", t.Item2));
                                c.ExecuteNonQuery();
                            }
                        });
                        using (SqlCommand cf = new SqlCommand("UPDATE [docs] SET _dirty = 1 where [_id] = @d", clocal))
                        {
                            cf.Parameters.Add(new SqlParameter("d", _id));
                            cf.ExecuteNonQuery();
                        }
                        indexed = true;
                    }
                    catch
                    {
                    }
                    if (indexed) { tran.Commit(); } else { tran.Rollback(); throw new Exception("indexing failed"); }
                }
            }
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

        public JObject[] find(string doc)
        {
            return (find(JObject.Parse(doc)));
        }

        public JObject read(JObject doc)
        {
            return (read(doc["_id"].ToString()));
        }

        public JObject read(string id)
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
    }
}