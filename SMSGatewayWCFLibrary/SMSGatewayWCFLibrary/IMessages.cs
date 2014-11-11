using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;

namespace SMSGatewayWCFLibrary
{
    [ServiceContract]
    public interface IMessages
    {
        [OperationContract]
        bool SendSMS(string phone, string message);
    }
}
