using Dapper;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SJPCORE.Models.Attribute;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;

namespace SJPCORE.Models
{
    public class DapperContext
    {

        public IDbConnection CreateConnection()
        {
            return new SqliteConnection("Data Source=database.db");
        }

        public List<ConfigModel> GetConfig()
        {
            using (var con = CreateConnection())
            {
                return con.GetList<ConfigModel>().ToList();
            }
        }
      
    }
}
