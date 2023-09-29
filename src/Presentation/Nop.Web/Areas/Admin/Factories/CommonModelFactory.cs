﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Net.Http.Headers;
using Nop.Core;
using Nop.Core.Caching;
using Nop.Core.Configuration;
using Nop.Core.Domain.Common;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Directory;
using Nop.Core.Domain.Security;
using Nop.Core.Events;
using Nop.Core.Infrastructure;
using Nop.Data;
using Nop.Services.Authentication.External;
using Nop.Services.Authentication.MultiFactor;
using Nop.Services.Cms;
using Nop.Services.Common;
using Nop.Services.Customers;
using Nop.Services.Directory;
using Nop.Services.Events;
using Nop.Services.Helpers;
using Nop.Services.Localization;
using Nop.Services.Plugins;
using Nop.Services.Seo;
using Nop.Services.Stores;
using Nop.Web.Areas.Admin.Infrastructure.Mapper.Extensions;
using Nop.Web.Areas.Admin.Models.Common;
using Nop.Web.Areas.Admin.Models.Localization;
using Nop.Web.Framework.Models.Extensions;
using Nop.Web.Framework.Security;

namespace Nop.Web.Areas.Admin.Factories
{
    /// <summary>
    /// Represents common models factory implementation
    /// </summary>
    public partial class CommonModelFactory : ICommonModelFactory
    {
        #region Fields

        private readonly AdminAreaSettings _adminAreaSettings;
        private readonly AppSettings _appSettings;
        private readonly CommonSettings _commonSettings;
        private readonly IActionContextAccessor _actionContextAccessor;
        private readonly IAuthenticationPluginManager _authenticationPluginManager;
        private readonly IBaseAdminModelFactory _baseAdminModelFactory;
        private readonly ICustomerService _customerService;
        private readonly IEventPublisher _eventPublisher;
        private readonly INopDataProvider _dataProvider;
        private readonly IDateTimeHelper _dateTimeHelper;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILanguageService _languageService;
        private readonly ILocalizationService _localizationService;
        private readonly IMaintenanceService _maintenanceService;
        private readonly IMultiFactorAuthenticationPluginManager _multiFactorAuthenticationPluginManager;
        private readonly INopFileProvider _fileProvider;
        private readonly IPluginService _pluginService;
        private readonly ISearchTermService _searchTermService;
        private readonly IServiceCollection _serviceCollection;
        private readonly IStaticCacheManager _staticCacheManager;
        private readonly IStoreContext _storeContext;
        private readonly IStoreService _storeService;
        private readonly IUrlHelperFactory _urlHelperFactory;
        private readonly IUrlRecordService _urlRecordService;
        private readonly IWebHelper _webHelper;
        private readonly IWidgetPluginManager _widgetPluginManager;
        private readonly IWorkContext _workContext;
        private readonly ProxySettings _proxySettings;

        #endregion

        #region Ctor

        public CommonModelFactory(AdminAreaSettings adminAreaSettings,
            AppSettings appSettings,
            CommonSettings commonSettings,
            IActionContextAccessor actionContextAccessor,
            IAuthenticationPluginManager authenticationPluginManager,
            IBaseAdminModelFactory baseAdminModelFactory,
            ICustomerService customerService,
            IEventPublisher eventPublisher,
            INopDataProvider dataProvider,
            IDateTimeHelper dateTimeHelper,
            INopFileProvider fileProvider,
            IHttpContextAccessor httpContextAccessor,
            ILanguageService languageService,
            ILocalizationService localizationService,
            IMaintenanceService maintenanceService,
            IMultiFactorAuthenticationPluginManager multiFactorAuthenticationPluginManager,
            IPluginService pluginService,
            ISearchTermService searchTermService,
            IServiceCollection serviceCollection,
            IStaticCacheManager staticCacheManager,
            IStoreContext storeContext,
            IStoreService storeService,
            IUrlHelperFactory urlHelperFactory,
            IUrlRecordService urlRecordService,
            IWebHelper webHelper,
            IWidgetPluginManager widgetPluginManager,
            IWorkContext workContext,
            ProxySettings proxySettings)
        {
            _adminAreaSettings = adminAreaSettings;
            _appSettings = appSettings;
            _commonSettings = commonSettings;
            _actionContextAccessor = actionContextAccessor;
            _authenticationPluginManager = authenticationPluginManager;
            _baseAdminModelFactory = baseAdminModelFactory;
            _customerService = customerService;
            _eventPublisher = eventPublisher;
            _dataProvider = dataProvider;
            _dateTimeHelper = dateTimeHelper;
            _httpContextAccessor = httpContextAccessor;
            _languageService = languageService;
            _localizationService = localizationService;
            _maintenanceService = maintenanceService;
            _multiFactorAuthenticationPluginManager = multiFactorAuthenticationPluginManager;
            _fileProvider = fileProvider;
            _pluginService = pluginService;
            _searchTermService = searchTermService;
            _serviceCollection = serviceCollection;
            _staticCacheManager = staticCacheManager;
            _storeContext = storeContext;
            _storeService = storeService;
            _urlHelperFactory = urlHelperFactory;
            _urlRecordService = urlRecordService;
            _webHelper = webHelper;
            _widgetPluginManager = widgetPluginManager;
            _workContext = workContext;
            _proxySettings = proxySettings;
        }

