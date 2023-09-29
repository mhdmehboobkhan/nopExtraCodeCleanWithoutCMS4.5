using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Nop.Core;
using Nop.Core.Configuration;
using Nop.Core.Domain;
using Nop.Core.Domain.Common;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Localization;
using Nop.Core.Domain.Media;
using Nop.Core.Domain.Security;
using Nop.Core.Domain.Seo;
using Nop.Core.Events;
using Nop.Core.Infrastructure;
using Nop.Data;
using Nop.Data.Configuration;
using Nop.Services.Authentication.MultiFactor;
using Nop.Services.Common;
using Nop.Services.Configuration;
using Nop.Services.Customers;
using Nop.Services.Helpers;
using Nop.Services.Localization;
using Nop.Services.Logging;
using Nop.Services.Media;
using Nop.Services.Media.RoxyFileman;
using Nop.Services.Messages;
using Nop.Services.Plugins;
using Nop.Services.Security;
using Nop.Services.Stores;
using Nop.Web.Areas.Admin.Factories;
using Nop.Web.Areas.Admin.Infrastructure.Mapper.Extensions;
using Nop.Web.Areas.Admin.Models.Settings;
using Nop.Web.Framework;
using Nop.Web.Framework.Configuration;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Mvc;
using Nop.Web.Framework.Mvc.Filters;
using Nop.Web.Framework.Mvc.ModelBinding;

namespace Nop.Web.Areas.Admin.Controllers
{
    public partial class SettingController : BaseAdminController
    {
        #region Fields

        private readonly AppSettings _appSettings;
        private readonly ICustomerActivityService _customerActivityService;
        private readonly ICustomerService _customerService;
        private readonly INopDataProvider _dataProvider;
        private readonly IEncryptionService _encryptionService;
        private readonly IEventPublisher _eventPublisher;
        private readonly IGenericAttributeService _genericAttributeService;
        private readonly ILocalizedEntityService _localizedEntityService;
        private readonly ILocalizationService _localizationService;
        private readonly IMultiFactorAuthenticationPluginManager _multiFactorAuthenticationPluginManager;
        private readonly INopFileProvider _fileProvider;
        private readonly INotificationService _notificationService;
        private readonly IPermissionService _permissionService;
        private readonly IPictureService _pictureService;
        private readonly IRoxyFilemanService _roxyFilemanService;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly ISettingModelFactory _settingModelFactory;
        private readonly ISettingService _settingService;
        private readonly IStoreContext _storeContext;
        private readonly IStoreService _storeService;
        private readonly IWorkContext _workContext;
        private readonly IUploadService _uploadService;

        #endregion

        #region Ctor

        public SettingController(AppSettings appSettings,
            ICustomerActivityService customerActivityService,
            ICustomerService customerService,
            INopDataProvider dataProvider,
            IEncryptionService encryptionService,
            IEventPublisher eventPublisher,
            IGenericAttributeService genericAttributeService,
            ILocalizedEntityService localizedEntityService,
            ILocalizationService localizationService,
            IMultiFactorAuthenticationPluginManager multiFactorAuthenticationPluginManager,
            INopFileProvider fileProvider,
            INotificationService notificationService,
            IPermissionService permissionService,
            IPictureService pictureService,
            IRoxyFilemanService roxyFilemanService,
            IServiceScopeFactory serviceScopeFactory,
            ISettingModelFactory settingModelFactory,
            ISettingService settingService,
            IStoreContext storeContext,
            IStoreService storeService,
            IWorkContext workContext,
            IUploadService uploadService)
        {
            _appSettings = appSettings;
            _customerActivityService = customerActivityService;
            _customerService = customerService;
            _dataProvider = dataProvider;
            _encryptionService = encryptionService;
            _eventPublisher = eventPublisher;
            _genericAttributeService = genericAttributeService;
            _localizedEntityService = localizedEntityService;
            _localizationService = localizationService;
            _multiFactorAuthenticationPluginManager = multiFactorAuthenticationPluginManager;
            _fileProvider = fileProvider;
            _notificationService = notificationService;
            _permissionService = permissionService;
            _pictureService = pictureService;
            _roxyFilemanService = roxyFilemanService;
            _serviceScopeFactory = serviceScopeFactory;
            _settingModelFactory = settingModelFactory;
            _settingService = settingService;
            _storeContext = storeContext;
            _storeService = storeService;
            _workContext = workContext;
            _uploadService = uploadService;
        }

        #endregion

        #region Utilities

        #endregion

        #region Methods

        public virtual async Task<IActionResult> ChangeStoreScopeConfiguration(int storeid, string returnUrl = "")
        {
            var store = await _storeService.GetStoreByIdAsync(storeid);
            if (store != null || storeid == 0)
            {
                await _genericAttributeService
                    .SaveAttributeAsync(await _workContext.GetCurrentCustomerAsync(), NopCustomerDefaults.AdminAreaStoreScopeConfigurationAttribute, storeid);
            }

            //home page
            if (string.IsNullOrEmpty(returnUrl))
                returnUrl = Url.Action("Index", "Home", new { area = AreaNames.Admin });

            //prevent open redirection attack
            if (!Url.IsLocalUrl(returnUrl))
                return RedirectToAction("Index", "Home", new { area = AreaNames.Admin });

            return Redirect(returnUrl);
        }

