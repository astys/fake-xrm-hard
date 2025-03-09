using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.Text;

namespace FakeXrmEasy.Extensions
{
    public static class EntityReferenceExtensions
    {
        public static bool HasKeyAttributes(this EntityReference er)
        {
            if(er == null)
            {
                return false;
            }
            
            return er.KeyAttributes.Count > 0;
        }
    }
}
