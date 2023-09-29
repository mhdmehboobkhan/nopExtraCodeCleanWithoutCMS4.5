using Nop.Core.Domain.Customers;
using Nop.Plugin.Misc.ZsWebApi.Models;
using Nop.Plugin.Misc.ZsWebApi.Models.Customer;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Nop.Plugin.Misc.ZsWebApi.Factories
{
    /// <summary>
    /// Represents the interface of the customer model factory
    /// </summary>
    public partial interface ICustomerModelFactoryApi
    {
        /// <summary>
        /// Get token by customer id in the pay load data
        /// </summary>
        /// <param name="customerId"></param>
        /// <returns></returns>
        string GetToken(int customerId);

        /// <summary>
        /// Get Random Number
        /// </summary>
        /// <returns>Random number</returns>
        int GetRandomNumber();

        /// <summary>
        /// Get alphanumeric random string
        /// </summary>
        /// <param name="length"></param>
        /// <returns></returns>
        string GetRandomString(int length = 8);

        /// <summary>
        /// Prepare the customer login model
        /// </summary>
        /// <param name="model">Customer login model</param>
        /// <param name="customer">Customer</param>
        /// <param name="excludeProperties">Whether to exclude populating of model properties from the entity</param>
        /// <param name="overrideCustomCustomerAttributesXml">Overridden customer attributes in XML format; pass null to use CustomCustomerAttributes of customer</param>
        /// <returns>Customer login model</returns>
        Task<LogInResponseModel> PrepareCustomerLoginModel(LogInResponseModel model, Customer customer);

        /// <summary>
        /// Prepare the register result model
        /// </summary>
        /// <param name="resultId">Value of UserRegistrationType enum</param>
        /// <param name="customer">Customer instance</param>
        /// <returns>Register result model</returns>
        Task<RegisterResponseModel> PrepareRegisterResultModel(int resultId, Customer customer);

        /// <summary>
        /// Prepare the customer info model
        /// </summary>
        /// <param name="model">Customer info model</param>
        /// <param name="customer">Customer</param>
        /// <param name="excludeProperties">Whether to exclude populating of model properties from the entity</param>
        /// <param name="overrideCustomCustomerAttributesXml">Overridden customer attributes in XML format; pass null to use CustomCustomerAttributes of customer</param>
        /// <returns>Customer info model</returns>
        Task<CustomerInfoResponseModel> PrepareCustomerInfoModel(CustomerInfoResponseModel model, Customer customer,
            bool excludeProperties, string overrideCustomCustomerAttributesXml = "");
        
        /// <summary>
        /// Prepare the custom customer attribute models
        /// </summary>
        /// <param name="customer">Customer</param>
        /// <param name="overrideAttributesXml">Overridden customer attributes in XML format; pass null to use CustomCustomerAttributes of customer</param>
        /// <returns>List of the customer attribute model</returns>
        Task<IList<CustomerAttributeModel>> PrepareCustomCustomerAttributes(Customer customer, string overrideAttributesXml = "");

        /// <summary>
        /// Prepare response model using query model data
        /// </summary>
        /// <param name="queryModel"></param>
        /// <returns></returns>
        Task<CustomerInfoResponseModel> PrepareCustomerInfoResponseModel(CustomerInfoQueryModel queryModel);
    }
}
