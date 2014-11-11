using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace SMSGatewayWCFLibrary
{
    [DataContract]
    class SMS
    {
        [DataMember]
        public string phone { get; set; }
        [DataMember]
        public string message { get; set; }
    }
}
