using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BanByHostname
{
    public class BannedHost
    {
        public string hostname { get; set; }
        public string reason { get; set; }
        public BannedHost(string Hostname, string Reason)
        {
            hostname = Hostname;
            reason = Reason;
        }
    }
}
