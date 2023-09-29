using System.Collections.Generic;
using System.Linq;
using Nop.Web.Framework.Models;

namespace Nop.Web.Models.Common
{
    public partial record TopMenuModel : BaseNopModel
    {
        public TopMenuModel()
        {
            Topics = new List<TopicModel>();
        }

        public IList<TopicModel> Topics { get; set; }

        public bool DisplayHomepageMenuItem { get; set; }
        public bool DisplayCustomerInfoMenuItem { get; set; }
        public bool DisplayContactUsMenuItem { get; set; }

        public bool UseAjaxMenu { get; set; }

        #region Nested classes

        public record TopicModel : BaseNopEntityModel
        {
            public string Name { get; set; }
            public string SeName { get; set; }
        }

        #endregion
    }
}