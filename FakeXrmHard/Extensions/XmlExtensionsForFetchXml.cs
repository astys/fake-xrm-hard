﻿using Microsoft.Xrm.Sdk.Query;
using System.Xml.Linq;
using Microsoft.Xrm.Sdk;
using System.Globalization;

namespace FakeXrmEasy.Extensions.FetchXml;

public static class XmlExtensionsForFetchXml
{
    private static IEnumerable<ConditionOperator> OperatorsNotToConvertArray = new[]
    {
        ConditionOperator.OlderThanXWeeks,
        ConditionOperator.OlderThanXYears,
        ConditionOperator.OlderThanXDays,
        ConditionOperator.OlderThanXHours,
        ConditionOperator.OlderThanXMinutes,
        ConditionOperator.OlderThanXMonths,
        ConditionOperator.LastXDays,
        ConditionOperator.LastXHours,
        ConditionOperator.LastXMonths,
        ConditionOperator.LastXWeeks,
        ConditionOperator.LastXYears,
        ConditionOperator.NextXHours,
        ConditionOperator.NextXDays,
        ConditionOperator.NextXWeeks,
        ConditionOperator.NextXMonths,
        ConditionOperator.NextXYears,
        ConditionOperator.NextXWeeks,
        ConditionOperator.InFiscalYear
    };

    public static bool IsAttributeTrue(this XElement elem, string attributeName)
    {
        var val = elem.GetAttribute(attributeName)?.Value;

        return "true".Equals(val, StringComparison.InvariantCultureIgnoreCase)
               || "1".Equals(val, StringComparison.InvariantCultureIgnoreCase);
    }

    public static bool IsAggregateFetchXml(this XDocument doc)
    {
        return doc.Root.IsAttributeTrue("aggregate");
    }

    public static bool IsDistincFetchXml(this XDocument doc)
    {
        return doc.Root.IsAttributeTrue("distinct");
    }

    public static bool IsFetchXmlNodeValid(this XElement elem)
    {
        switch (elem.Name.LocalName)
        {
            case "filter":
                return true;

            case "value":
            case "fetch":
                return true;

            case "entity":
                return elem.GetAttribute("name") != null;

            case "all-attributes":
                return true;

            case "attribute":
                return elem.GetAttribute("name") != null;

            case "link-entity":
                return elem.GetAttribute("name") != null
                       && elem.GetAttribute("from") != null
                       && elem.GetAttribute("to") != null;

            case "order":
                if (elem.Document.IsAggregateFetchXml())
                {
                    return elem.GetAttribute("alias") != null
                           && elem.GetAttribute("attribute") == null;
                }
                else
                {
                    return elem.GetAttribute("attribute") != null;
                }

            case "condition":
                return elem.GetAttribute("attribute") != null
                       && elem.GetAttribute("operator") != null;

            default:
                throw new Exception(string.Format("Node {0} is not a valid FetchXml node or it doesn't have the required attributes", elem.Name.LocalName));
        }
    }

    public static XAttribute GetAttribute(this XElement elem, string sAttributeName)
    {
        return elem.Attributes().FirstOrDefault((a => a.Name.LocalName.Equals(sAttributeName)));
    }

    public static ColumnSet ToColumnSet(this XElement el)
    {
        var allAttributes = el.Elements()
            .Where(e => e.Name.LocalName.Equals("all-attributes"))
            .FirstOrDefault();

        if (allAttributes != null)
        {
            return new ColumnSet(true);
        }

        var attributes = el.Elements()
            .Where(e => e.Name.LocalName.Equals("attribute"))
            .Select(e => e.GetAttribute("name").Value)
            .ToArray();


        return new ColumnSet(attributes);
    }

    public static int? ToTopCount(this XElement el)
    {
        var countAttr = el.GetAttribute("top");
        if (countAttr == null) return null;

        int iCount;
        if (!int.TryParse(countAttr.Value, out iCount))
            throw new Exception("Top attribute in fetch node must be an integer");

        return iCount;
    }

    public static int? ToCount(this XElement el)
    {
        var countAttr = el.GetAttribute("count");
        if (countAttr == null) return null;

        int iCount;
        if (!int.TryParse(countAttr.Value, out iCount))
            throw new Exception("Count attribute in fetch node must be an integer");

        return iCount;
    }


    public static bool ToReturnTotalRecordCount(this XElement el)
    {
        var returnTotalRecordCountAttr = el.GetAttribute("returntotalrecordcount");
        if (returnTotalRecordCountAttr == null) return false;

        bool bReturnCount;
        if (!bool.TryParse(returnTotalRecordCountAttr.Value, out bReturnCount))
            throw new Exception("returntotalrecordcount attribute in fetch node must be an boolean");

        return bReturnCount;
    }

