using System.Collections.Generic;
using Nop.Web.Framework.Mvc;
using Nop.Web.Framework.Models;
using Nop.Core.Domain.Common;

namespace Nop.Plugin.Misc.ZsWebApi.Models.Customer
{
    public partial record CustomerAttributeModel : BaseNopEntityModel
    {
        public CustomerAttributeModel()
        {
            Values = new List<CustomerAttributeValueModel>();
        }

        public string Name { get; set; }

        public bool IsRequired { get; set; }

        public string DefaultValue { get; set; }

        public AttributeControlType AttributeControlType { get; set; }
        
        public string Type { get; set; }

        public IList<CustomerAttributeValueModel> Values { get; set; }

    }

    public partial record CustomerAttributeValueModel : BaseNopEntityModel
    {
        public string Name { get; set; }

        public bool IsPreSelected { get; set; }
    }
}