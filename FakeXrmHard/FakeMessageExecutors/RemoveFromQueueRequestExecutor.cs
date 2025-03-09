using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using System.ServiceModel;

namespace FakeXrmEasy.FakeMessageExecutors;

public class RemoveFromQueueRequestExecutor : IFakeMessageExecutor
{
    public bool CanExecute(OrganizationRequest request)
    {
        return request is RemoveFromQueueRequest;
    }

    public OrganizationResponse Execute(OrganizationRequest request, XrmFakedContext ctx)
    {
        var removeFromQueueRequest = (RemoveFromQueueRequest)request;

        var queueItemId = removeFromQueueRequest.QueueItemId;
        if (queueItemId == Guid.Empty)
        {
            throw new FaultException<OrganizationServiceFault>(new OrganizationServiceFault(), new FaultReason("Cannot remove without queue item."));
        }

        var service = ctx.GetOrganizationService();
        service.Delete("queueitem", queueItemId);

        return new RemoveFromQueueResponse();
    }

    public Type GetResponsibleRequestType()
    {
        return typeof(RemoveFromQueueRequest);
    }
}