    public static int? ToPageNumber(this XElement el)
    {
        var pageAttr = el.GetAttribute("page");
        if (pageAttr == null) return null;

        int iPage;
        if (!int.TryParse(pageAttr.Value, out iPage))
            throw new Exception("Count attribute in fetch node must be an integer");

        return iPage;
    }

    public static ColumnSet ToColumnSet(this XDocument xlDoc)
    {
        //Check if all-attributes exist
        return xlDoc.Elements()   //fetch
            .Elements()
            .FirstOrDefault()
            .ToColumnSet();
    }


    public static int? ToTopCount(this XDocument xlDoc)
    {
        //Check if all-attributes exist
        return xlDoc.Elements()   //fetch
            .FirstOrDefault()
            .ToTopCount();
    }

    public static int? ToCount(this XDocument xlDoc)
    {
        //Check if all-attributes exist
        return xlDoc.Elements()   //fetch
            .FirstOrDefault()
            .ToCount();
    }

    public static bool ToReturnTotalRecordCount(this XDocument xlDoc)
    {
        return xlDoc.Elements()   //fetch
            .FirstOrDefault()
            .ToReturnTotalRecordCount();
    }


    public static int? ToPageNumber(this XDocument xlDoc)
    {
        //Check if all-attributes exist
        return xlDoc.Elements()   //fetch
            .FirstOrDefault()
            .ToPageNumber();
    }

    public static FilterExpression ToCriteria(this XDocument xlDoc, XrmFakedContext ctx)
    {
        return xlDoc.Elements()   //fetch
            .Elements()     //entity
            .Elements()     //child nodes of entity
            .Where(el => el.Name.LocalName.Equals("filter"))
            .Select(el => el.ToFilterExpression(ctx))
            .FirstOrDefault();
    }

    public static string GetAssociatedEntityNameForConditionExpression(this XElement el)
    {

        while (el != null)
        {
            var parent = el.Parent;
            if (parent.Name.LocalName.Equals("entity") || parent.Name.LocalName.Equals("link-entity"))
            {
                return parent.GetAttribute("name").Value;
            }
            el = parent;
        }

        return null;
    }

    public static LinkEntity ToLinkEntity(this XElement el, XrmFakedContext ctx)
    {
        //Create this node
        var linkEntity = new LinkEntity();

        linkEntity.LinkFromEntityName = el.Parent.GetAttribute("name").Value;
        linkEntity.LinkFromAttributeName = el.GetAttribute("to").Value;
        linkEntity.LinkToAttributeName = el.GetAttribute("from").Value;
        linkEntity.LinkToEntityName = el.GetAttribute("name").Value;

        if (el.GetAttribute("alias") != null)
        {
            linkEntity.EntityAlias = el.GetAttribute("alias").Value;
        }

        //Join operator
        if (el.GetAttribute("link-type") != null)
        {
            switch (el.GetAttribute("link-type").Value)
            {
                case "outer":
                    linkEntity.JoinOperator = JoinOperator.LeftOuter;
                    break;
                default:
                    linkEntity.JoinOperator = JoinOperator.Inner;
                    break;
            }
        }

        //Process other link entities recursively
        var convertedLinkEntityNodes = el.Elements()
            .Where(e => e.Name.LocalName.Equals("link-entity"))
            .Select(e => e.ToLinkEntity(ctx))
            .ToList();

        foreach (var le in convertedLinkEntityNodes)
        {
            linkEntity.LinkEntities.Add(le);
        }

        //Process column sets
        linkEntity.Columns = el.ToColumnSet();

        //Process filter
        linkEntity.LinkCriteria = el.Elements()
            .Where(e => e.Name.LocalName.Equals("filter"))
            .Select(e => e.ToFilterExpression(ctx))
            .FirstOrDefault();

        return linkEntity;
    }

    public static List<LinkEntity> ToLinkEntities(this XDocument xlDoc, XrmFakedContext ctx)
    {
        return xlDoc.Elements()   //fetch
            .Elements()     //entity
            .Elements()     //child nodes of entity
            .Where(el => el.Name.LocalName.Equals("link-entity"))
            .Select(el => el.ToLinkEntity(ctx))
            .ToList();
    }

