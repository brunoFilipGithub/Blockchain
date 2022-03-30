using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;

namespace Blockchain.Models
{
    public class LandModel
    {
        public string landId { get; set; }
        public string landOwnerHash { get; set; }
        public string landRequesterHash { get; set; }
        public string location { get; set; }
        public string points { get; set; }
    }
}
