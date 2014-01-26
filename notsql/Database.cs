using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace notsql
{
    public class Database
    {
        private string _cs;
        public string cs
        {
            get
            {
                return (_cs);
            }
        }

        public Database(string connectionString)
        {
            _cs = connectionString;
        }

        public Table Table(string name)
        {
            return (new Table(this, name));
        }
    }
}