        #endregion

        #region Utilities

        /// <summary>
        /// Prepare store URL warning model
        /// </summary>
        /// <param name="models">List of system warning models</param>
        /// <returns>A task that represents the asynchronous operation</returns>
        protected virtual async Task PrepareStoreUrlWarningModelAsync(IList<SystemWarningModel> models)
        {
            if (models == null)
                throw new ArgumentNullException(nameof(models));

            //check whether current store URL matches the store configured URL
            var store = await _storeContext.GetCurrentStoreAsync();
            var currentStoreUrl = store.Url;
            if (!string.IsNullOrEmpty(currentStoreUrl) &&
                (currentStoreUrl.Equals(_webHelper.GetStoreLocation(false), StringComparison.InvariantCultureIgnoreCase) ||
                currentStoreUrl.Equals(_webHelper.GetStoreLocation(true), StringComparison.InvariantCultureIgnoreCase)))
            {
                models.Add(new SystemWarningModel
                {
                    Level = SystemWarningLevel.Pass,
                    Text = await _localizationService.GetResourceAsync("Admin.System.Warnings.URL.Match")
                });
                return;
            }

            models.Add(new SystemWarningModel
            {
                Level = SystemWarningLevel.Fail,
                Text = string.Format(await _localizationService.GetResourceAsync("Admin.System.Warnings.URL.NoMatch"),
                    currentStoreUrl, _webHelper.GetStoreLocation(false))
            });
        }

        /// <summary>
        /// Prepare plugins warning model
        /// </summary>
        /// <param name="models">List of system warning models</param>
        /// <returns>A task that represents the asynchronous operation</returns>
        protected virtual async Task PreparePluginsWarningModelAsync(IList<SystemWarningModel> models)
        {
            if (models == null)
                throw new ArgumentNullException(nameof(models));

            //check whether there are incompatible plugins
            foreach (var pluginName in _pluginService.GetIncompatiblePlugins())
            {
                models.Add(new SystemWarningModel
                {
                    Level = SystemWarningLevel.Warning,
                    Text = string.Format(await _localizationService.GetResourceAsync("Admin.System.Warnings.PluginNotLoaded"), pluginName)
                });
            }

            //check whether there are any collision of loaded assembly
            foreach (var assembly in _pluginService.GetAssemblyCollisions())
            {
                //get plugin references message
                var message = (await assembly.Collisions
                    .SelectAwait(async item => string.Format(await _localizationService
                        .GetResourceAsync("Admin.System.Warnings.PluginRequiredAssembly"), item.PluginName, item.AssemblyName))
                    .AggregateAsync("", (curent, all) => all + ", " + curent)).TrimEnd(',', ' ');

                models.Add(new SystemWarningModel
                {
                    Level = SystemWarningLevel.Warning,
                    Text = string.Format(await _localizationService.GetResourceAsync("Admin.System.Warnings.AssemblyHasCollision"),
                        assembly.ShortName, assembly.AssemblyFullNameInMemory, message)
                });
            }
            
            //check whether there are different plugins which try to override the same interface
            var baseLibraries = new[] { "Nop.Core", "Nop.Data", "Nop.Services", "Nop.Web", "Nop.Web.Framework" };
            var overridenServices = _serviceCollection.Where(p =>
                    p.ServiceType.FullName != null &&
                    p.ServiceType.FullName.StartsWith("Nop.", StringComparison.InvariantCulture) &&
                    !p.ServiceType.FullName.StartsWith(
                        typeof(IConsumer<>).FullName?.Replace("~1", string.Empty) ?? string.Empty,
                        StringComparison.InvariantCulture)).Select(p =>
                    KeyValuePair.Create(p.ServiceType.FullName, p.ImplementationType?.Assembly.GetName().Name))
                .Where(p => baseLibraries.All(library =>
                    !p.Value?.StartsWith(library, StringComparison.InvariantCultureIgnoreCase) ?? false))
                .GroupBy(p => p.Key, p => p.Value)
                .Where(p => p.Count() > 1)
                .ToDictionary(p => p.Key, p => p.ToList());

            foreach (var overridenService in overridenServices)
            {
                var assemblies = overridenService.Value
                    .Aggregate("", (current, all) => all + ", " + current).TrimEnd(',', ' ');

                models.Add(new SystemWarningModel
                {
                    Level = SystemWarningLevel.Warning,
                    Text = string.Format(await _localizationService.GetResourceAsync("Admin.System.Warnings.PluginsOverrideSameService"), overridenService.Key, assemblies)
                });
            }
        }

