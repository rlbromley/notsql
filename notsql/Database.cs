using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace notsql
{
    public class Database
    {
        public enum IndexModes
        {
            Inline,
            Async,
            External
        }

        private string _cs;
        public string cs { get { return (_cs); } }

        private IndexModes _im = IndexModes.Inline;
        public IndexModes im { get { return (_im); } }

        public Database(string connectionString)
        {
            _cs = connectionString;
        }

        public Database(string connectionString, IndexModes mode) : this(connectionString)
        {
            _im = mode;
        }

        public Table Table(string name)
        {
            return (new Table(this, name));
        }
    }
}