        public virtual async Task<IActionResult> AppSettings()
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManageAppSettings))
                return AccessDeniedView();

            //prepare model
            var model = await _settingModelFactory.PrepareAppSettingsModel();

            return View(model);
        }

        [HttpPost]
        public virtual async Task<IActionResult> AppSettings(AppSettingsModel model)
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManageAppSettings))
                return AccessDeniedView();

            if (ModelState.IsValid)
            {
                var configurations = new List<IConfig>
                {
                    model.CacheConfigModel.ToConfig(_appSettings.Get<CacheConfig>()),
                    model.HostingConfigModel.ToConfig(_appSettings.Get<HostingConfig>()),
                    model.DistributedCacheConfigModel.ToConfig(_appSettings.Get<DistributedCacheConfig>()),
                    model.AzureBlobConfigModel.ToConfig(_appSettings.Get<AzureBlobConfig>()),
                    model.InstallationConfigModel.ToConfig(_appSettings.Get<InstallationConfig>()),
                    model.PluginConfigModel.ToConfig(_appSettings.Get<PluginConfig>()),
                    model.CommonConfigModel.ToConfig(_appSettings.Get<CommonConfig>()),
                    model.DataConfigModel.ToConfig(_appSettings.Get<DataConfig>()),
                    model.WebOptimizerConfigModel.ToConfig(_appSettings.Get<WebOptimizerConfig>())
                };

                await _eventPublisher.PublishAsync(new AppSettingsSavingEvent(configurations));

                AppSettingsHelper.SaveAppSettings(configurations, _fileProvider);

                await _customerActivityService.InsertActivityAsync("EditSettings",
                    await _localizationService.GetResourceAsync("ActivityLog.EditSettings"));

                _notificationService.SuccessNotification(
                    await _localizationService.GetResourceAsync("Admin.Configuration.Updated"));

                var returnUrl = Url.Action("AppSettings", "Setting", new {area = AreaNames.Admin});
                return View("RestartApplication", returnUrl);
            }

            //prepare model
            model = await _settingModelFactory.PrepareAppSettingsModel(model);

            //if we got this far, something failed, redisplay form
            return View(model);
        }

        public virtual async Task<IActionResult> Media()
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManageSettings))
                return AccessDeniedView();

            //prepare model
            var model = await _settingModelFactory.PrepareMediaSettingsModelAsync();

            return View(model);
        }

        [HttpPost]
        [FormValueRequired("save")]
        public virtual async Task<IActionResult> Media(MediaSettingsModel model)
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManageSettings))
                return AccessDeniedView();

            if (ModelState.IsValid)
            { 
                //load settings for a chosen store scope
                var storeScope = await _storeContext.GetActiveStoreScopeConfigurationAsync();
                var mediaSettings = await _settingService.LoadSettingAsync<MediaSettings>(storeScope);
                mediaSettings = model.ToSettings(mediaSettings);

                //we do not clear cache after each setting update.
                //this behavior can increase performance because cached settings will not be cleared 
                //and loaded from database after each update
                await _settingService.SaveSettingOverridablePerStoreAsync(mediaSettings, x => x.AvatarPictureSize, model.AvatarPictureSize_OverrideForStore, storeScope, false);
                await _settingService.SaveSettingOverridablePerStoreAsync(mediaSettings, x => x.MaximumImageSize, model.MaximumImageSize_OverrideForStore, storeScope, false);
                await _settingService.SaveSettingOverridablePerStoreAsync(mediaSettings, x => x.MultipleThumbDirectories, model.MultipleThumbDirectories_OverrideForStore, storeScope, false);
                await _settingService.SaveSettingOverridablePerStoreAsync(mediaSettings, x => x.DefaultImageQuality, model.DefaultImageQuality_OverrideForStore, storeScope, false);
                await _settingService.SaveSettingOverridablePerStoreAsync(mediaSettings, x => x.DefaultPictureZoomEnabled, model.DefaultPictureZoomEnabled_OverrideForStore, storeScope, false);

                //now clear settings cache
                await _settingService.ClearCacheAsync();

                //activity log
                await _customerActivityService.InsertActivityAsync("EditSettings", await _localizationService.GetResourceAsync("ActivityLog.EditSettings"));

                _notificationService.SuccessNotification(await _localizationService.GetResourceAsync("Admin.Configuration.Updated"));

                return RedirectToAction("Media");
            }

            //prepare model
            model = await _settingModelFactory.PrepareMediaSettingsModelAsync(model);

            //if we got this far, something failed, redisplay form
            return View(model);
        }

        [HttpPost, ActionName("Media")]
        [FormValueRequired("change-picture-storage")]
        public virtual async Task<IActionResult> ChangePictureStorage()
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManageSettings))
                return AccessDeniedView();

            await _roxyFilemanService.FlushAllImagesOnDiskAsync();

            await _pictureService.SetIsStoreInDbAsync(!await _pictureService.IsStoreInDbAsync());

            //use "Resolve" to load the correct service
            //we do it because the IRoxyFilemanService service is registered for
            //a scope and in the usual way to get a new instance there is no possibility
            using (var scope = _serviceScopeFactory.CreateScope())
            {
                var newRoxyFilemanService = EngineContext.Current.Resolve<IRoxyFilemanService>(scope);
                await newRoxyFilemanService.ConfigureAsync();
            }

            //activity log
            await _customerActivityService.InsertActivityAsync("EditSettings", await _localizationService.GetResourceAsync("ActivityLog.EditSettings"));

            _notificationService.SuccessNotification(await _localizationService.GetResourceAsync("Admin.Configuration.Updated"));

            return RedirectToAction("Media");
        }

        public virtual async Task<IActionResult> CustomerUser()
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManageSettings))
                return AccessDeniedView();

            //prepare model
            var model = await _settingModelFactory.PrepareCustomerUserSettingsModelAsync();

            return View(model);
        }

        [HttpPost]
        public virtual async Task<IActionResult> CustomerUser(CustomerUserSettingsModel model)
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManageSettings))
                return AccessDeniedView();

            if (ModelState.IsValid)
            { 
                var storeScope = await _storeContext.GetActiveStoreScopeConfigurationAsync();
                var customerSettings = await _settingService.LoadSettingAsync<CustomerSettings>(storeScope);

                var lastUsernameValidationRule = customerSettings.UsernameValidationRule;
                var lastUsernameValidationEnabledValue = customerSettings.UsernameValidationEnabled;
                var lastUsernameValidationUseRegexValue = customerSettings.UsernameValidationUseRegex;

                //Phone number validation settings
                var lastPhoneNumberValidationRule = customerSettings.PhoneNumberValidationRule;
                var lastPhoneNumberValidationEnabledValue = customerSettings.PhoneNumberValidationEnabled;
                var lastPhoneNumberValidationUseRegexValue = customerSettings.PhoneNumberValidationUseRegex;

                var dateTimeSettings = await _settingService.LoadSettingAsync<DateTimeSettings>(storeScope);
                var externalAuthenticationSettings = await _settingService.LoadSettingAsync<ExternalAuthenticationSettings>(storeScope);
                var multiFactorAuthenticationSettings = await _settingService.LoadSettingAsync<MultiFactorAuthenticationSettings>(storeScope);

                customerSettings = model.CustomerSettings.ToSettings(customerSettings);

                if (customerSettings.UsernameValidationEnabled && customerSettings.UsernameValidationUseRegex)
                {
                    try
                    {
                        //validate regex rule
                        var unused = Regex.IsMatch("test_user_name", customerSettings.UsernameValidationRule);
                    }
                    catch (ArgumentException)
                    {
                        //restoring previous settings
                        customerSettings.UsernameValidationRule = lastUsernameValidationRule;
                        customerSettings.UsernameValidationEnabled = lastUsernameValidationEnabledValue;
                        customerSettings.UsernameValidationUseRegex = lastUsernameValidationUseRegexValue;

                        _notificationService.ErrorNotification(await _localizationService.GetResourceAsync("Admin.Configuration.Settings.CustomerSettings.RegexValidationRule.Error"));
                    }
                }

                if (customerSettings.PhoneNumberValidationEnabled && customerSettings.PhoneNumberValidationUseRegex)
                {
                    try
                    {
                        //validate regex rule
                        var unused = Regex.IsMatch("123456789", customerSettings.PhoneNumberValidationRule);
                    }
                    catch (ArgumentException)
                    {
                        //restoring previous settings
                        customerSettings.PhoneNumberValidationRule = lastPhoneNumberValidationRule;
                        customerSettings.PhoneNumberValidationEnabled = lastPhoneNumberValidationEnabledValue;
                        customerSettings.PhoneNumberValidationUseRegex = lastPhoneNumberValidationUseRegexValue;

                        _notificationService.ErrorNotification(await _localizationService.GetResourceAsync("Admin.Configuration.Settings.CustomerSettings.PhoneNumberRegexValidationRule.Error"));
                    }
                }

                await _settingService.SaveSettingAsync(customerSettings);

                dateTimeSettings.DefaultStoreTimeZoneId = model.DateTimeSettings.DefaultStoreTimeZoneId;
                dateTimeSettings.AllowCustomersToSetTimeZone = model.DateTimeSettings.AllowCustomersToSetTimeZone;
                await _settingService.SaveSettingAsync(dateTimeSettings);

                externalAuthenticationSettings.AllowCustomersToRemoveAssociations = model.ExternalAuthenticationSettings.AllowCustomersToRemoveAssociations;
                await _settingService.SaveSettingAsync(externalAuthenticationSettings);

                multiFactorAuthenticationSettings = model.MultiFactorAuthenticationSettings.ToSettings(multiFactorAuthenticationSettings);
                await _settingService.SaveSettingAsync(multiFactorAuthenticationSettings);

                //activity log
                await _customerActivityService.InsertActivityAsync("EditSettings", await _localizationService.GetResourceAsync("ActivityLog.EditSettings"));

                _notificationService.SuccessNotification(await _localizationService.GetResourceAsync("Admin.Configuration.Updated"));

                return RedirectToAction("CustomerUser");
            }

            //prepare model
            model = await _settingModelFactory.PrepareCustomerUserSettingsModelAsync(model);

            //if we got this far, something failed, redisplay form
            return View(model);
        }

        public virtual async Task<IActionResult> GeneralCommon(bool showtour = false)
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManageSettings))
                return AccessDeniedView();

            //prepare model
            var model = await _settingModelFactory.PrepareGeneralCommonSettingsModelAsync();

            //show configuration tour
            if (showtour)
            {
                var customer = await _workContext.GetCurrentCustomerAsync();
                var hideCard = await _genericAttributeService.GetAttributeAsync<bool>(customer, NopCustomerDefaults.HideConfigurationStepsAttribute);
                var closeCard = await _genericAttributeService.GetAttributeAsync<bool>(customer, NopCustomerDefaults.CloseConfigurationStepsAttribute);

                if (!hideCard && !closeCard)
                    ViewBag.ShowTour = true;
            }

            return View(model);
        }

        [HttpPost]
        [FormValueRequired("save")]
        public virtual async Task<IActionResult> GeneralCommon(GeneralCommonSettingsModel model)
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManageSettings))
                return AccessDeniedView();

            if (ModelState.IsValid)
            { 
                //load settings for a chosen store scope
                var storeScope = await _storeContext.GetActiveStoreScopeConfigurationAsync();

                //store information settings
                var storeInformationSettings = await _settingService.LoadSettingAsync<StoreInformationSettings>(storeScope);
                var commonSettings = await _settingService.LoadSettingAsync<CommonSettings>(storeScope);
                var sitemapSettings = await _settingService.LoadSettingAsync<SitemapSettings>(storeScope);

                storeInformationSettings.StoreClosed = model.StoreInformationSettings.StoreClosed;
                storeInformationSettings.DefaultStoreTheme = model.StoreInformationSettings.DefaultStoreTheme;
                storeInformationSettings.AllowCustomerToSelectTheme = model.StoreInformationSettings.AllowCustomerToSelectTheme;
                storeInformationSettings.LogoPictureId = model.StoreInformationSettings.LogoPictureId;
                //EU Cookie law
                storeInformationSettings.DisplayEuCookieLawWarning = model.StoreInformationSettings.DisplayEuCookieLawWarning;
                //social pages
                storeInformationSettings.FacebookLink = model.StoreInformationSettings.FacebookLink;
                storeInformationSettings.TwitterLink = model.StoreInformationSettings.TwitterLink;
                storeInformationSettings.YoutubeLink = model.StoreInformationSettings.YoutubeLink;
                //contact us
                commonSettings.SubjectFieldOnContactUsForm = model.StoreInformationSettings.SubjectFieldOnContactUsForm;
                commonSettings.UseSystemEmailForContactUsForm = model.StoreInformationSettings.UseSystemEmailForContactUsForm;
                //terms of service
                commonSettings.PopupForTermsOfServiceLinks = model.StoreInformationSettings.PopupForTermsOfServiceLinks;
                //sitemap
                sitemapSettings.SitemapEnabled = model.SitemapSettings.SitemapEnabled;
                sitemapSettings.SitemapPageSize = model.SitemapSettings.SitemapPageSize;
                sitemapSettings.SitemapIncludeTopics = model.SitemapSettings.SitemapIncludeTopics;

                //minification
                commonSettings.EnableHtmlMinification = model.MinificationSettings.EnableHtmlMinification;
                //use response compression
                commonSettings.UseResponseCompression = model.MinificationSettings.UseResponseCompression;

                //we do not clear cache after each setting update.
                //this behavior can increase performance because cached settings will not be cleared 
                //and loaded from database after each update
                await _settingService.SaveSettingOverridablePerStoreAsync(storeInformationSettings, x => x.StoreClosed, model.StoreInformationSettings.StoreClosed_OverrideForStore, storeScope, false);
                await _settingService.SaveSettingOverridablePerStoreAsync(storeInformationSettings, x => x.DefaultStoreTheme, model.StoreInformationSettings.DefaultStoreTheme_OverrideForStore, storeScope, false);
                await _settingService.SaveSettingOverridablePerStoreAsync(storeInformationSettings, x => x.AllowCustomerToSelectTheme, model.StoreInformationSettings.AllowCustomerToSelectTheme_OverrideForStore, storeScope, false);
                await _settingService.SaveSettingOverridablePerStoreAsync(storeInformationSettings, x => x.LogoPictureId, model.StoreInformationSettings.LogoPictureId_OverrideForStore, storeScope, false);
                await _settingService.SaveSettingOverridablePerStoreAsync(storeInformationSettings, x => x.DisplayEuCookieLawWarning, model.StoreInformationSettings.DisplayEuCookieLawWarning_OverrideForStore, storeScope, false);
                await _settingService.SaveSettingOverridablePerStoreAsync(storeInformationSettings, x => x.FacebookLink, model.StoreInformationSettings.FacebookLink_OverrideForStore, storeScope, false);
                await _settingService.SaveSettingOverridablePerStoreAsync(storeInformationSettings, x => x.TwitterLink, model.StoreInformationSettings.TwitterLink_OverrideForStore, storeScope, false);
                await _settingService.SaveSettingOverridablePerStoreAsync(storeInformationSettings, x => x.YoutubeLink, model.StoreInformationSettings.YoutubeLink_OverrideForStore, storeScope, false);
                await _settingService.SaveSettingOverridablePerStoreAsync(commonSettings, x => x.SubjectFieldOnContactUsForm, model.StoreInformationSettings.SubjectFieldOnContactUsForm_OverrideForStore, storeScope, false);
                await _settingService.SaveSettingOverridablePerStoreAsync(commonSettings, x => x.UseSystemEmailForContactUsForm, model.StoreInformationSettings.UseSystemEmailForContactUsForm_OverrideForStore, storeScope, false);
                await _settingService.SaveSettingOverridablePerStoreAsync(commonSettings, x => x.PopupForTermsOfServiceLinks, model.StoreInformationSettings.PopupForTermsOfServiceLinks_OverrideForStore, storeScope, false);
                await _settingService.SaveSettingOverridablePerStoreAsync(sitemapSettings, x => x.SitemapEnabled, model.SitemapSettings.SitemapEnabled_OverrideForStore, storeScope, false);
                await _settingService.SaveSettingOverridablePerStoreAsync(sitemapSettings, x => x.SitemapPageSize, model.SitemapSettings.SitemapPageSize_OverrideForStore, storeScope, false);
                await _settingService.SaveSettingOverridablePerStoreAsync(sitemapSettings, x => x.SitemapIncludeTopics, model.SitemapSettings.SitemapIncludeTopics_OverrideForStore, storeScope, false);
                await _settingService.SaveSettingOverridablePerStoreAsync(commonSettings, x => x.EnableHtmlMinification, model.MinificationSettings.EnableHtmlMinification_OverrideForStore, storeScope, false);
                await _settingService.SaveSettingOverridablePerStoreAsync(commonSettings, x => x.UseResponseCompression, model.MinificationSettings.UseResponseCompression_OverrideForStore, storeScope, false);

                //now clear settings cache
                await _settingService.ClearCacheAsync();

                //seo settings
                var seoSettings = await _settingService.LoadSettingAsync<SeoSettings>(storeScope);
                seoSettings.PageTitleSeparator = model.SeoSettings.PageTitleSeparator;
                seoSettings.PageTitleSeoAdjustment = (PageTitleSeoAdjustment)model.SeoSettings.PageTitleSeoAdjustment;
                seoSettings.HomepageTitle = model.SeoSettings.HomepageTitle;
                seoSettings.HomepageDescription = model.SeoSettings.HomepageDescription;
                seoSettings.DefaultTitle = model.SeoSettings.DefaultTitle;
                seoSettings.DefaultMetaKeywords = model.SeoSettings.DefaultMetaKeywords;
                seoSettings.DefaultMetaDescription = model.SeoSettings.DefaultMetaDescription;
                seoSettings.ConvertNonWesternChars = model.SeoSettings.ConvertNonWesternChars;
                seoSettings.CanonicalUrlsEnabled = model.SeoSettings.CanonicalUrlsEnabled;
                seoSettings.WwwRequirement = (WwwRequirement)model.SeoSettings.WwwRequirement;
                seoSettings.TwitterMetaTags = model.SeoSettings.TwitterMetaTags;
                seoSettings.OpenGraphMetaTags = model.SeoSettings.OpenGraphMetaTags;
                seoSettings.MicrodataEnabled = model.SeoSettings.MicrodataEnabled;
                seoSettings.CustomHeadTags = model.SeoSettings.CustomHeadTags;

                //we do not clear cache after each setting update.
                //this behavior can increase performance because cached settings will not be cleared 
                //and loaded from database after each update
                await _settingService.SaveSettingOverridablePerStoreAsync(seoSettings, x => x.PageTitleSeparator, model.SeoSettings.PageTitleSeparator_OverrideForStore, storeScope, false);
                await _settingService.SaveSettingOverridablePerStoreAsync(seoSettings, x => x.PageTitleSeoAdjustment, model.SeoSettings.PageTitleSeoAdjustment_OverrideForStore, storeScope, false);
                await _settingService.SaveSettingOverridablePerStoreAsync(seoSettings, x => x.HomepageTitle, model.SeoSettings.HomepageTitle_OverrideForStore, storeScope, false);
                await _settingService.SaveSettingOverridablePerStoreAsync(seoSettings, x => x.HomepageDescription, model.SeoSettings.HomepageDescription_OverrideForStore, storeScope, false);
                await _settingService.SaveSettingOverridablePerStoreAsync(seoSettings, x => x.DefaultTitle, model.SeoSettings.DefaultTitle_OverrideForStore, storeScope, false);
                await _settingService.SaveSettingOverridablePerStoreAsync(seoSettings, x => x.DefaultMetaKeywords, model.SeoSettings.DefaultMetaKeywords_OverrideForStore, storeScope, false);
                await _settingService.SaveSettingOverridablePerStoreAsync(seoSettings, x => x.DefaultMetaDescription, model.SeoSettings.DefaultMetaDescription_OverrideForStore, storeScope, false);
                await _settingService.SaveSettingOverridablePerStoreAsync(seoSettings, x => x.ConvertNonWesternChars, model.SeoSettings.ConvertNonWesternChars_OverrideForStore, storeScope, false);
                await _settingService.SaveSettingOverridablePerStoreAsync(seoSettings, x => x.CanonicalUrlsEnabled, model.SeoSettings.CanonicalUrlsEnabled_OverrideForStore, storeScope, false);
                await _settingService.SaveSettingOverridablePerStoreAsync(seoSettings, x => x.WwwRequirement, model.SeoSettings.WwwRequirement_OverrideForStore, storeScope, false);
                await _settingService.SaveSettingOverridablePerStoreAsync(seoSettings, x => x.TwitterMetaTags, model.SeoSettings.TwitterMetaTags_OverrideForStore, storeScope, false);
                await _settingService.SaveSettingOverridablePerStoreAsync(seoSettings, x => x.OpenGraphMetaTags, model.SeoSettings.OpenGraphMetaTags_OverrideForStore, storeScope, false);
                await _settingService.SaveSettingOverridablePerStoreAsync(seoSettings, x => x.CustomHeadTags, model.SeoSettings.CustomHeadTags_OverrideForStore, storeScope, false);
                await _settingService.SaveSettingOverridablePerStoreAsync(seoSettings, x => x.MicrodataEnabled, model.SeoSettings.MicrodataEnabled_OverrideForStore, storeScope, false);

                //now clear settings cache
                await _settingService.ClearCacheAsync();

                //security settings
                var securitySettings = await _settingService.LoadSettingAsync<SecuritySettings>(storeScope);
                if (securitySettings.AdminAreaAllowedIpAddresses == null)
                    securitySettings.AdminAreaAllowedIpAddresses = new List<string>();
                securitySettings.AdminAreaAllowedIpAddresses.Clear();
                if (!string.IsNullOrEmpty(model.SecuritySettings.AdminAreaAllowedIpAddresses))
                    foreach (var s in model.SecuritySettings.AdminAreaAllowedIpAddresses.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                        if (!string.IsNullOrWhiteSpace(s))
                            securitySettings.AdminAreaAllowedIpAddresses.Add(s.Trim());
                securitySettings.HoneypotEnabled = model.SecuritySettings.HoneypotEnabled;
                await _settingService.SaveSettingAsync(securitySettings);

                //captcha settings
                var captchaSettings = await _settingService.LoadSettingAsync<CaptchaSettings>(storeScope);
                captchaSettings.Enabled = model.CaptchaSettings.Enabled;
                captchaSettings.ShowOnLoginPage = model.CaptchaSettings.ShowOnLoginPage;
                captchaSettings.ShowOnRegistrationPage = model.CaptchaSettings.ShowOnRegistrationPage;
                captchaSettings.ShowOnContactUsPage = model.CaptchaSettings.ShowOnContactUsPage;
                captchaSettings.ShowOnForgotPasswordPage = model.CaptchaSettings.ShowOnForgotPasswordPage;
                captchaSettings.ReCaptchaPublicKey = model.CaptchaSettings.ReCaptchaPublicKey;
                captchaSettings.ReCaptchaPrivateKey = model.CaptchaSettings.ReCaptchaPrivateKey;
                captchaSettings.CaptchaType = (CaptchaType)model.CaptchaSettings.CaptchaType;
                captchaSettings.ReCaptchaV3ScoreThreshold = model.CaptchaSettings.ReCaptchaV3ScoreThreshold;

                //we do not clear cache after each setting update.
                //this behavior can increase performance because cached settings will not be cleared 
                //and loaded from database after each update
                await _settingService.SaveSettingOverridablePerStoreAsync(captchaSettings, x => x.Enabled, model.CaptchaSettings.Enabled_OverrideForStore, storeScope, false);
                await _settingService.SaveSettingOverridablePerStoreAsync(captchaSettings, x => x.ShowOnLoginPage, model.CaptchaSettings.ShowOnLoginPage_OverrideForStore, storeScope, false);
                await _settingService.SaveSettingOverridablePerStoreAsync(captchaSettings, x => x.ShowOnRegistrationPage, model.CaptchaSettings.ShowOnRegistrationPage_OverrideForStore, storeScope, false);
                await _settingService.SaveSettingOverridablePerStoreAsync(captchaSettings, x => x.ShowOnContactUsPage, model.CaptchaSettings.ShowOnContactUsPage_OverrideForStore, storeScope, false);
                await _settingService.SaveSettingOverridablePerStoreAsync(captchaSettings, x => x.ShowOnForgotPasswordPage, model.CaptchaSettings.ShowOnForgotPasswordPage_OverrideForStore, storeScope, false);
                await _settingService.SaveSettingOverridablePerStoreAsync(captchaSettings, x => x.ReCaptchaPublicKey, model.CaptchaSettings.ReCaptchaPublicKey_OverrideForStore, storeScope, false);
                await _settingService.SaveSettingOverridablePerStoreAsync(captchaSettings, x => x.ReCaptchaPrivateKey, model.CaptchaSettings.ReCaptchaPrivateKey_OverrideForStore, storeScope, false);
                await _settingService.SaveSettingOverridablePerStoreAsync(captchaSettings, x => x.ReCaptchaV3ScoreThreshold, model.CaptchaSettings.ReCaptchaV3ScoreThreshold_OverrideForStore, storeScope, false);
                await _settingService.SaveSettingOverridablePerStoreAsync(captchaSettings, x => x.CaptchaType, model.CaptchaSettings.CaptchaType_OverrideForStore, storeScope, false);

                // now clear settings cache
                await _settingService.ClearCacheAsync();

                if (captchaSettings.Enabled &&
                    (string.IsNullOrWhiteSpace(captchaSettings.ReCaptchaPublicKey) || string.IsNullOrWhiteSpace(captchaSettings.ReCaptchaPrivateKey)))
                {
                    //captcha is enabled but the keys are not entered
                    _notificationService.ErrorNotification(await _localizationService.GetResourceAsync("Admin.Configuration.Settings.GeneralCommon.CaptchaAppropriateKeysNotEnteredError"));
                }

                //PDF settings
                var pdfSettings = await _settingService.LoadSettingAsync<PdfSettings>(storeScope);
                pdfSettings.LetterPageSizeEnabled = model.PdfSettings.LetterPageSizeEnabled;
                pdfSettings.LogoPictureId = model.PdfSettings.LogoPictureId;
                pdfSettings.InvoiceFooterTextColumn1 = model.PdfSettings.InvoiceFooterTextColumn1;
                pdfSettings.InvoiceFooterTextColumn2 = model.PdfSettings.InvoiceFooterTextColumn2;

                //we do not clear cache after each setting update.
                //this behavior can increase performance because cached settings will not be cleared 
                //and loaded from database after each update
                await _settingService.SaveSettingOverridablePerStoreAsync(pdfSettings, x => x.LetterPageSizeEnabled, model.PdfSettings.LetterPageSizeEnabled_OverrideForStore, storeScope, false);
                await _settingService.SaveSettingOverridablePerStoreAsync(pdfSettings, x => x.LogoPictureId, model.PdfSettings.LogoPictureId_OverrideForStore, storeScope, false);
                await _settingService.SaveSettingOverridablePerStoreAsync(pdfSettings, x => x.InvoiceFooterTextColumn1, model.PdfSettings.InvoiceFooterTextColumn1_OverrideForStore, storeScope, false);
                await _settingService.SaveSettingOverridablePerStoreAsync(pdfSettings, x => x.InvoiceFooterTextColumn2, model.PdfSettings.InvoiceFooterTextColumn2_OverrideForStore, storeScope, false);

                //now clear settings cache
                await _settingService.ClearCacheAsync();

                //localization settings
                var localizationSettings = await _settingService.LoadSettingAsync<LocalizationSettings>(storeScope);
                localizationSettings.UseImagesForLanguageSelection = model.LocalizationSettings.UseImagesForLanguageSelection;
                if (localizationSettings.SeoFriendlyUrlsForLanguagesEnabled != model.LocalizationSettings.SeoFriendlyUrlsForLanguagesEnabled)
                {
                    localizationSettings.SeoFriendlyUrlsForLanguagesEnabled = model.LocalizationSettings.SeoFriendlyUrlsForLanguagesEnabled;
                }

                localizationSettings.AutomaticallyDetectLanguage = model.LocalizationSettings.AutomaticallyDetectLanguage;
                localizationSettings.LoadAllLocaleRecordsOnStartup = model.LocalizationSettings.LoadAllLocaleRecordsOnStartup;
                localizationSettings.LoadAllLocalizedPropertiesOnStartup = model.LocalizationSettings.LoadAllLocalizedPropertiesOnStartup;
                localizationSettings.LoadAllUrlRecordsOnStartup = model.LocalizationSettings.LoadAllUrlRecordsOnStartup;
                await _settingService.SaveSettingAsync(localizationSettings);

                //display default menu item
                var displayDefaultMenuItemSettings = await _settingService.LoadSettingAsync<DisplayDefaultMenuItemSettings>(storeScope);

                //we do not clear cache after each setting update.
                //this behavior can increase performance because cached settings will not be cleared 
                //and loaded from database after each update
                displayDefaultMenuItemSettings.DisplayHomepageMenuItem = model.DisplayDefaultMenuItemSettings.DisplayHomepageMenuItem;
                displayDefaultMenuItemSettings.DisplayCustomerInfoMenuItem = model.DisplayDefaultMenuItemSettings.DisplayCustomerInfoMenuItem;
                displayDefaultMenuItemSettings.DisplayContactUsMenuItem = model.DisplayDefaultMenuItemSettings.DisplayContactUsMenuItem;

                await _settingService.SaveSettingOverridablePerStoreAsync(displayDefaultMenuItemSettings, x => x.DisplayHomepageMenuItem, model.DisplayDefaultMenuItemSettings.DisplayHomepageMenuItem_OverrideForStore, storeScope, false);
                await _settingService.SaveSettingOverridablePerStoreAsync(displayDefaultMenuItemSettings, x => x.DisplayCustomerInfoMenuItem, model.DisplayDefaultMenuItemSettings.DisplayCustomerInfoMenuItem_OverrideForStore, storeScope, false);
                await _settingService.SaveSettingOverridablePerStoreAsync(displayDefaultMenuItemSettings, x => x.DisplayContactUsMenuItem, model.DisplayDefaultMenuItemSettings.DisplayContactUsMenuItem_OverrideForStore, storeScope, false);

                //now clear settings cache
                await _settingService.ClearCacheAsync();

                //display default footer item
                var displayDefaultFooterItemSettings = await _settingService.LoadSettingAsync<DisplayDefaultFooterItemSettings>(storeScope);

                //we do not clear cache after each setting update.
                //this behavior can increase performance because cached settings will not be cleared 
                //and loaded from database after each update
                displayDefaultFooterItemSettings.DisplaySitemapFooterItem = model.DisplayDefaultFooterItemSettings.DisplaySitemapFooterItem;
                displayDefaultFooterItemSettings.DisplayContactUsFooterItem = model.DisplayDefaultFooterItemSettings.DisplayContactUsFooterItem;
                displayDefaultFooterItemSettings.DisplayCustomerInfoFooterItem = model.DisplayDefaultFooterItemSettings.DisplayCustomerInfoFooterItem;

                await _settingService.SaveSettingOverridablePerStoreAsync(displayDefaultFooterItemSettings, x => x.DisplaySitemapFooterItem, model.DisplayDefaultFooterItemSettings.DisplaySitemapFooterItem_OverrideForStore, storeScope, false);
                await _settingService.SaveSettingOverridablePerStoreAsync(displayDefaultFooterItemSettings, x => x.DisplayContactUsFooterItem, model.DisplayDefaultFooterItemSettings.DisplayContactUsFooterItem_OverrideForStore, storeScope, false);
                await _settingService.SaveSettingOverridablePerStoreAsync(displayDefaultFooterItemSettings, x => x.DisplayCustomerInfoFooterItem, model.DisplayDefaultFooterItemSettings.DisplayCustomerInfoFooterItem_OverrideForStore, storeScope, false);

                //now clear settings cache
                await _settingService.ClearCacheAsync();

                //admin area
                var adminAreaSettings = await _settingService.LoadSettingAsync<AdminAreaSettings>(storeScope);

                //we do not clear cache after each setting update.
                //this behavior can increase performance because cached settings will not be cleared 
                //and loaded from database after each update
                adminAreaSettings.UseRichEditorInMessageTemplates = model.AdminAreaSettings.UseRichEditorInMessageTemplates;

                await _settingService.SaveSettingOverridablePerStoreAsync(adminAreaSettings, x => x.UseRichEditorInMessageTemplates, model.AdminAreaSettings.UseRichEditorInMessageTemplates_OverrideForStore, storeScope, false);

                //now clear settings cache
                await _settingService.ClearCacheAsync();

                //activity log
                await _customerActivityService.InsertActivityAsync("EditSettings", await _localizationService.GetResourceAsync("ActivityLog.EditSettings"));

                _notificationService.SuccessNotification(await _localizationService.GetResourceAsync("Admin.Configuration.Updated"));

                return RedirectToAction("GeneralCommon");
            }

            //prepare model
            model = await _settingModelFactory.PrepareGeneralCommonSettingsModelAsync(model);

            //if we got this far, something failed, redisplay form
            return View(model);
        }

        [HttpPost, ActionName("GeneralCommon")]
        [FormValueRequired("changeencryptionkey")]
        public virtual async Task<IActionResult> ChangeEncryptionKey(GeneralCommonSettingsModel model)
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManageSettings))
                return AccessDeniedView();

            var storeScope = await _storeContext.GetActiveStoreScopeConfigurationAsync();
            var securitySettings = await _settingService.LoadSettingAsync<SecuritySettings>(storeScope);

            try
            {
                if (model.SecuritySettings.EncryptionKey == null)
                    model.SecuritySettings.EncryptionKey = string.Empty;

                model.SecuritySettings.EncryptionKey = model.SecuritySettings.EncryptionKey.Trim();

                var newEncryptionPrivateKey = model.SecuritySettings.EncryptionKey;
                if (string.IsNullOrEmpty(newEncryptionPrivateKey) || newEncryptionPrivateKey.Length != 16)
                    throw new NopException(await _localizationService.GetResourceAsync("Admin.Configuration.Settings.GeneralCommon.EncryptionKey.TooShort"));

                var oldEncryptionPrivateKey = securitySettings.EncryptionKey;
                if (oldEncryptionPrivateKey == newEncryptionPrivateKey)
                    throw new NopException(await _localizationService.GetResourceAsync("Admin.Configuration.Settings.GeneralCommon.EncryptionKey.TheSame"));

                //update password information
                //optimization - load only passwords with PasswordFormat.Encrypted
                var customerPasswords = await _customerService.GetCustomerPasswordsAsync(passwordFormat: PasswordFormat.Encrypted);
                foreach (var customerPassword in customerPasswords)
                {
                    var decryptedPassword = _encryptionService.DecryptText(customerPassword.Password, oldEncryptionPrivateKey);
                    var encryptedPassword = _encryptionService.EncryptText(decryptedPassword, newEncryptionPrivateKey);

                    customerPassword.Password = encryptedPassword;
                    await _customerService.UpdateCustomerPasswordAsync(customerPassword);
                }

                securitySettings.EncryptionKey = newEncryptionPrivateKey;
                await _settingService.SaveSettingAsync(securitySettings);

                _notificationService.SuccessNotification(await _localizationService.GetResourceAsync("Admin.Configuration.Settings.GeneralCommon.EncryptionKey.Changed"));
            }
            catch (Exception exc)
            {
                await _notificationService.ErrorNotificationAsync(exc);
            }

            return RedirectToAction("GeneralCommon");
        }

        [HttpPost]
        public virtual async Task<IActionResult> UploadLocalePattern()
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManageSettings))
                return AccessDeniedView();

            try
            {
                _uploadService.UploadLocalePattern();
                _notificationService.SuccessNotification(await _localizationService.GetResourceAsync("Admin.Configuration.Settings.GeneralCommon.LocalePattern.SuccessUpload"));
            }
            catch (Exception exc)
            {
                await _notificationService.ErrorNotificationAsync(exc);
            }

            return RedirectToAction("GeneralCommon");
        }

        [HttpPost]
        public virtual async Task<IActionResult> UploadIcons(IFormFile iconsFile)
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManageSettings))
                return AccessDeniedView();

            try
            {
                if (iconsFile == null || iconsFile.Length == 0)
                {
                    _notificationService.ErrorNotification(await _localizationService.GetResourceAsync("Admin.Common.UploadFile"));
                    return RedirectToAction("GeneralCommon");
                }

                //load settings for a chosen store scope
                var storeScope = await _storeContext.GetActiveStoreScopeConfigurationAsync();
                var commonSettings = await _settingService.LoadSettingAsync<CommonSettings>(storeScope);

                switch (_fileProvider.GetFileExtension(iconsFile.FileName))
                {
                    case ".ico":
                        _uploadService.UploadFavicon(iconsFile);
                        commonSettings.FaviconAndAppIconsHeadCode = string.Format(NopCommonDefaults.SingleFaviconHeadLink, storeScope, iconsFile.FileName);

                        break;

                    case ".zip":
                        _uploadService.UploadIconsArchive(iconsFile);

                        var headCodePath = _fileProvider.GetAbsolutePath(string.Format(NopCommonDefaults.FaviconAndAppIconsPath, storeScope), NopCommonDefaults.HeadCodeFileName);
                        if (!_fileProvider.FileExists(headCodePath))
                            throw new Exception(string.Format(await _localizationService.GetResourceAsync("Admin.Configuration.Settings.GeneralCommon.FaviconAndAppIcons.MissingFile"), NopCommonDefaults.HeadCodeFileName));

                        using (var sr = new StreamReader(headCodePath))
                            commonSettings.FaviconAndAppIconsHeadCode = await sr.ReadToEndAsync();

                        break;

                    default:
                        throw new InvalidOperationException("File is not supported.");
                }

                await _settingService.SaveSettingOverridablePerStoreAsync(commonSettings, x => x.FaviconAndAppIconsHeadCode, true, storeScope);

                //delete old favicon icon if exist
                var oldFaviconIconPath = _fileProvider.GetAbsolutePath(string.Format(NopCommonDefaults.OldFaviconIconName, storeScope));
                if (_fileProvider.FileExists(oldFaviconIconPath))
                {
                    _fileProvider.DeleteFile(oldFaviconIconPath);
                }

                //activity log
                await _customerActivityService.InsertActivityAsync("UploadIcons", string.Format(await _localizationService.GetResourceAsync("ActivityLog.UploadNewIcons"), storeScope));
                _notificationService.SuccessNotification(await _localizationService.GetResourceAsync("Admin.Configuration.FaviconAndAppIcons.Uploaded"));
            }
            catch (Exception exc)
            {
                await _notificationService.ErrorNotificationAsync(exc);
            }

            return RedirectToAction("GeneralCommon");
        }

        public virtual async Task<IActionResult> AllSettings(string settingName)
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManageSettings))
                return AccessDeniedView();

            //prepare model
            var model = await _settingModelFactory.PrepareSettingSearchModelAsync(new SettingSearchModel { SearchSettingName = WebUtility.HtmlEncode(settingName) });

            return View(model);
        }

        [HttpPost]
        public virtual async Task<IActionResult> AllSettings(SettingSearchModel searchModel)
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManageSettings))
                return await AccessDeniedDataTablesJson();

            //prepare model
            var model = await _settingModelFactory.PrepareSettingListModelAsync(searchModel);

            return Json(model);
        }

        [HttpPost]
        public virtual async Task<IActionResult> SettingUpdate(SettingModel model)
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManageSettings))
                return AccessDeniedView();

            if (model.Name != null)
                model.Name = model.Name.Trim();

            if (model.Value != null)
                model.Value = model.Value.Trim();

            if (!ModelState.IsValid)
                return ErrorJson(ModelState.SerializeErrors());

            //try to get a setting with the specified id
            var setting = await _settingService.GetSettingByIdAsync(model.Id)
                ?? throw new ArgumentException("No setting found with the specified id");

            if (!setting.Name.Equals(model.Name, StringComparison.InvariantCultureIgnoreCase))
            {
                //setting name has been changed
                await _settingService.DeleteSettingAsync(setting);
            }

            await _settingService.SetSettingAsync(model.Name, model.Value, setting.StoreId);

            //activity log
            await _customerActivityService.InsertActivityAsync("EditSettings", await _localizationService.GetResourceAsync("ActivityLog.EditSettings"), setting);

            return new NullJsonResult();
        }

        [HttpPost]
        public virtual async Task<IActionResult> SettingAdd(SettingModel model)
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManageSettings))
                return AccessDeniedView();

            if (model.Name != null)
                model.Name = model.Name.Trim();

            if (model.Value != null)
                model.Value = model.Value.Trim();

            if (!ModelState.IsValid)
                return ErrorJson(ModelState.SerializeErrors());

            var storeId = model.StoreId;
            await _settingService.SetSettingAsync(model.Name, model.Value, storeId);

            //activity log
            await _customerActivityService.InsertActivityAsync("AddNewSetting",
                string.Format(await _localizationService.GetResourceAsync("ActivityLog.AddNewSetting"), model.Name),
                await _settingService.GetSettingAsync(model.Name, storeId));

            return Json(new { Result = true });
        }

        [HttpPost]
        public virtual async Task<IActionResult> SettingDelete(int id)
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManageSettings))
                return AccessDeniedView();

            //try to get a setting with the specified id
            var setting = await _settingService.GetSettingByIdAsync(id)
                ?? throw new ArgumentException("No setting found with the specified id", nameof(id));

            await _settingService.DeleteSettingAsync(setting);

            //activity log
            await _customerActivityService.InsertActivityAsync("DeleteSetting",
                string.Format(await _localizationService.GetResourceAsync("ActivityLog.DeleteSetting"), setting.Name), setting);

            return new NullJsonResult();
        }

        //action displaying notification (warning) to a store owner about a lot of traffic 
        //between the distributed cache server and the application when LoadAllLocaleRecordsOnStartup setting is set
        public async Task<IActionResult> DistributedCacheHighTrafficWarning(bool loadAllLocaleRecordsOnStartup)
        {
            //LoadAllLocaleRecordsOnStartup is set and distributed cache is used, so display warning
            if (_appSettings.Get<DistributedCacheConfig>().Enabled && _appSettings.Get<DistributedCacheConfig>().DistributedCacheType != DistributedCacheType.Memory && loadAllLocaleRecordsOnStartup)
            {
                return Json(new
                {
                    Result = await _localizationService
                        .GetResourceAsync("Admin.Configuration.Settings.GeneralCommon.LoadAllLocaleRecordsOnStartup.Warning")
                });
            }

            return Json(new { Result = string.Empty });
        }

        //Action that displays a notification (warning) to the store owner about the absence of active authentication providers
        public async Task<IActionResult> ForceMultifactorAuthenticationWarning(bool forceMultifactorAuthentication)
        {
            //ForceMultifactorAuthentication is set and the store haven't active Authentication provider , so display warning
            if (forceMultifactorAuthentication && !await _multiFactorAuthenticationPluginManager.HasActivePluginsAsync())
            {
                return Json(new
                {
                    Result = await _localizationService
                        .GetResourceAsync("Admin.Configuration.Settings.CustomerUser.ForceMultifactorAuthentication.Warning")
                });
            }

            return Json(new { Result = string.Empty });
        }

        //Action that displays a notification (warning) to the store owner about the need to restart the application after changing the setting
        public async Task<IActionResult> SeoFriendlyUrlsForLanguagesEnabledWarning(bool seoFriendlyUrlsForLanguagesEnabled)
        {
            //load settings for a chosen store scope
            var storeScope = await _storeContext.GetActiveStoreScopeConfigurationAsync();
            var localizationSettings = await _settingService.LoadSettingAsync<LocalizationSettings>(storeScope);

            if (seoFriendlyUrlsForLanguagesEnabled != localizationSettings.SeoFriendlyUrlsForLanguagesEnabled)
            {
                return Json(new
                {
                    Result = await _localizationService
                        .GetResourceAsync("Admin.Configuration.Settings.GeneralCommon.SeoFriendlyUrlsForLanguagesEnabled.Warning")
                });
            }

            return Json(new { Result = string.Empty });
        }

        #endregion
    }
}