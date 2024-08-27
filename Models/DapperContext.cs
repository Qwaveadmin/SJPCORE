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

    public static class DatabaseInitializer
    {
        public static async Task InitializeDatabaseAsync(DapperContext context)
        {
            Console.WriteLine("Initializing database...");
            using (var connection = context.CreateConnection()) 
            {
                IEnumerable<(string key, string value, string grp, DateTime updateAt)> records = new List<(string key, string value, string grp, DateTime updateAt)>
                {
                    ("STATION_NAME", "สถานีกระจายเสียง : SJP Radio Broadcast", null, DateTime.MinValue),
                    ("FOOTER", "© NT. All rights reserved (V101.291022.001)", null, DateTime.MinValue),
                    ("CONFIG_MQTT_PORT", "1883", "MQTT", new DateTime(2023, 1, 20, 18, 20, 0)),
                    ("CONFIG_MQTT_USER", "QWAVE", "MQTT", new DateTime(2023, 1, 20, 18, 20, 0)),
                    ("CONFIG_MQTT_PASS", "QWAVE", "MQTT", new DateTime(2023, 1, 20, 18, 20, 0)),
                    ("BANNER_NOTICE", "หากมีข้องสังสัยหรือมีปัญหาโปรดติดต่อ 062-641-9124", null, DateTime.MinValue),
                    ("DARKMODE", "0", null, DateTime.MinValue),
                    ("SLOGAN", "ระบบเสียงตามสายเเละกระจายเสียง", null, new DateTime(2024, 6, 5, 15, 24, 0)),
                    ("EMQX_IP", "qwaveoffice.trueddns.com", "EMQX", new DateTime(2024, 5, 31, 13, 41, 0)),
                    ("EMQX_PORT", "12660", "EMQX", new DateTime(2024, 5, 31, 13, 54, 0)),
                    ("EMQX_USER", "QWAVE", "EMQX", new DateTime(2024, 6, 5, 15, 24, 0)),
                    ("EMQX_PASS", "QWAVE", "EMQX", new DateTime(2024, 6, 5, 15, 24, 0)),
                    ("DATABASE_HOST", "qwaveoffice.trueddns.com", "MySQL", new DateTime(2024, 6, 5, 14, 44, 0)),
                    ("DATABASE_PORT", "12661", "MySQL", new DateTime(2024, 6, 5, 14, 48, 0)),
                    ("DATABASE_USER", "qwave@it@admin", "MySQL", new DateTime(2024, 6, 5, 14, 48, 0)),
                    ("DATABASE_PASS", "Qwave@dmin020890751", "MySQL", new DateTime(2024, 6, 5, 14, 49, 0)),
                    ("DATABASE_NAME", "sjp_broadcastradio", "MySQL", new DateTime(2024, 6, 5, 14, 50, 0)),
                    ("SITE_ID", "9db3edbd-26d1-4d57-a72a-e39f02260eb5", null, new DateTime(2024, 6, 5, 15, 18, 0)),
                    ("HOST_URL", "https://demo.sjpradio.cloud/", null, new DateTime(2024, 6, 6, 11, 35, 0)),
                    ("SECRETKEY", "19c103a0b278a71e9fdeac2c50284c71", null, new DateTime(2024, 6, 7, 14, 49, 0))
                };

                string checkQuery = "SELECT COUNT(1) FROM sjp_setting WHERE [key] = @key";
                string insertQuery = @"
                    INSERT INTO sjp_setting ([key], [value], grp, update_at)
                    VALUES (@key, @value, @grp, @updateAt)";

                foreach (var record in records)
                {
                    var existingRecordCount = await connection.ExecuteScalarAsync<int>(checkQuery, new { key = record.key });
                    Console.WriteLine($"Record count for {record.key}: {existingRecordCount}");

                    if (existingRecordCount == 0)
                    {
                        await connection.ExecuteAsync(insertQuery, new { key = record.key, value = record.value, grp = record.grp, updateAt = record.updateAt });
                    }
                }
            }
        }
    }
}