        /// <summary>
        /// Prepare performance settings warning model
        /// </summary>
        /// <param name="models">List of system warning models</param>
        /// <returns>A task that represents the asynchronous operation</returns>
        protected virtual async Task PreparePerformanceSettingsWarningModelAsync(IList<SystemWarningModel> models)
        {
            if (models == null)
                throw new ArgumentNullException(nameof(models));

            //check whether "IgnoreStoreLimitations" setting disabled
            if (!_commonSettings.IgnoreStoreLimitations && (await _storeService.GetAllStoresAsync()).Count == 1)
            {
                models.Add(new SystemWarningModel
                {
                    Level = SystemWarningLevel.Recommendation,
                    Text = await _localizationService.GetResourceAsync("Admin.System.Warnings.Performance.IgnoreStoreLimitations")
                });
            }

            //check whether "IgnoreAcl" setting disabled
            if (!_commonSettings.IgnoreAcl)
            {
                models.Add(new SystemWarningModel
                {
                    Level = SystemWarningLevel.Recommendation,
                    Text = await _localizationService.GetResourceAsync("Admin.System.Warnings.Performance.IgnoreAcl")
                });
            }
        }

        /// <summary>
        /// Prepare file permissions warning model
        /// </summary>
        /// <param name="models">List of system warning models</param>
        /// <returns>A task that represents the asynchronous operation</returns>
        protected virtual async Task PrepareFilePermissionsWarningModelAsync(IList<SystemWarningModel> models)
        {
            if (models == null)
                throw new ArgumentNullException(nameof(models));

            var dirPermissionsOk = true;
            var dirsToCheck = _fileProvider.GetDirectoriesWrite();
            foreach (var dir in dirsToCheck)
            {
                if (_fileProvider.CheckPermissions(dir, false, true, true, false))
                    continue;

                models.Add(new SystemWarningModel
                {
                    Level = SystemWarningLevel.Warning,
                    Text = string.Format(await _localizationService.GetResourceAsync("Admin.System.Warnings.DirectoryPermission.Wrong"),
                        CurrentOSUser.FullName, dir)
                });
                dirPermissionsOk = false;
            }

            if (dirPermissionsOk)
            {
                models.Add(new SystemWarningModel
                {
                    Level = SystemWarningLevel.Pass,
                    Text = await _localizationService.GetResourceAsync("Admin.System.Warnings.DirectoryPermission.OK")
                });
            }

            var filePermissionsOk = true;
            var filesToCheck = _fileProvider.GetFilesWrite();
            foreach (var file in filesToCheck)
            {
                if (_fileProvider.CheckPermissions(file, false, true, true, true))
                    continue;

                models.Add(new SystemWarningModel
                {
                    Level = SystemWarningLevel.Warning,
                    Text = string.Format(await _localizationService.GetResourceAsync("Admin.System.Warnings.FilePermission.Wrong"),
                        CurrentOSUser.FullName, file)
                });
                filePermissionsOk = false;
            }

            if (filePermissionsOk)
            {
                models.Add(new SystemWarningModel
                {
                    Level = SystemWarningLevel.Pass,
                    Text = await _localizationService.GetResourceAsync("Admin.System.Warnings.FilePermission.OK")
                });
            }
        }

