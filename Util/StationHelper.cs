using Microsoft.Extensions.Configuration;
using SJPCORE.Models;
using System;
using System.Collections.Generic;
using Dapper;
using System.Linq;

namespace SJPCORE.Util
{
    public static class StationHelper
    {
        public static Dictionary<string, string> getStation()
        {
            var dict = new Dictionary<string, string>();
            using (var con = new DapperContext().CreateConnection())
            {
               var dic =  con.GetList<StationModel>().Select(s => new { s.key,s.name });
                
               foreach(var item in dic)
                {
                    dict.Add(item.key,item.name);
                }
            }

            return dict;
        }


    }
}
