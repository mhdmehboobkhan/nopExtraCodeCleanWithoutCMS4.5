using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Core;
using Nop.Core.Caching;
using Nop.Services.Authentication.External;
using Nop.Services.Authentication.MultiFactor;
using Nop.Services.Cms;
using Nop.Services.Localization;
using Nop.Services.Plugins;
using Nop.Web.Areas.Admin.Infrastructure.Mapper.Extensions;
using Nop.Web.Areas.Admin.Models.Plugins;
using Nop.Web.Framework.Factories;
using Nop.Web.Framework.Models.Extensions;

namespace Nop.Web.Areas.Admin.Factories
{
    /// <summary>
    /// Represents the plugin model factory implementation
    /// </summary>
    public partial class PluginModelFactory : IPluginModelFactory
    {
        #region Fields

        private readonly IAclSupportedModelFactory _aclSupportedModelFactory;
        private readonly IAuthenticationPluginManager _authenticationPluginManager;
        private readonly IBaseAdminModelFactory _baseAdminModelFactory;
        private readonly ILocalizationService _localizationService;
        private readonly ILocalizedModelFactory _localizedModelFactory;
        private readonly IMultiFactorAuthenticationPluginManager _multiFactorAuthenticationPluginManager;
        private readonly IPluginService _pluginService;
        private readonly IStaticCacheManager _staticCacheManager;
        private readonly IStoreMappingSupportedModelFactory _storeMappingSupportedModelFactory;
        private readonly IWidgetPluginManager _widgetPluginManager;
        private readonly IWorkContext _workContext;

        #endregion

        #region Ctor

        public PluginModelFactory(IAclSupportedModelFactory aclSupportedModelFactory,
            IAuthenticationPluginManager authenticationPluginManager,
            IBaseAdminModelFactory baseAdminModelFactory,
            ILocalizationService localizationService,
            IMultiFactorAuthenticationPluginManager multiFactorAuthenticationPluginManager,
            ILocalizedModelFactory localizedModelFactory,
            IPluginService pluginService,
            IStaticCacheManager staticCacheManager,
            IStoreMappingSupportedModelFactory storeMappingSupportedModelFactory,
            IWidgetPluginManager widgetPluginManager,
            IWorkContext workContext)
        {
            _aclSupportedModelFactory = aclSupportedModelFactory;
            _authenticationPluginManager = authenticationPluginManager;
            _baseAdminModelFactory = baseAdminModelFactory;
            _localizationService = localizationService;
            _localizedModelFactory = localizedModelFactory;
            _multiFactorAuthenticationPluginManager = multiFactorAuthenticationPluginManager;
            _pluginService = pluginService;
            _staticCacheManager = staticCacheManager;
            _storeMappingSupportedModelFactory = storeMappingSupportedModelFactory;
            _widgetPluginManager = widgetPluginManager;
            _workContext = workContext;
        }

        #endregion

        #region Utilities

