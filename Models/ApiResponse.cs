using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SJPCORE.Models
{
    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public int StatusCode { get; set; }
        public T Data { get; set; }
        public string Message { get; set; }
        public string Error { get; set; }
    }

}