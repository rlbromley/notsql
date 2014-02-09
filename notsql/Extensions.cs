using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace notsql
{
    public static class ExtensionMethods
    {
        public static void Each<T>(this IEnumerable<T> col, Action<T> a)
        {
            foreach (var x in col)
            {
                a(x);
            }
        }

        public static List<Tuple<string, string>> ToTuple(this JObject doc)
        {
            var inserts = new List<Tuple<string, string>>();
            foreach (var p in doc)
            {
                switch (p.Value.Type)
                {
                    case Newtonsoft.Json.Linq.JTokenType.Array:
                        {
                            int c = 0;
                            (p.Value as JArray).Each(x =>
                            {
                                var jo = new Newtonsoft.Json.Linq.JObject();
                                jo[String.Format("{0}[{1}]", p.Key, c++)] = x;
                                inserts.AddRange(jo.ToTuple());
                            });
                            break;
                        }
                    case Newtonsoft.Json.Linq.JTokenType.Object:
                        {
                            (p.Value as Newtonsoft.Json.Linq.JObject).ToTuple().ForEach(x =>
                            {
                                inserts.Add(new Tuple<string, string>(String.Format("{0}.{1}", p.Key, x.Item1), x.Item2));
                            });
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
