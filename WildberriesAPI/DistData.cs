using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WildberriesAPI
{
    internal class DistData
    {
        public double latitude { get; set; }
        public double longitude { get; set; }
        public string address { get; set; }
        public string xinfo { get; set; }
        public string userDataSign { get; set; }
        public List<int> destinations { get; set; }
        public string locale { get; set; }
        public int shard { get; set; }
        public string currency { get; set; }
        public string ip { get; set; }
        public int dt { get; set; }
    }

}