    public static List<OrderExpression> ToOrderExpressionList(this XDocument xlDoc)
    {
        var orderByElements = xlDoc.Elements()   //fetch
            .Elements()     //entity
            .Elements()     //child nodes of entity
            .Where(el => el.Name.LocalName.Equals("order"))
            .Select(el =>
                new OrderExpression
                {
                    AttributeName = el.GetAttribute("attribute").Value,
                    OrderType = el.IsAttributeTrue("descending") ? OrderType.Descending : OrderType.Ascending
                })
            .ToList();

        return orderByElements;
    }

    public static FilterExpression ToFilterExpression(this XElement elem, XrmFakedContext ctx)
    {
        var filterExpression = new FilterExpression();

        var filterType = elem.GetAttribute("type");
        if (filterType == null)
        {
            filterExpression.FilterOperator = LogicalOperator.And; //By default
        }
        else
        {
            filterExpression.FilterOperator = filterType.Value.Equals("and") ?
                LogicalOperator.And : LogicalOperator.Or;
        }

        //Process other filters recursively
        var otherFilters = elem
            .Elements() //child nodes of this filter
            .Where(el => el.Name.LocalName.Equals("filter"))
            .Select(el => el.ToFilterExpression(ctx))
            .ToList();


        //Process conditions
        var conditions = elem
            .Elements() //child nodes of this filter
            .Where(el => el.Name.LocalName.Equals("condition"))
            .Select(el => el.ToConditionExpression(ctx))
            .ToList();

        foreach (var c in conditions)
            filterExpression.AddCondition(c);

        foreach (var f in otherFilters)
            filterExpression.AddFilter(f);

        return filterExpression;
    }

    public static object ToValue(this XElement elem, XrmFakedContext ctx, string sEntityName, string sAttributeName, ConditionOperator op)
    {
        return GetConditionExpressionValueCast(elem.Value, ctx, sEntityName, sAttributeName, op);
    }

