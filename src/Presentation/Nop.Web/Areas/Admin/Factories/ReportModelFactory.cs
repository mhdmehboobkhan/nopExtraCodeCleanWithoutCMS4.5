using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Core;
using Nop.Services;
using Nop.Services.Customers;
using Nop.Services.Directory;
using Nop.Services.Helpers;
using Nop.Services.Localization;
using Nop.Web.Areas.Admin.Models.Reports;
using Nop.Web.Framework.Models.Extensions;

namespace Nop.Web.Areas.Admin.Factories
{
    /// <summary>
    /// Represents the report model factory implementation
    /// </summary>
    public partial class ReportModelFactory : IReportModelFactory
    {
        #region Fields

        private readonly IBaseAdminModelFactory _baseAdminModelFactory;
        private readonly ICountryService _countryService;
        private readonly ICustomerReportService _customerReportService;
        private readonly ICustomerService _customerService;
        private readonly IDateTimeHelper _dateTimeHelper;
        private readonly ILocalizationService _localizationService;
        private readonly IWorkContext _workContext;

        #endregion

        #region Ctor

        public ReportModelFactory(IBaseAdminModelFactory baseAdminModelFactory,
            ICountryService countryService,
            ICustomerReportService customerReportService,
            ICustomerService customerService,
            IDateTimeHelper dateTimeHelper,
            ILocalizationService localizationService,
            IWorkContext workContext)
        {
            _baseAdminModelFactory = baseAdminModelFactory;
            _countryService = countryService;
            _customerReportService = customerReportService;
            _customerService = customerService;
            _dateTimeHelper = dateTimeHelper;
            _localizationService = localizationService;
            _workContext = workContext;
        }

        #endregion

        #region Utilities

        #endregion

        #region Methods

        #region Customer reports

        /// <summary>
        /// Prepare customer reports search model
        /// </summary>
        /// <param name="searchModel">Customer reports search model</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the customer reports search model
        /// </returns>
        public virtual async Task<CustomerReportsSearchModel> PrepareCustomerReportsSearchModelAsync(CustomerReportsSearchModel searchModel)
        {
            if (searchModel == null)
                throw new ArgumentNullException(nameof(searchModel));

            //prepare page parameters
            searchModel.SetGridPageSize();

            return searchModel;
        }

        /// <summary>
        /// Prepare paged registered customers report list model
        /// </summary>
        /// <param name="searchModel">Registered customers report search model</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the registered customers report list model
        /// </returns>
        public virtual async Task<RegisteredCustomersReportListModel> PrepareRegisteredCustomersReportListModelAsync(RegisteredCustomersReportSearchModel searchModel)
        {
            if (searchModel == null)
                throw new ArgumentNullException(nameof(searchModel));

            //get report items
            var reportItems = new List<RegisteredCustomersReportModel>
            {
                new RegisteredCustomersReportModel
                {
                    Period = await _localizationService.GetResourceAsync("Admin.Reports.Customers.RegisteredCustomers.Fields.Period.7days"),
                    Customers = await _customerReportService.GetRegisteredCustomersReportAsync(7)
                },
                new RegisteredCustomersReportModel
                {
                    Period = await _localizationService.GetResourceAsync("Admin.Reports.Customers.RegisteredCustomers.Fields.Period.14days"),
                    Customers = await _customerReportService.GetRegisteredCustomersReportAsync(14)
                },
                new RegisteredCustomersReportModel
                {
                    Period = await _localizationService.GetResourceAsync("Admin.Reports.Customers.RegisteredCustomers.Fields.Period.month"),
                    Customers = await _customerReportService.GetRegisteredCustomersReportAsync(30)
                },
                new RegisteredCustomersReportModel
                {
                    Period = await _localizationService.GetResourceAsync("Admin.Reports.Customers.RegisteredCustomers.Fields.Period.year"),
                    Customers = await _customerReportService.GetRegisteredCustomersReportAsync(365)
                }
            };

            var pagedList = reportItems.ToPagedList(searchModel);

            //prepare list model
            var model = new RegisteredCustomersReportListModel().PrepareToGrid(searchModel, pagedList, () => pagedList);

            return model;
        }

        #endregion

        #endregion
    }
}