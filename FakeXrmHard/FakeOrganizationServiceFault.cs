using Microsoft.Xrm.Sdk;
using System.ServiceModel;

namespace FakeXrmEasy;

public class FakeOrganizationServiceFault
{
    public static void Throw(ErrorCodes errorCode, string message)
    {
        var faultReason = new FaultReason(message);
        throw new FaultException<OrganizationServiceFault>(new OrganizationServiceFault { ErrorCode = (int)errorCode, Message = message }, faultReason);
    }
}