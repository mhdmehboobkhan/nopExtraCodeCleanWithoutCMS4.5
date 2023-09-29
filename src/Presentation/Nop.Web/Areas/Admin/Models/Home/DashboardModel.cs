using Nop.Web.Areas.Admin.Models.Common;
using Nop.Web.Areas.Admin.Models.Reports;
using Nop.Web.Framework.Models;

namespace Nop.Web.Areas.Admin.Models.Home
{
    /// <summary>
    /// Represents a dashboard model
    /// </summary>
    public partial record DashboardModel : BaseNopModel
    {
        #region Ctor

        public DashboardModel()
        {
            PopularSearchTerms = new PopularSearchTermSearchModel();
        }

        #endregion

        #region Properties

        public PopularSearchTermSearchModel PopularSearchTerms { get; set; }

        #endregion
    }
}