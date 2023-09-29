using Nop.Core.Configuration;

namespace Nop.Core.Security
{
    public partial class CookieSettings : ISettings
    {
        /// <summary>
        /// Expiration time on hours for the "Customer" cookie
        /// </summary>
        public int CustomerCookieExpires { get; set; }
    }
}
