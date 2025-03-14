﻿using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;

namespace FakeXrmEasy.FakeMessageExecutors;

public class AddMemberListRequestExecutor : IFakeMessageExecutor
{
    public enum ListCreatedFromCode
    {
        Account = 1,
        Contact = 2,
        Lead = 4
    }

    public bool CanExecute(OrganizationRequest request)
    {
        return request is AddMemberListRequest;
    }

    public OrganizationResponse Execute(OrganizationRequest request, XrmFakedContext ctx)
    {
        var req = (AddMemberListRequest)request;

        if ( req.ListId == Guid.Empty)
        {
            FakeOrganizationServiceFault.Throw(ErrorCodes.InvalidArgument, "ListId parameter is required");
        }

        if ( req.EntityId == Guid.Empty)
        {
            FakeOrganizationServiceFault.Throw(ErrorCodes.InvalidArgument, "EntityId parameter is required");
        }

        var service = ctx.GetOrganizationService();

        //Find the list
        var list = ctx.CreateQuery("list")
            .Where(e => e.Id == req.ListId)
            .FirstOrDefault();

        if (list == null)
        {
            FakeOrganizationServiceFault.Throw(ErrorCodes.IsvAborted, string.Format("List with Id {0} wasn't found", req.ListId.ToString()));
        }

        //Find the member
        if (!list.Attributes.ContainsKey("createdfromcode"))
        {
            FakeOrganizationServiceFault.Throw(ErrorCodes.IsvUnExpected, string.Format("List with Id {0} must have a CreatedFromCode attribute defined and it has to be an option set value.", req.ListId.ToString()));
        }

        if (list["createdfromcode"] != null && !(list["createdfromcode"] is OptionSetValue))
        {
            FakeOrganizationServiceFault.Throw(ErrorCodes.IsvUnExpected, string.Format("List with Id {0} must have a CreatedFromCode attribute defined and it has to be an option set value.", req.ListId.ToString()));
        }

        var createdFromCodeValue = (list["createdfromcode"] as OptionSetValue).Value;
        string memberEntityName = "";
        switch (createdFromCodeValue)
        {
            case (int)ListCreatedFromCode.Account:
                memberEntityName = "account";
                break;

            case (int)ListCreatedFromCode.Contact:
                memberEntityName = "contact";
                break;

            case (int)ListCreatedFromCode.Lead:
                memberEntityName = "lead";
                break;

            default:
                FakeOrganizationServiceFault.Throw(ErrorCodes.IsvUnExpected, string.Format("List with Id {0} must have a supported CreatedFromCode value (Account, Contact or Lead).", req.ListId.ToString()));
                break;
        }

        var member = ctx.CreateQuery(memberEntityName)
            .Where(e => e.Id == req.EntityId)
            .FirstOrDefault();

        if (member == null)
        {
            FakeOrganizationServiceFault.Throw(ErrorCodes.IsvAborted, string.Format("Member with Id {0} wasn't found", req.EntityId.ToString()));
        }

        //create member list
        var listmember = new Entity("listmember");
        listmember["listid"] = new EntityReference("list", req.ListId);
        listmember["entityid"] = new EntityReference(memberEntityName, req.EntityId);
        service.Create(listmember);

        return new AddMemberListResponse();
    }

    public Type GetResponsibleRequestType()
    {
        return typeof(AddMemberListRequest);
    }
}