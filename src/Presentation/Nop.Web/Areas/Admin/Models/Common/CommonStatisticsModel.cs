using Nop.Web.Framework.Models;

namespace Nop.Web.Areas.Admin.Models.Common
{
    public partial record CommonStatisticsModel : BaseNopModel
    {
        public int NumberOfCustomers { get; set; }
    }
}