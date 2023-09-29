using FluentMigrator;
using Nop.Data.Mapping;
using Nop.Data.Extensions;
using System.Collections.Generic;
using Nop.Core.Domain.Localization;
using System.Linq;
using Nop.Core.Domain.Configuration;
using Nop.Core.Domain.Messages;
using System;
using System.Threading.Tasks;
using Nop.Core.Domain.Logging;
using Nop.Core.Domain.Security;
using System.Data.SqlClient;
using Nop.Core.Domain.Customers;

namespace Nop.Data.Migrations.CustomMigrations
{
    [NopMigration("2022-07-15 03:55:08:9037680", "SchemaMigration2022_06", MigrationProcessType.NoMatter)]
    public class SchemaMigration2022_06 : MigrationBase
    {
        #region Fields

        private readonly IMigrationManager _migrationManager;
        private readonly INopDataProvider _dataProvider;

        #endregion

        #region Ctor

        public SchemaMigration2022_06(IMigrationManager migrationManager,
            INopDataProvider dataProvider)
        {
            _migrationManager = migrationManager;
            _dataProvider = dataProvider;
        }

        #endregion

        #region Utilities

        /// <summary>
        /// Run new table queires
        /// </summary>
        protected virtual void RunNewTableQueries()
        {
        }

        /// <summary>
        /// Run locales queires
        /// </summary>
        protected virtual void RunLocalesQueries()
        {
            var resources = new Dictionary<string, string>();
            var languages = _dataProvider.QueryAsync<Language>($"Select * from {nameof(Language)}").Result;

            var localeStringResource = _dataProvider.QueryAsync<int>
                ($"Select count(id) from {nameof(LocaleStringResource)} Where {nameof(LocaleStringResource.ResourceName)} = 'account.dashboard'").Result;
            if (localeStringResource.FirstOrDefault() == 0)
            {
                resources.Add("account.dashboard", "Dashboard");
            }
            localeStringResource = _dataProvider.QueryAsync<int>
                ($"Select count(id) from {nameof(LocaleStringResource)} Where {nameof(LocaleStringResource.ResourceName)} = 'Admin.Customers.Customers.Choose'").Result;
            if (localeStringResource.FirstOrDefault() == 0)
            {
                resources.Add("Admin.Customers.Customers.Choose", "Choose");
                resources.Add("Admin.Customers.Customers.Choose.Title", "Choose customer");
            }






            //insert new locale resources
            var locales = languages.SelectMany(language => resources.Select(resource => new LocaleStringResource
            {
                LanguageId = language.Id,
                ResourceName = resource.Key,
                ResourceValue = resource.Value
            })).ToList();

            foreach (var res in locales)
                _dataProvider.InsertEntityAsync(res);

            string[] updateresources = {
                "Update  LocaleStringResource set ResourceValue='Working days' where ResourceName='Admin.Test' and LanguageId = 1",
            };
            foreach (var res in updateresources)
            {
                _dataProvider.ExecuteNonQueryAsync(res);
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Collect the UP migration expressions
        /// </summary>
        public override void Up()
        {
            RunNewTableQueries();

            RunLocalesQueries();

            #region Message templates


            #endregion
        }

        public override void Down()
        {
            //add the downgrade logic if necessary 
        }

        #endregion
    }
}
