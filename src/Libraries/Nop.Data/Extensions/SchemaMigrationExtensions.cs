using System;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Threading.Tasks;
using FluentMigrator.Builders.Alter.Table;
using FluentMigrator.Builders.Create.Table;
using Nop.Core;
using Nop.Data.Mapping;

namespace Nop.Data.Extensions
{
    /// <summary>
    /// Schema migration extensions
    /// </summary>
    public static class SchemaMigrationExtensions
    {
        /// <summary>
        /// Run the sql query directly
        /// </summary>
        /// <param name="query">The query to run</param>
        public static void RunSqlQuery(string query, string connectionString)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                using (var command = new SqlCommand(query, connection))
                {
                    try
                    {
                        command.ExecuteNonQuery();
                    }
                    catch (Exception ex)
                    {
                        throw;
                    }
                }
                connection.Close();
            }
        }
    }
}