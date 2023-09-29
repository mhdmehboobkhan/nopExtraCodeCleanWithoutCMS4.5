using Nop.Web.Framework.Models;
using Nop.Web.Framework.Mvc.ModelBinding;

namespace Nop.Web.Areas.Admin.Models.Settings
{
    /// <summary>
    /// Represents a display default footer item settings model
    /// </summary>
    public partial record DisplayDefaultFooterItemSettingsModel : BaseNopModel, ISettingsModel
    {
        #region Properties

        public int ActiveStoreScopeConfiguration { get; set; }

        [NopResourceDisplayName("Admin.Configuration.Settings.GeneralCommon.DisplayDefaultFooterItemSettingsModel.DisplaySitemapFooterItem")]
        public bool DisplaySitemapFooterItem { get; set; }
        public bool DisplaySitemapFooterItem_OverrideForStore { get; set; }

        [NopResourceDisplayName("Admin.Configuration.Settings.GeneralCommon.DisplayDefaultFooterItemSettingsModel.DisplayContactUsFooterItem")]
        public bool DisplayContactUsFooterItem { get; set; }
        public bool DisplayContactUsFooterItem_OverrideForStore { get; set; }

        [NopResourceDisplayName("Admin.Configuration.Settings.GeneralCommon.DisplayDefaultFooterItemSettingsModel.DisplayCustomerInfoFooterItem")]
        public bool DisplayCustomerInfoFooterItem { get; set; }
        public bool DisplayCustomerInfoFooterItem_OverrideForStore { get; set; }

        [NopResourceDisplayName("Admin.Configuration.Settings.GeneralCommon.DisplayDefaultFooterItemSettingsModel.DisplayCustomerAddressesFooterItem")]
        public bool DisplayCustomerAddressesFooterItem { get; set; }
        public bool DisplayCustomerAddressesFooterItem_OverrideForStore { get; set; }

        #endregion
    }
}