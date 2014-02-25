using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;

namespace notsql_restdemo.Controllers
{
    [RoutePrefix("api/db")]
    public class DbController : ApiController
    {
        [Route("{tablename}"), HttpPut, HttpPost]
        public async Task<object> write(string tablename)
        {
            var n = new notsql.Database("Data Source=172.16.10.1\\SQLEXPRESS;Database=notsql;user=sa;password=tvt7215;Connect Timeout=30", notsql.Database.IndexModes.External);
            var req = await Request.Content.ReadAsStringAsync();
            return (n.Table(tablename).write(req));
        }

        [Route("{tablename}/{id}"), HttpGet]
        public async Task<object> read(string tablename, string id)
        {
            var n = new notsql.Database("Data Source=172.16.10.1\\SQLEXPRESS;Database=notsql;user=sa;password=tvt7215;Connect Timeout=30", notsql.Database.IndexModes.External);
            var req = await Request.Content.ReadAsStringAsync();
            return (n.Table(tablename).read(id));
        }

        [Route("{tablename}/find"), HttpPost]
        public async Task<object> find(string tablename)
        {
            var n = new notsql.Database("Data Source=172.16.10.1\\SQLEXPRESS;Database=notsql;user=sa;password=tvt7215;Connect Timeout=30", notsql.Database.IndexModes.External);
            var req = await Request.Content.ReadAsStringAsync();
            return (n.Table(tablename).find(req));
        }
    }
}
