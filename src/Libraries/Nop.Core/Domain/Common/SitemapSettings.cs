using Nop.Core.Configuration;

namespace Nop.Core.Domain.Common
{
    /// <summary>
    /// Sitemap settings
    /// </summary>
    public class SitemapSettings : ISettings
    {
        /// <summary>
        /// Gets or sets a value indicating whether sitemap is enabled
        /// </summary>
        public bool SitemapEnabled { get; set; }

        /// <summary>
        /// Gets or sets the page size for sitemap
        /// </summary>
        public int SitemapPageSize { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to include topics to sitemap
        /// </summary>
        public bool SitemapIncludeTopics { get; set; }
    }
}
