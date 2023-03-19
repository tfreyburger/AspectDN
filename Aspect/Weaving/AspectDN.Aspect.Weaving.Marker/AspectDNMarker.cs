using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AspectDN.Aspect.Weaving.Marker
{
    public class AspectDNMarkerAttribute : Attribute
    {
        public string AdviceName { get; set; }
        public string AspectRepositoryName { get; set; }
        public string Update { get; set; }
    }
}
