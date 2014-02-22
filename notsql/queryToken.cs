using System;
using System.Collections.Generic;
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
}