    public static ConditionExpression ToConditionExpression(this XElement elem, XrmFakedContext ctx)
    {
        var conditionExpression = new ConditionExpression();

        var conditionEntityName = "";

        var attributeName = elem.GetAttribute("attribute").Value;
        ConditionOperator op = ConditionOperator.Equal;

        string value = null;
        if (elem.GetAttribute("value") != null)
        {
            value = elem.GetAttribute("value").Value;
        }
        if (elem.GetAttribute("entityname") != null)
        {
            conditionEntityName = elem.GetAttribute("entityname").Value;
        }

        switch (elem.GetAttribute("operator").Value)
        {
            case "eq":
                op = ConditionOperator.Equal;
                break;
            case "ne":
            case "neq":
                op = ConditionOperator.NotEqual;
                break;
            case "begins-with":
                op = ConditionOperator.BeginsWith;
                break;
            case "not-begin-with":
                op = ConditionOperator.DoesNotBeginWith;
                break;
            case "ends-with":
                op = ConditionOperator.EndsWith;
                break;
            case "not-end-with":
                op = ConditionOperator.DoesNotEndWith;
                break;
            case "in":
                op = ConditionOperator.In;
                break;
            case "not-in":
                op = ConditionOperator.NotIn;
                break;
            case "null":
                op = ConditionOperator.Null;
                break;
            case "not-null":
                op = ConditionOperator.NotNull;
                break;
            case "like":
                op = ConditionOperator.Like;

                if (value != null)
                {
                    if (value.StartsWith("%") && !value.EndsWith("%"))
                        op = ConditionOperator.EndsWith;
                    else if (!value.StartsWith("%") && value.EndsWith("%"))
                        op = ConditionOperator.BeginsWith;
                    else if (value.StartsWith("%") && value.EndsWith("%"))
                        op = ConditionOperator.Contains;

                    value = value.Replace("%", "");
                }
                break;
            case "not-like":
                op = ConditionOperator.NotLike;
                if (value != null)
                {
                    if (value.StartsWith("%") && !value.EndsWith("%"))
                        op = ConditionOperator.DoesNotEndWith;
                    else if (!value.StartsWith("%") && value.EndsWith("%"))
                        op = ConditionOperator.DoesNotBeginWith;
                    else if (value.StartsWith("%") && value.EndsWith("%"))
                        op = ConditionOperator.DoesNotContain;

                    value = value.Replace("%", "");
                }
                break;
            case "gt":
                op = ConditionOperator.GreaterThan;
                break;
            case "ge":
                op = ConditionOperator.GreaterEqual;
                break;
            case "lt":
                op = ConditionOperator.LessThan;
                break;
            case "le":
                op = ConditionOperator.LessEqual;
                break;
            case "on":
                op = ConditionOperator.On;
                break;
            case "on-or-before":
                op = ConditionOperator.OnOrBefore;
                break;
            case "on-or-after":
                op = ConditionOperator.OnOrAfter;
                break;
            case "today":
                op = ConditionOperator.Today;
                break;
            case "yesterday":
                op = ConditionOperator.Yesterday;
                break;
            case "tomorrow":
                op = ConditionOperator.Tomorrow;
                break;
            case "between":
                op = ConditionOperator.Between;
                break;
            case "not-between":
                op = ConditionOperator.NotBetween;
                break;
            case "eq-userid":
                op = ConditionOperator.EqualUserId;
                break;
            case "ne-userid":
                op = ConditionOperator.NotEqualUserId;
                break;
            case "olderthan-x-months":
                op = ConditionOperator.OlderThanXMonths;
                break;
            case "last-seven-days":
                op = ConditionOperator.Last7Days;
                break;
            case "eq-businessid":
                op = ConditionOperator.EqualBusinessId;
                break;
            case "neq-businessid":
                op = ConditionOperator.NotEqualBusinessId;
                break;
            case "next-x-weeks":
                op = ConditionOperator.NextXWeeks;
                break;
            case "next-seven-days":
                op = ConditionOperator.Next7Days;
                break;
            case "this-year":
                op = ConditionOperator.ThisYear;
                break;
            case "last-year":
                op = ConditionOperator.LastYear;
                break;
            case "next-year":
                op = ConditionOperator.NextYear;
                break;
            case "last-x-hours":
                op = ConditionOperator.LastXHours;
                break;
            case "last-x-days":
                op = ConditionOperator.LastXDays;
                break;
            case "last-x-weeks":
                op = ConditionOperator.LastXWeeks;
                break;
            case "last-x-months":
                op = ConditionOperator.LastXMonths;
                break;
            case "last-x-years":
                op = ConditionOperator.LastXYears;
                break;
            case "next-x-hours":
                op = ConditionOperator.NextXHours;
                break;
            case "next-x-days":
                op = ConditionOperator.NextXDays;
                break;
            case "next-x-months":
                op = ConditionOperator.NextXMonths;
                break;
            case "next-x-years":
                op = ConditionOperator.NextXYears;
                break;
            case "this-month":
                op = ConditionOperator.ThisMonth;
                break;
            case "last-month":
                op = ConditionOperator.LastMonth;
                break;
            case "next-month":
                op = ConditionOperator.NextMonth;
                break;
            case "last-week":
                op = ConditionOperator.LastWeek;
                break;
            case "this-week":
                op = ConditionOperator.ThisWeek;
                break;
            case "next-week":
                op = ConditionOperator.NextWeek;
                break;
            case "in-fiscal-year":
                op = ConditionOperator.InFiscalYear;
                break;
            case "olderthan-x-minutes":
                op = ConditionOperator.OlderThanXMinutes;
                break;
            case "olderthan-x-hours":
                op = ConditionOperator.OlderThanXHours;
                break;
            case "olderthan-x-days":
                op = ConditionOperator.OlderThanXDays;
                break;
            case "olderthan-x-weeks":
                op = ConditionOperator.OlderThanXWeeks;
                break;
            case "olderthan-x-years":
                op = ConditionOperator.OlderThanXYears;
                break;
                case "contain-values":
                    op = ConditionOperator.ContainValues;
                    break;
                case "not-contain-values":
                    op = ConditionOperator.DoesNotContainValues;
                    break;
            default:
                throw PullRequestException.FetchXmlOperatorNotImplemented(elem.GetAttribute("operator").Value);
        }

        //Process values
        object[] values = null;


        var entityName = GetAssociatedEntityNameForConditionExpression(elem);

        //Find values inside the condition expression, if apply
        values = elem
            .Elements() //child nodes of this filter
            .Where(el => el.Name.LocalName.Equals("value"))
            .Select(el => el.ToValue(ctx, entityName, attributeName, op))
            .ToArray();


        //Otherwise, a single value was used
        if (value != null)
        {
            if (string.IsNullOrWhiteSpace(conditionEntityName))
            {
                return new ConditionExpression(attributeName, op, GetConditionExpressionValueCast(value, ctx, entityName, attributeName, op));
            }
            else
            {
                return new ConditionExpression(conditionEntityName, attributeName, op, GetConditionExpressionValueCast(value, ctx, entityName, attributeName, op));
            }
        }
        if (string.IsNullOrWhiteSpace(conditionEntityName))
        {
            return new ConditionExpression(attributeName, op, values);
        }

        return new ConditionExpression(conditionEntityName, attributeName, op, values);
    }


