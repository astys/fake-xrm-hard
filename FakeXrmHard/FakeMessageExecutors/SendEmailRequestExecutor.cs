using System;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;

namespace FakeXrmEasy.FakeMessageExecutors
{
    public class SendEmailRequestExecutor : IFakeMessageExecutor
    {
        public bool CanExecute(OrganizationRequest request)
        {
            return request is SendEmailRequest;
        }

        public OrganizationResponse Execute(OrganizationRequest request, XrmFakedContext ctx)
        {
            var req = request as SendEmailRequest;
            var entity = new Entity("email", req.EmailId);
            entity["statecode"] = new OptionSetValue(1); //Completed
            entity["statuscode"] = new OptionSetValue(3); //Sent
            ctx.GetOrganizationService().Update(entity);
            return new SendEmailResponse();
        }

        public Type GetResponsibleRequestType()
        {
            return typeof(SendEmailRequest);
        }
    }
}
