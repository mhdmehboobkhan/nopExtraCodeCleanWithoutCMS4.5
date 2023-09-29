using Nop.Core.Domain.Customers;
using Nop.Web.Framework.Models;

namespace Nop.Web.Models.Common
{
    public partial record HeaderLinksModel : BaseNopModel
    {
        public bool IsAuthenticated { get; set; }
        public string CustomerName { get; set; }
        
        public string AlertMessage { get; set; }
        public UserRegistrationType RegistrationType { get; set; }
    }
}