using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TDMWeb.Models.VOs
{
    public class ChaveVO
    {
        public string value { get; set; }
        public int key { get; set; }

        public ChaveVO(string value, int key)
        {
            this.key = key;
            this.value = value;
        }
    }
}