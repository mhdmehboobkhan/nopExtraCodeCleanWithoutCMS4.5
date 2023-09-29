using Nop.Web.Framework.Models;
using Nop.Web.Framework.Mvc.ModelBinding;

namespace Nop.Web.Areas.Admin.Models.Settings
{
    /// <summary>
    /// Represents a Sitemap settings model
    /// </summary>
    public partial record SitemapSettingsModel : BaseNopModel, ISettingsModel
    {
        public int ActiveStoreScopeConfiguration { get; set; }

        [NopResourceDisplayName("Admin.Configuration.Settings.GeneralCommon.SitemapEnabled")]
        public bool SitemapEnabled { get; set; }
        public bool SitemapEnabled_OverrideForStore { get; set; }

        [NopResourceDisplayName("Admin.Configuration.Settings.GeneralCommon.SitemapIncludeTopics")]
        public bool SitemapIncludeTopics { get; set; }
        public bool SitemapIncludeTopics_OverrideForStore { get; set; }

        [NopResourceDisplayName("Admin.Configuration.Settings.GeneralCommon.SitemapPageSize")]
        public int SitemapPageSize { get; set; }
        public bool SitemapPageSize_OverrideForStore { get; set; }
    }
}
