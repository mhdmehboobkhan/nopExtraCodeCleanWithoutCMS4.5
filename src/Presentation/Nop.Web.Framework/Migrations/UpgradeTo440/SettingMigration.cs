using FluentMigrator;
using Nop.Core.Domain.Common;
using Nop.Core.Domain.Configuration;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Seo;
using Nop.Core.Infrastructure;
using Nop.Data;
using Nop.Data.Migrations;
using Nop.Services.Configuration;
using Nop.Services.Seo;

namespace Nop.Web.Framework.Migrations.UpgradeTo440
{
    [NopMigration("2020-06-10 00:00:00", "4.40.0", UpdateMigrationType.Settings, MigrationProcessType.Update)]
    public class SettingMigration : MigrationBase
    {
        /// <summary>Collect the UP migration expressions</summary>
        public override void Up()
        {
            if (!DataSettingsManager.IsDatabaseInstalled())
                return;

            //do not use DI, because it produces exception on the installation process
            var settingRepository = EngineContext.Current.Resolve<IRepository<Setting>>();
            var settingService = EngineContext.Current.Resolve<ISettingService>();

            //#4904 External authentication errors logging
            var externalAuthenticationSettings = settingService.LoadSettingAsync<ExternalAuthenticationSettings>().Result;
            if (!settingService.SettingExistsAsync(externalAuthenticationSettings, settings => settings.LogErrors).Result)
            {
                externalAuthenticationSettings.LogErrors = false;
                settingService.SaveSettingAsync(externalAuthenticationSettings, settings => settings.LogErrors).Wait();
            }

            var multiFactorAuthenticationSettings = settingService.LoadSettingAsync<MultiFactorAuthenticationSettings>().Result;
            if (!settingService.SettingExistsAsync(multiFactorAuthenticationSettings, settings => settings.ForceMultifactorAuthentication).Result)
            {
                multiFactorAuthenticationSettings.ForceMultifactorAuthentication = false;

                settingService.SaveSettingAsync(multiFactorAuthenticationSettings, settings => settings.ForceMultifactorAuthentication).Wait();
            }

            //#5102 Delete Full-text settings
            settingRepository
                .DeleteAsync(setting => setting.Name == "commonsettings.usefulltextsearch" || setting.Name == "commonsettings.fulltextmode")
                .Wait();

            //#4196
            settingRepository
                .DeleteAsync(setting => setting.Name == "commonsettings.scheduletaskruntimeout" ||
                    setting.Name == "commonsettings.staticfilescachecontrol" ||
                    setting.Name == "commonsettings.supportpreviousnopcommerceversions" ||
                    setting.Name == "securitysettings.pluginstaticfileextensionsBlacklist")
                .Wait();

            //#5384
            var seoSettings = settingService.LoadSettingAsync<SeoSettings>().Result;
            foreach (var slug in NopSeoDefaults.ReservedUrlRecordSlugs)
            {
                if (!seoSettings.ReservedUrlRecordSlugs.Contains(slug))
                    seoSettings.ReservedUrlRecordSlugs.Add(slug);
            }
            settingService.SaveSettingAsync(seoSettings, settings => seoSettings.ReservedUrlRecordSlugs).Wait();

            //#3015
            if (!settingService.SettingExistsAsync(seoSettings, settings => settings.HomepageTitle).Result)
            {
                seoSettings.HomepageTitle = seoSettings.DefaultTitle;
                settingService.SaveSettingAsync(seoSettings, settings => settings.HomepageTitle).Wait();
            }

            if (!settingService.SettingExistsAsync(seoSettings, settings => settings.HomepageDescription).Result)
            {
                seoSettings.HomepageDescription = "Your home page description";
                settingService.SaveSettingAsync(seoSettings, settings => settings.HomepageDescription).Wait();
            }

            //#5210
            var adminAreaSettings = settingService.LoadSettingAsync<AdminAreaSettings>().Result;
            if (!settingService.SettingExistsAsync(adminAreaSettings, settings => settings.ShowDocumentationReferenceLinks).Result)
            {
                adminAreaSettings.ShowDocumentationReferenceLinks = true;
                settingService.SaveSettingAsync(adminAreaSettings, settings => settings.ShowDocumentationReferenceLinks).Wait();
            }

            //#5482
            settingService.SetSettingAsync("avalarataxsettings.gettaxratebyaddressonly", true).Wait();
            settingService.SetSettingAsync("avalarataxsettings.taxratebyaddresscachetime", 480).Wait();
        }

        public override void Down()
        {
            //add the downgrade logic if necessary 
        }
    }
}