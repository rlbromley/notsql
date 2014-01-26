using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace notsql
{
    public class Tools
    {
        public static bool IsArray(string json)
        {
            return ((json.Length > 0) && (json[0] == '['));
        }
    }
}
