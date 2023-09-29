using System;
using System.Linq;
using System.Threading.Tasks;
using Nop.Core;
using Nop.Core.Domain.Customers;
using Nop.Data;
using Nop.Data.Extensions;
using Nop.Services.Helpers;

namespace Nop.Services.Customers
{
    /// <summary>
    /// Customer report service
    /// </summary>
    public partial class CustomerReportService : ICustomerReportService
    {
        #region Fields

        private readonly ICustomerService _customerService;
        private readonly IDateTimeHelper _dateTimeHelper;
        private readonly IRepository<Customer> _customerRepository;

        #endregion

        #region Ctor

        public CustomerReportService(ICustomerService customerService,
            IDateTimeHelper dateTimeHelper,
            IRepository<Customer> customerRepository)
        {
            _customerService = customerService;
            _dateTimeHelper = dateTimeHelper;
            _customerRepository = customerRepository;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Gets a report of customers registered in the last days
        /// </summary>
        /// <param name="days">Customers registered in the last days</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the number of registered customers
        /// </returns>
        public virtual async Task<int> GetRegisteredCustomersReportAsync(int days)
        {
            var date = (await _dateTimeHelper.ConvertToUserTimeAsync(DateTime.Now)).AddDays(-days);

            var registeredCustomerRole = await _customerService.GetCustomerRoleBySystemNameAsync(NopCustomerDefaults.RegisteredRoleName);
            if (registeredCustomerRole == null)
                return 0;

            return (await _customerService.GetAllCustomersAsync(
                date,
                customerRoleIds: new[] { registeredCustomerRole.Id })).Count;
        }

        #endregion
    }
}