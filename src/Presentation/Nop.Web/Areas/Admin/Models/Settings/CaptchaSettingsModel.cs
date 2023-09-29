using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Web.Framework.Models;
using Nop.Web.Framework.Mvc.ModelBinding;

namespace Nop.Web.Areas.Admin.Models.Settings
{
    /// <summary>
    /// Represents a CAPTCHA settings model
    /// </summary>
    public partial record CaptchaSettingsModel : BaseNopModel, ISettingsModel
    {
        #region Properties

        public int ActiveStoreScopeConfiguration { get; set; }

        [NopResourceDisplayName("Admin.Configuration.Settings.GeneralCommon.CaptchaEnabled")]
        public bool Enabled { get; set; }
        public bool Enabled_OverrideForStore { get; set; }

        [NopResourceDisplayName("Admin.Configuration.Settings.GeneralCommon.CaptchaShowOnLoginPage")]
        public bool ShowOnLoginPage { get; set; }
        public bool ShowOnLoginPage_OverrideForStore { get; set; }

        [NopResourceDisplayName("Admin.Configuration.Settings.GeneralCommon.CaptchaShowOnRegistrationPage")]
        public bool ShowOnRegistrationPage { get; set; }
        public bool ShowOnRegistrationPage_OverrideForStore { get; set; }

        [NopResourceDisplayName("Admin.Configuration.Settings.GeneralCommon.CaptchaShowOnContactUsPage")]
        public bool ShowOnContactUsPage { get; set; }
        public bool ShowOnContactUsPage_OverrideForStore { get; set; }

        [NopResourceDisplayName("Admin.Configuration.Settings.GeneralCommon.CaptchaShowOnForgotPasswordPage")]
        public bool ShowOnForgotPasswordPage { get; set; }
        public bool ShowOnForgotPasswordPage_OverrideForStore { get; set; }

        [NopResourceDisplayName("Admin.Configuration.Settings.GeneralCommon.reCaptchaPublicKey")]
        public string ReCaptchaPublicKey { get; set; }
        public bool ReCaptchaPublicKey_OverrideForStore { get; set; }

        [NopResourceDisplayName("Admin.Configuration.Settings.GeneralCommon.reCaptchaPrivateKey")]
        public string ReCaptchaPrivateKey { get; set; }
        public bool ReCaptchaPrivateKey_OverrideForStore { get; set; }

        [NopResourceDisplayName("Admin.Configuration.Settings.GeneralCommon.CaptchaType")]
        public int CaptchaType { get; set; }
        public bool CaptchaType_OverrideForStore { get; set; }
        public SelectList CaptchaTypeValues { get; set; }

        [NopResourceDisplayName("Admin.Configuration.Settings.GeneralCommon.reCaptchaV3ScoreThreshold")]
        public decimal ReCaptchaV3ScoreThreshold { get; set; }
        public bool ReCaptchaV3ScoreThreshold_OverrideForStore { get; set; }

        #endregion
    }
}