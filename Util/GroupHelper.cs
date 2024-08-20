using Microsoft.Extensions.Configuration;
using SJPCORE.Models;
using System;
using System.Collections.Generic;
using Dapper;
using System.Linq;

namespace SJPCORE.Util
{
    public static class GroupHelper
    {
        public static Dictionary<string, string> getGroup()
        {
            var dict = new Dictionary<string, string>();
            using (var con = new DapperContext().CreateConnection())
            {
               var dic =  con.GetList<GroupModel>().Select(s => new { s.key,s.name });
                
               foreach(var item in dic)
                {
                    dict.Add(item.key,item.name);
                }
            }
            
            return dict;
        }

        public static Dictionary<string, _GroupAssignModel> getGroupNode()
        {
            var dict = new Dictionary<string, _GroupAssignModel>();
            using (var con = new DapperContext().CreateConnection())
            {
               var dic =  con.GetList<GroupAssignModel>().Select(s => new { s.grp_id,s.station_id });
                foreach (var item in dic)
                {
                    var groupname = con.Get<GroupModel>(item.grp_id);
                    if (dict.ContainsKey(item.grp_id))
                    {
                        var arr = dict[item.grp_id].nodes;
                        var newArr = arr.ToList();
                        newArr.Add(item.station_id);
                        dict[item.grp_id].nodes = newArr.ToArray();
                    }
                    else
                    {
                        dict.Add(item.grp_id, new _GroupAssignModel { name = groupname.name, nodes = new string[] { item.station_id } });
                    }
                }
            }

            return dict;           
        }
    }
}