        /// <summary>
        /// Prepare backup file search model
        /// </summary>
        /// <param name="searchModel">Backup file search model</param>
        /// <returns>Backup file search model</returns>
        protected virtual BackupFileSearchModel PrepareBackupFileSearchModel(BackupFileSearchModel searchModel)
        {
            if (searchModel == null)
                throw new ArgumentNullException(nameof(searchModel));

            //prepare page parameters
            searchModel.SetGridPageSize();

            return searchModel;
        }

        /// <summary>
        /// Prepare plugins installed warning model
        /// </summary>
        /// <param name="models">List of system warning models</param>
        /// <returns>A task that represents the asynchronous operation</returns>
        protected virtual async Task PreparePluginsInstalledWarningModelAsync(List<SystemWarningModel> models)
        {
            var plugins = await _pluginService.GetPluginDescriptorsAsync<IPlugin>(LoadPluginsMode.NotInstalledOnly);

            var notInstalled = plugins.Select(p => p.FriendlyName).ToList();

            if (!notInstalled.Any())
                return;

            models.Add(new SystemWarningModel
            {
                Level = SystemWarningLevel.Warning,
                DontEncode = true,
                Text = $"{await _localizationService.GetResourceAsync("Admin.System.Warnings.PluginNotInstalled")}: {string.Join(", ", notInstalled)}. {await _localizationService.GetResourceAsync("Admin.System.Warnings.PluginNotInstalled.HelpText")}"
            });
        }