    public static object GetValueBasedOnType(Type t, string value)
    {
        if (t == typeof(int)
            || t == typeof(int?)
            || t.IsOptionSet()
            || t.IsOptionSetValueCollection()
           )
        {
            int intValue = 0;

            if (int.TryParse(value, out intValue))
            {
                if (t.IsOptionSet())
                {
                    return new OptionSetValue(intValue);
                }
                return intValue;
            }
            else
            {
                throw new Exception("Integer value expected");
            }
        }

        else if (t == typeof(Guid)
                 || t == typeof(Guid?)
                 || t == typeof(EntityReference))
        {
            Guid gValue = Guid.Empty;

            if (Guid.TryParse(value, out gValue))
            {
                if (t == typeof(EntityReference))
                {
                    return new EntityReference() { Id = gValue };
                }
                return gValue;
            }
            else
            {
                throw new Exception("Guid value expected");
            }
        }
        else if (t == typeof(decimal)
                 || t == typeof(decimal?)
                 || t == typeof(Money))
        {
            decimal decValue = 0;
            if (decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out decValue))
            {
                if (t == typeof(Money))
                {
                    return new Money(decValue);
                }
                return decValue;
            }
            else
            {
                throw new Exception("Decimal value expected");
            }
        }

        else if (t == typeof(double)
                 || t == typeof(double?))
        {
            double dblValue = 0;
            if (double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out dblValue))
            {
                return dblValue;
            }
            else
            {
                throw new Exception("Double value expected");
            }
        }

        else if (t == typeof(float)
                 || t == typeof(float?))
        {
            float fltValue = 0;
            if (float.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out fltValue))
            {
                return fltValue;
            }
            else
            {
                throw new Exception("Float value expected");
            }
        }

        else if (t == typeof(DateTime)
                 || t == typeof(DateTime?))
        {
            DateTime dtValue = DateTime.MinValue;
            if (DateTime.TryParse(value, out dtValue))
            {
                return dtValue;
            }
            else
            {
                throw new Exception("DateTime value expected");
            }
        }
        //fix Issue #141
        else if (t == typeof(bool)
                 || t == typeof(bool?))
        {
            bool boolValue = false;
            if (bool.TryParse(value, out boolValue))
            {
                return boolValue;
            }
            else
            {
                switch (value)
                {
                    case "0": return false;
                    case "1": return true;
                    default:
                        throw new Exception("Boolean value expected");
                }
            }
        }

        //Otherwise, return the string
        return value;
    }

    public static bool ValueNeedsConverting(ConditionOperator conditionOperator)
    {
        return !OperatorsNotToConvertArray.Contains(conditionOperator);
    }

    public static object GetConditionExpressionValueCast(string value, XrmFakedContext ctx, string sEntityName, string sAttributeName, ConditionOperator op)
    {
        if (ctx.ProxyTypesAssembly != null)
        {
            //We have proxy types so get appropiate type value based on entity name and attribute type
            var reflectedType = ctx.FindReflectedType(sEntityName);
            if (reflectedType != null)
            {
                var attributeType = ctx.FindReflectedAttributeType(reflectedType, sEntityName, sAttributeName);
                if (attributeType != null)
                {
                    try
                    {
                        if (ValueNeedsConverting(op))
                        {
                            return GetValueBasedOnType(attributeType, value);
                        }

                        else
                        {
                            return int.Parse(value);
                        }
                    }
                    catch (Exception e)
                    {
                        throw new Exception(string.Format("When trying to parse value for entity {0} and attribute {1}: {2}", sEntityName, sAttributeName, e.Message));
                    }

                }
            }
        }


        //Try parsing a guid
        Guid gOut = Guid.Empty;
        if (Guid.TryParse(value, out gOut))
            return gOut;

        //Try checking if it is a numeric value, cause, from the fetchxml it 
        //would be impossible to know the real typed based on the string value only
        // ex: "123" might compared as a string, or, as an int, it will depend on the attribute
        //    data type, therefore, in this case we do need to use proxy types

        bool bIsNumeric = false;
        bool bIsDateTime = false;
        double dblValue = 0.0;
        decimal decValue = 0.0m;
        int intValue = 0;

        if (double.TryParse(value, out dblValue))
            bIsNumeric = true;

        if (decimal.TryParse(value, out decValue))
            bIsNumeric = true;

        if (int.TryParse(value, out intValue))
            bIsNumeric = true;

        DateTime dtValue = DateTime.MinValue;
        if (DateTime.TryParse(value, out dtValue))
            bIsDateTime = true;

        if (bIsNumeric || bIsDateTime)
        {
            throw new Exception("When using arithmetic values in Fetch a ProxyTypesAssembly must be used in order to know which types to cast values to.");
        }

        //Default value
        return value;
    }
}