        /// <summary>
        /// Prepare plugin model properties of the installed plugin
        /// </summary>
        /// <param name="model">Plugin model</param>
        /// <param name="plugin">Plugin</param>
        protected virtual void PrepareInstalledPluginModel(PluginModel model, IPlugin plugin)
        {
            if (model == null)
                throw new ArgumentNullException(nameof(model));

            if (plugin == null)
                throw new ArgumentNullException(nameof(plugin));

            //prepare configuration URL
            model.ConfigurationUrl = plugin.GetConfigurationPageUrl();

            //prepare enabled/disabled (only for some plugin types)
            model.CanChangeEnabled = true;
            switch (plugin)
            {
                case IExternalAuthenticationMethod externalAuthenticationMethod:
                    model.IsEnabled = _authenticationPluginManager.IsPluginActive(externalAuthenticationMethod);
                    break;

                case IMultiFactorAuthenticationMethod multiFactorAuthenticationMethod:
                    model.IsEnabled = _multiFactorAuthenticationPluginManager.IsPluginActive(multiFactorAuthenticationMethod);
                    break;

                case IWidgetPlugin widgetPlugin:
                    model.IsEnabled = _widgetPluginManager.IsPluginActive(widgetPlugin);
                    break;

                default:
                    model.CanChangeEnabled = false;
                    break;
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Prepare plugin search model
        /// </summary>
        /// <param name="searchModel">Plugin search model</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the plugin search model
        /// </returns>
        public virtual async Task<PluginSearchModel> PreparePluginSearchModelAsync(PluginSearchModel searchModel)
        {
            if (searchModel == null)
                throw new ArgumentNullException(nameof(searchModel));

            //prepare available load plugin modes
            await _baseAdminModelFactory.PrepareLoadPluginModesAsync(searchModel.AvailableLoadModes, false);

            //prepare available groups
            await _baseAdminModelFactory.PreparePluginGroupsAsync(searchModel.AvailableGroups);

            //prepare page parameters
            searchModel.SetGridPageSize();

            searchModel.NeedToRestart = _pluginService.IsRestartRequired();

            return searchModel;
        }

        /// <summary>
        /// Prepare paged plugin list model
        /// </summary>
        /// <param name="searchModel">Plugin search model</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the plugin list model
        /// </returns>
        public virtual async Task<PluginListModel> PreparePluginListModelAsync(PluginSearchModel searchModel)
        {
            if (searchModel == null)
                throw new ArgumentNullException(nameof(searchModel));

            //get parameters to filter plugins
            var group = string.IsNullOrEmpty(searchModel.SearchGroup) || searchModel.SearchGroup.Equals("0") ? null : searchModel.SearchGroup;
            var loadMode = (LoadPluginsMode)searchModel.SearchLoadModeId;
            var friendlyName = string.IsNullOrEmpty(searchModel.SearchFriendlyName) ? null : searchModel.SearchFriendlyName;
            var author = string.IsNullOrEmpty(searchModel.SearchAuthor) ? null : searchModel.SearchAuthor;

            //filter visible plugins
            var plugins = (await _pluginService.GetPluginDescriptorsAsync<IPlugin>(group: group, loadMode: loadMode, friendlyName: friendlyName, author: author))
                .Where(p => p.ShowInPluginsList)
                .OrderBy(plugin => plugin.Group).ToList()
                .ToPagedList(searchModel);

            //prepare list model
            var model = await new PluginListModel().PrepareToGridAsync(searchModel, plugins, () =>
            {
                return plugins.SelectAwait(async pluginDescriptor =>
                {
                    //fill in model values from the entity
                    var pluginModel = pluginDescriptor.ToPluginModel<PluginModel>();

                    //fill in additional values (not existing in the entity)
                    pluginModel.LogoUrl = await _pluginService.GetPluginLogoUrlAsync(pluginDescriptor);

                    if (pluginDescriptor.Installed)
                        PrepareInstalledPluginModel(pluginModel, pluginDescriptor.Instance<IPlugin>());

                    return pluginModel;
                });
            });

            return model;
        }

        /// <summary>
        /// Prepare plugin model
        /// </summary>
        /// <param name="model">Plugin model</param>
        /// <param name="pluginDescriptor">Plugin descriptor</param>
        /// <param name="excludeProperties">Whether to exclude populating of some properties of model</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the plugin model
        /// </returns>
        public virtual async Task<PluginModel> PreparePluginModelAsync(PluginModel model, PluginDescriptor pluginDescriptor, bool excludeProperties = false)
        {
            Func<PluginLocalizedModel, int, Task> localizedModelConfiguration = null;

            if (pluginDescriptor != null)
            {
                //fill in model values from the entity
                model ??= pluginDescriptor.ToPluginModel(model);

                model.LogoUrl = await _pluginService.GetPluginLogoUrlAsync(pluginDescriptor);
                model.SelectedStoreIds = pluginDescriptor.LimitedToStores;
                model.SelectedCustomerRoleIds = pluginDescriptor.LimitedToCustomerRoles;
                var plugin = pluginDescriptor.Instance<IPlugin>();
                if (pluginDescriptor.Installed)
                    PrepareInstalledPluginModel(model, plugin);

                //define localized model configuration action
                localizedModelConfiguration = async (locale, languageId) =>
                {
                    locale.FriendlyName = await _localizationService.GetLocalizedFriendlyNameAsync(plugin, languageId, false);
                };
            }

            //prepare localized models
            if (!excludeProperties)
                model.Locales = await _localizedModelFactory.PrepareLocalizedModelsAsync(localizedModelConfiguration);

            //prepare model customer roles
            await _aclSupportedModelFactory.PrepareModelCustomerRolesAsync(model);

            //prepare available stores
            await _storeMappingSupportedModelFactory.PrepareModelStoresAsync(model);

            return model;
        }

        /// <summary>
        /// Prepare plugin models for admin navigation
        /// </summary>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the list of models
        /// </returns>
        public virtual async Task<IList<AdminNavigationPluginModel>> PrepareAdminNavigationPluginModelsAsync()
        {
            var customer = await _workContext.GetCurrentCustomerAsync();
            var cacheKey = _staticCacheManager.PrepareKeyForDefaultCache(NopPluginDefaults.AdminNavigationPluginsCacheKey, customer);
            return await _staticCacheManager.GetAsync(cacheKey, async () =>
            {
                //get installed plugins
                return (await _pluginService.GetPluginDescriptorsAsync<IPlugin>(LoadPluginsMode.InstalledOnly, customer))
                    .Where(plugin => plugin.ShowInPluginsList)
                    .Select(plugin => new AdminNavigationPluginModel
                    {
                        FriendlyName = plugin.FriendlyName,
                        ConfigurationUrl = plugin.Instance<IPlugin>().GetConfigurationPageUrl()
                    }).Where(model => !string.IsNullOrEmpty(model.ConfigurationUrl)).ToList();
            });
        }

        #endregion
    }
}