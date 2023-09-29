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
using Nop.Core.Domain.Directory;
using Nop.Core.Domain.Common;

namespace Nop.Data.Migrations.CustomMigrations
{
    [NopMigration("2022-07-25 02:00:08:9037680", "SchemaMigration2022_07_Columns", MigrationProcessType.NoMatter)]
    public class SchemaMigration2022_07_Columns : MigrationBase
    {
        #region Fields

        private readonly IMigrationManager _migrationManager;
        private readonly INopDataProvider _dataProvider;

        #endregion

        #region Ctor

        public SchemaMigration2022_07_Columns(IMigrationManager migrationManager,
            INopDataProvider dataProvider)
        {
            _migrationManager = migrationManager;
            _dataProvider = dataProvider;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Collect the UP migration expressions
        /// </summary>
        public override void Up()
        {
            //add new column

        }

        public override void Down()
        {
            //add the downgrade logic if necessary 
        }

        #endregion
    }
}
