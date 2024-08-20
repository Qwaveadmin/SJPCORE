using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Data;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using Dapper;
using Microsoft.AspNetCore.SignalR;

namespace SJPCORE.Models
{
    public class AppDbContext
    {
        public IDbConnection CreateConnection()
        {
            string host_name = GlobalParameter.Config.Where(w => w.key == "DATABASE_HOST").FirstOrDefault().value;
            string port = GlobalParameter.Config.Where(w => w.key == "DATABASE_PORT").FirstOrDefault().value;
            string database = GlobalParameter.Config.Where(w => w.key == "DATABASE_NAME").FirstOrDefault().value;
            string username = GlobalParameter.Config.Where(w => w.key == "DATABASE_USER").FirstOrDefault().value;
            string password = GlobalParameter.Config.Where(w => w.key == "DATABASE_PASS").FirstOrDefault().value;

            return new MySqlConnection($"Server={host_name};Port={port};Database={database};Uid={username};Pwd={password};");       
        }

        
    }
}