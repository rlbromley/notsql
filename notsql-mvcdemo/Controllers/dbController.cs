using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace notsql_mvcdemo.Controllers
{
    public class dbController : ApiController
    {
        [HttpGet, ActionName("index")] // fetch a record
        public string read(string tablename, string id)
        {
            var db = new notsql.Database("Data Source=172.16.10.1\\SQLEXPRESS;Database=notsql;user=sa;password=tvt7215;Connect Timeout=30");
            var result = db.Table(tablename).read(id);
            return (result.ToString());
        }

        [HttpPut, ActionName("index")]
        public string create(string tablename)
        {
            HttpContent requestContent = Request.Content;
            string jsonContent = requestContent.ReadAsStringAsync().Result;
            return (write(tablename, jsonContent));
        }

        [HttpPost, ActionName("index")]
        public string update(string tablename)
        {
            HttpContent requestContent = Request.Content;
            string jsonContent = requestContent.ReadAsStringAsync().Result;
            return (write(tablename, jsonContent));
        }

        [HttpDelete, ActionName("index")]
        public string delete(string tablename, string id)
        {
            // guess what method still needs writing
            return ("");
        }

        private string write(string tablename, string content)
        {
            var db = new notsql.Database("Data Source=172.16.10.1\\SQLEXPRESS;Database=notsql;user=sa;password=tvt7215;Connect Timeout=30");
            var result = db.Table(tablename).write(content);
            return (result.ToString());
        }
    }
}