        /// <summary>
        /// Prepare plugins enabled warning model
        /// </summary>
        /// <param name="models">List of system warning models</param>
        /// <returns>A task that represents the asynchronous operation</returns>
        protected virtual async Task PreparePluginsEnabledWarningModelAsync(List<SystemWarningModel> models)
        {
            var plugins = await _pluginService.GetPluginsAsync<IPlugin>();

            var notEnabled = new List<string>();
            var notEnabledSystemNames = new List<string>();

            foreach (var plugin in plugins)
            {
                var isEnabled = true;

                switch (plugin)
                {
                    case IExternalAuthenticationMethod externalAuthenticationMethod:
                        isEnabled = _authenticationPluginManager.IsPluginActive(externalAuthenticationMethod);
                        break;

                    case IMultiFactorAuthenticationMethod multiFactorAuthenticationMethod:
                        isEnabled = _multiFactorAuthenticationPluginManager.IsPluginActive(multiFactorAuthenticationMethod);
                        break;

                    case IWidgetPlugin widgetPlugin:
                        isEnabled = _widgetPluginManager.IsPluginActive(widgetPlugin);
                        break;
                }

                if (isEnabled)
                    continue;

                notEnabled.Add(plugin.PluginDescriptor.FriendlyName);
                notEnabledSystemNames.Add(plugin.PluginDescriptor.SystemName);
            }

            if (notEnabled.Any())
            {
                //get URL helper
                var urlHelper = _urlHelperFactory.GetUrlHelper(_actionContextAccessor.ActionContext);

                models.Add(new SystemWarningModel
                {
                    Level = SystemWarningLevel.Warning,
                    DontEncode = true,

                    Text = $"{await _localizationService.GetResourceAsync("Admin.System.Warnings.PluginNotEnabled")}: {string.Join(", ", notEnabled)} (<a href=\"{urlHelper.Action("UninstallAndDeleteUnusedPlugins", "Plugin", new { names = notEnabledSystemNames.ToArray() })}\">{await _localizationService.GetResourceAsync("Admin.System.Warnings.PluginNotEnabled.AutoFixAndRestart")}</a>)"
                });
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Prepare system info model
        /// </summary>
        /// <param name="model">System info model</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the system info model
        /// </returns>
        public virtual async Task<SystemInfoModel> PrepareSystemInfoModelAsync(SystemInfoModel model)
        {
            if (model == null)
                throw new ArgumentNullException(nameof(model));

            model.NopVersion = NopVersion.FULL_VERSION;
            model.ServerTimeZone = TimeZoneInfo.Local.StandardName;
            model.ServerLocalTime = DateTime.Now;
            model.UtcTime = DateTime.UtcNow;
            model.CurrentUserTime = await _dateTimeHelper.ConvertToUserTimeAsync(DateTime.Now);
            model.HttpHost = _httpContextAccessor.HttpContext.Request.Headers[HeaderNames.Host];

            //ensure no exception is thrown
            try
            {
                model.OperatingSystem = Environment.OSVersion.VersionString;
                model.AspNetInfo = RuntimeInformation.FrameworkDescription;
                model.IsFullTrust = AppDomain.CurrentDomain.IsFullyTrusted;
            }
            catch
            {
                // ignored
            }

            foreach (var header in _httpContextAccessor.HttpContext.Request.Headers)
            {
                model.Headers.Add(new SystemInfoModel.HeaderModel
                {
                    Name = header.Key,
                    Value = header.Value
                });
            }

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                var loadedAssemblyModel = new SystemInfoModel.LoadedAssembly
                {
                    FullName = assembly.FullName
                };

                //ensure no exception is thrown
                try
                {
                    loadedAssemblyModel.Location = assembly.IsDynamic ? null : assembly.Location;
                    loadedAssemblyModel.IsDebug = assembly.GetCustomAttributes(typeof(DebuggableAttribute), false)
                        .FirstOrDefault() is DebuggableAttribute attribute && attribute.IsJITOptimizerDisabled;

                    //https://stackoverflow.com/questions/2050396/getting-the-date-of-a-net-assembly
                    //we use a simple method because the more Jeff Atwood's solution doesn't work anymore 
                    //more info at https://blog.codinghorror.com/determining-build-date-the-hard-way/
                    loadedAssemblyModel.BuildDate = assembly.IsDynamic ? null : (DateTime?)TimeZoneInfo.ConvertTimeFromUtc(_fileProvider.GetLastWriteTimeUtc(assembly.Location), TimeZoneInfo.Local);

                }
                catch
                {
                    // ignored
                }

                model.LoadedAssemblies.Add(loadedAssemblyModel);
            }


            var currentStaticCacheManagerName = _staticCacheManager.GetType().Name;

            if (_appSettings.Get<DistributedCacheConfig>().Enabled)
                currentStaticCacheManagerName +=
                    $"({await _localizationService.GetLocalizedEnumAsync(_appSettings.Get<DistributedCacheConfig>().DistributedCacheType)})";

            model.CurrentStaticCacheManager = currentStaticCacheManagerName;

            model.AzureBlobStorageEnabled = _appSettings.Get<AzureBlobConfig>().Enabled;

            return model;
        }

        /// <summary>
        /// Prepare system warning models
        /// </summary>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the list of system warning models
        /// </returns>
        public virtual async Task<IList<SystemWarningModel>> PrepareSystemWarningModelsAsync()
        {
            var models = new List<SystemWarningModel>();

            //store URL
            await PrepareStoreUrlWarningModelAsync(models);

            //plugins
            await PreparePluginsWarningModelAsync(models);

            //performance settings
            await PreparePerformanceSettingsWarningModelAsync(models);

            //validate write permissions (the same procedure like during installation)
            await PrepareFilePermissionsWarningModelAsync(models);

            //not active plugins
            await PreparePluginsEnabledWarningModelAsync(models);

            //not install plugins
            await PreparePluginsInstalledWarningModelAsync(models);

            //publish event
            var warningEvent = new SystemWarningCreatedEvent();
            await _eventPublisher.PublishAsync(warningEvent);
            //add another warnings (for example from plugins) 
            models.AddRange(warningEvent.SystemWarnings);

            return models;
        }

        /// <summary>
        /// Prepare maintenance model
        /// </summary>
        /// <param name="model">Maintenance model</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the maintenance model
        /// </returns>
        public virtual Task<MaintenanceModel> PrepareMaintenanceModelAsync(MaintenanceModel model)
        {
            if (model == null)
                throw new ArgumentNullException(nameof(model));

            model.DeleteGuests.EndDate = DateTime.UtcNow.AddDays(-7);
            model.DeleteAbandonedCarts.OlderThan = DateTime.UtcNow.AddDays(-182);

            model.DeleteAlreadySentQueuedEmails.EndDate = DateTime.UtcNow.AddDays(-7);

            model.BackupSupported = _dataProvider.BackupSupported;

            //prepare nested search model
            PrepareBackupFileSearchModel(model.BackupFileSearchModel);

            return Task.FromResult(model);
        }

        /// <summary>
        /// Prepare paged backup file list model
        /// </summary>
        /// <param name="searchModel">Backup file search model</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the backup file list model
        /// </returns>
        public virtual Task<BackupFileListModel> PrepareBackupFileListModelAsync(BackupFileSearchModel searchModel)
        {
            if (searchModel == null)
                throw new ArgumentNullException(nameof(searchModel));

            //get backup files
            var backupFiles = _maintenanceService.GetAllBackupFiles().ToPagedList(searchModel);

            //prepare list model
            var model = new BackupFileListModel().PrepareToGrid(searchModel, backupFiles, () =>
            {
                return backupFiles.Select(file => new BackupFileModel
                {
                    Name = _fileProvider.GetFileName(file),

                    //fill in additional values (not existing in the entity)
                    Length = $"{_fileProvider.FileLength(file) / 1024f / 1024f:F2} Mb",

                    Link = $"{_webHelper.GetStoreLocation()}db_backups/{_fileProvider.GetFileName(file)}"
                });
            });

            return Task.FromResult(model);
        }

        /// <summary>
        /// Prepare URL record search model
        /// </summary>
        /// <param name="searchModel">URL record search model</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the uRL record search model
        /// </returns>
        public virtual async Task<UrlRecordSearchModel> PrepareUrlRecordSearchModelAsync(UrlRecordSearchModel searchModel)
        {
            if (searchModel == null)
                throw new ArgumentNullException(nameof(searchModel));

            //prepare available languages
            //we insert 0 as 'Standard' language.
            //let's insert -1 for 'All' language selection.
            await _baseAdminModelFactory.PrepareLanguagesAsync(searchModel.AvailableLanguages,
                defaultItemText: await _localizationService.GetResourceAsync("Admin.System.SeNames.List.Language.Standard"));
            searchModel.AvailableLanguages.Insert(0,
                new SelectListItem { Text = await _localizationService.GetResourceAsync("Admin.Common.All"), Value = "-1" });
            searchModel.LanguageId = -1;

            //prepare "is active" filter (0 - all; 1 - active only; 2 - inactive only)
            searchModel.AvailableActiveOptions.Add(new SelectListItem
            {
                Value = "0",
                Text = await _localizationService.GetResourceAsync("Admin.System.SeNames.List.IsActive.All")
            });
            searchModel.AvailableActiveOptions.Add(new SelectListItem
            {
                Value = "1",
                Text = await _localizationService.GetResourceAsync("Admin.System.SeNames.List.IsActive.ActiveOnly")
            });
            searchModel.AvailableActiveOptions.Add(new SelectListItem
            {
                Value = "2",
                Text = await _localizationService.GetResourceAsync("Admin.System.SeNames.List.IsActive.InactiveOnly")
            });

            //prepare page parameters
            searchModel.SetGridPageSize();

            return searchModel;
        }

        /// <summary>
        /// Prepare paged URL record list model
        /// </summary>
        /// <param name="searchModel">URL record search model</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the uRL record list model
        /// </returns>
        public virtual async Task<UrlRecordListModel> PrepareUrlRecordListModelAsync(UrlRecordSearchModel searchModel)
        {
            if (searchModel == null)
                throw new ArgumentNullException(nameof(searchModel));

            var isActive = searchModel.IsActiveId == 0 ? null : (bool?)(searchModel.IsActiveId == 1);
            var languageId = searchModel.LanguageId < 0 ? null : (int?)(searchModel.LanguageId);

            //get URL records
            var urlRecords = await _urlRecordService.GetAllUrlRecordsAsync(slug: searchModel.SeName,
                languageId: languageId, isActive: isActive,
                pageIndex: searchModel.Page - 1, pageSize: searchModel.PageSize);

            //get URL helper
            var urlHelper = _urlHelperFactory.GetUrlHelper(_actionContextAccessor.ActionContext);

            //prepare list model
            var model = await new UrlRecordListModel().PrepareToGridAsync(searchModel, urlRecords, () =>
            {
                return urlRecords.SelectAwait(async urlRecord =>
                {
                    //fill in model values from the entity
                    var urlRecordModel = urlRecord.ToModel<UrlRecordModel>();

                    //fill in additional values (not existing in the entity)
                    urlRecordModel.Name = urlRecord.Slug;
                    urlRecordModel.Language = urlRecord.LanguageId == 0
                        ? await _localizationService.GetResourceAsync("Admin.System.SeNames.Language.Standard")
                        : (await _languageService.GetLanguageByIdAsync(urlRecord.LanguageId))?.Name ?? "Unknown";

                    //details URL
                    var detailsUrl = string.Empty;
                    var entityName = urlRecord.EntityName?.ToLowerInvariant() ?? string.Empty;
                    switch (entityName)
                    {
                        case "blogpost":
                            detailsUrl = urlHelper.Action("BlogPostEdit", "Blog", new { id = urlRecord.EntityId });
                            break;
                        case "product":
                            detailsUrl = urlHelper.Action("Edit", "Product", new { id = urlRecord.EntityId });
                            break;
                        case "newsitem":
                            detailsUrl = urlHelper.Action("NewsItemEdit", "News", new { id = urlRecord.EntityId });
                            break;
                        case "topic":
                            detailsUrl = urlHelper.Action("Edit", "Topic", new { id = urlRecord.EntityId });
                            break;
                    }

                    urlRecordModel.DetailsUrl = detailsUrl;

                    return urlRecordModel;
                });
            });
            return model;
        }

        /// <summary>
        /// Prepare language selector model
        /// </summary>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the language selector model
        /// </returns>
        public virtual async Task<LanguageSelectorModel> PrepareLanguageSelectorModelAsync()
        {
            var store = await _storeContext.GetCurrentStoreAsync();
            var model = new LanguageSelectorModel
            {
                CurrentLanguage = (await _workContext.GetWorkingLanguageAsync()).ToModel<LanguageModel>(),
                AvailableLanguages = (await _languageService
                    .GetAllLanguagesAsync(storeId: store.Id))
                    .Select(language => language.ToModel<LanguageModel>()).ToList()
            };

            return model;
        }

        /// <summary>
        /// Prepare popular search term search model
        /// </summary>
        /// <param name="searchModel">Popular search term search model</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the popular search term search model
        /// </returns>
        public virtual Task<PopularSearchTermSearchModel> PreparePopularSearchTermSearchModelAsync(PopularSearchTermSearchModel searchModel)
        {
            if (searchModel == null)
                throw new ArgumentNullException(nameof(searchModel));

            //prepare page parameters
            searchModel.SetGridPageSize(5);

            return Task.FromResult(searchModel);
        }

        /// <summary>
        /// Prepare paged popular search term list model
        /// </summary>
        /// <param name="searchModel">Popular search term search model</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the popular search term list model
        /// </returns>
        public virtual async Task<PopularSearchTermListModel> PreparePopularSearchTermListModelAsync(PopularSearchTermSearchModel searchModel)
        {
            if (searchModel == null)
                throw new ArgumentNullException(nameof(searchModel));

            //get popular search terms
            var searchTermRecordLines = await _searchTermService.GetStatsAsync(pageIndex: searchModel.Page - 1, pageSize: searchModel.PageSize);

            //prepare list model
            var model = new PopularSearchTermListModel().PrepareToGrid(searchModel, searchTermRecordLines, () =>
            {
                return searchTermRecordLines.Select(searchTerm => new PopularSearchTermModel
                {
                    Keyword = searchTerm.Keyword,
                    Count = searchTerm.Count
                });
            });

            return model;
        }

        /// <summary>
        /// Prepare common statistics model
        /// </summary>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the common statistics model
        /// </returns>
        public virtual async Task<CommonStatisticsModel> PrepareCommonStatisticsModelAsync()
        {
            var model = new CommonStatisticsModel();

            var customerRoleIds = new[] { (await _customerService.GetCustomerRoleBySystemNameAsync(NopCustomerDefaults.RegisteredRoleName)).Id };
            model.NumberOfCustomers = (await _customerService.GetAllCustomersAsync(customerRoleIds: customerRoleIds,
                pageIndex: 0, pageSize: 1, getOnlyTotalCount: true)).TotalCount;

            return model;
        }

        #endregion
    }
}