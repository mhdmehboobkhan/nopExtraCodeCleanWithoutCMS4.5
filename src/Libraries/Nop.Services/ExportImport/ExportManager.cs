using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using ClosedXML.Excel;
using Nop.Core;
using Nop.Core.Domain.Common;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Directory;
using Nop.Core.Domain.Messages;
using Nop.Services.Common;
using Nop.Services.Customers;
using Nop.Services.Directory;
using Nop.Services.ExportImport.Help;
using Nop.Services.Helpers;
using Nop.Services.Localization;
using Nop.Services.Media;
using Nop.Services.Messages;
using Nop.Services.Seo;
using Nop.Services.Stores;

namespace Nop.Services.ExportImport
{
    /// <summary>
    /// Export manager
    /// </summary>
    public partial class ExportManager : IExportManager
    {
        #region Fields

        private readonly CustomerSettings _customerSettings;
        private readonly DateTimeSettings _dateTimeSettings;
        private readonly ICountryService _countryService;
        private readonly ICustomerAttributeFormatter _customerAttributeFormatter;
        private readonly ICustomerService _customerService;
        private readonly IDateTimeHelper _dateTimeHelper;
        private readonly IGenericAttributeService _genericAttributeService;
        private readonly ILocalizationService _localizationService;
        private readonly IPictureService _pictureService;
        private readonly IStateProvinceService _stateProvinceService;
        private readonly IStoreMappingService _storeMappingService;
        private readonly IStoreService _storeService;
        private readonly IUrlRecordService _urlRecordService;
        private readonly IWorkContext _workContext;
        private readonly CommonSettings _commonSettings;

        #endregion

        #region Ctor

        public ExportManager(CustomerSettings customerSettings,
            DateTimeSettings dateTimeSettings,
            ICountryService countryService,
            ICustomerAttributeFormatter customerAttributeFormatter,
            ICustomerService customerService,
            IDateTimeHelper dateTimeHelper,
            IGenericAttributeService genericAttributeService,
            ILocalizationService localizationService,
            IPictureService pictureService,
            IStateProvinceService stateProvinceService,
            IStoreMappingService storeMappingService,
            IStoreService storeService,
            IUrlRecordService urlRecordService,
            IWorkContext workContext,
            CommonSettings commonSettings)
        {
            _customerSettings = customerSettings;
            _dateTimeSettings = dateTimeSettings;
            _countryService = countryService;
            _customerAttributeFormatter = customerAttributeFormatter;
            _customerService = customerService;
            _dateTimeHelper = dateTimeHelper;
            _genericAttributeService = genericAttributeService;
            _localizationService = localizationService;
            _pictureService = pictureService;
            _stateProvinceService = stateProvinceService;
            _storeMappingService = storeMappingService;
            _storeService = storeService;
            _urlRecordService = urlRecordService;
            _workContext = workContext;
            _commonSettings = commonSettings;
        }

        #endregion

        #region Utilities

        /// <summary>
        /// Returns the path to the image file by ID
        /// </summary>
        /// <param name="pictureId">Picture ID</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the path to the image file
        /// </returns>
        protected virtual async Task<string> GetPicturesAsync(int pictureId)
        {
            var picture = await _pictureService.GetPictureByIdAsync(pictureId);

            return await _pictureService.GetThumbLocalPathAsync(picture);
        }

        /// <returns>A task that represents the asynchronous operation</returns>
        protected virtual async Task<bool> IgnoreExportLimitedToStoreAsync()
        {
            return _commonSettings.IgnoreStoreLimitations ||
                   !_commonSettings.ExportImportProductUseLimitedToStores ||
                   (await _storeService.GetAllStoresAsync()).Count == 1;
        }

        /// <returns>A task that represents the asynchronous operation</returns>
        private async Task<object> GetCustomCustomerAttributesAsync(Customer customer)
        {
            var selectedCustomerAttributes = await _genericAttributeService.GetAttributeAsync<string>(customer, NopCustomerDefaults.CustomCustomerAttributes);
            
            return await _customerAttributeFormatter.FormatAttributesAsync(selectedCustomerAttributes, ";");
        }

        #endregion

        #region Methods

        /// <summary>
        /// Export customer list to XLSX
        /// </summary>
        /// <param name="customers">Customers</param>
        /// <returns>A task that represents the asynchronous operation</returns>
        public virtual async Task<byte[]> ExportCustomersToXlsxAsync(IList<Customer> customers)
        {
            async Task<object> getPasswordFormat(Customer customer)
            {
                var password = await _customerService.GetCurrentPasswordAsync(customer.Id);

                var passwordFormatId = password?.PasswordFormatId ?? 0;

                if (!_commonSettings.ExportImportRelatedEntitiesByName)
                    return passwordFormatId;

                return CommonHelper.ConvertEnum(((PasswordFormat)passwordFormatId).ToString());
            }

            async Task<object> getCountry(Customer customer)
            {
                var countryId = await _genericAttributeService.GetAttributeAsync<int>(customer, NopCustomerDefaults.CountryIdAttribute);

                if (!_commonSettings.ExportImportRelatedEntitiesByName)
                    return countryId;

                var country = await _countryService.GetCountryByIdAsync(countryId);

                return country?.Name ?? string.Empty;
            }

            async Task<object> getStateProvince(Customer customer)
            {
                var stateProvinceId = await _genericAttributeService.GetAttributeAsync<int>(customer, NopCustomerDefaults.StateProvinceIdAttribute);

                if (!_commonSettings.ExportImportRelatedEntitiesByName)
                    return stateProvinceId;

                var stateProvince = await _stateProvinceService.GetStateProvinceByIdAsync(stateProvinceId);

                return stateProvince?.Name ?? string.Empty;
            }

            //property manager 
            var manager = new PropertyManager<Customer>(new[]
            {
                new PropertyByName<Customer>("CustomerId", p => p.Id),
                new PropertyByName<Customer>("CustomerGuid", p => p.CustomerGuid),
                new PropertyByName<Customer>("Email", p => p.Email),
                new PropertyByName<Customer>("Username", p => p.Username),
                new PropertyByName<Customer>("Password", async p => (await _customerService.GetCurrentPasswordAsync(p.Id))?.Password),
                new PropertyByName<Customer>("PasswordFormat", getPasswordFormat),
                new PropertyByName<Customer>("PasswordSalt", async p => (await _customerService.GetCurrentPasswordAsync(p.Id))?.PasswordSalt),
                new PropertyByName<Customer>("Active", p => p.Active),
                new PropertyByName<Customer>("CustomerRoles", async p=>string.Join(", ",
                    (await _customerService.GetCustomerRolesAsync(p)).Select(role => _commonSettings.ExportImportRelatedEntitiesByName ? role.Name : role.Id.ToString()))), 
                new PropertyByName<Customer>("IsGuest", async p => await _customerService.IsGuestAsync(p)),
                new PropertyByName<Customer>("IsRegistered", async p => await _customerService.IsRegisteredAsync(p)),
                new PropertyByName<Customer>("IsAdministrator", async p => await _customerService.IsAdminAsync(p)),
                new PropertyByName<Customer>("CreatedOnUtc", p => p.CreatedOnUtc),
                //attributes
                new PropertyByName<Customer>("FirstName", async p => await _genericAttributeService.GetAttributeAsync<string>(p, NopCustomerDefaults.FirstNameAttribute), !_customerSettings.FirstNameEnabled),
                new PropertyByName<Customer>("LastName", async p => await _genericAttributeService.GetAttributeAsync<string>(p, NopCustomerDefaults.LastNameAttribute), !_customerSettings.LastNameEnabled),
                new PropertyByName<Customer>("Gender", async p => await _genericAttributeService.GetAttributeAsync<string>(p, NopCustomerDefaults.GenderAttribute), !_customerSettings.GenderEnabled),
                new PropertyByName<Customer>("Company", async p => await _genericAttributeService.GetAttributeAsync<string>(p, NopCustomerDefaults.CompanyAttribute), !_customerSettings.CompanyEnabled),
                new PropertyByName<Customer>("StreetAddress", async p => await _genericAttributeService.GetAttributeAsync<string>(p, NopCustomerDefaults.StreetAddressAttribute), !_customerSettings.StreetAddressEnabled),
                new PropertyByName<Customer>("StreetAddress2", async p => await _genericAttributeService.GetAttributeAsync<string>(p, NopCustomerDefaults.StreetAddress2Attribute), !_customerSettings.StreetAddress2Enabled),
                new PropertyByName<Customer>("ZipPostalCode", async p => await _genericAttributeService.GetAttributeAsync<string>(p, NopCustomerDefaults.ZipPostalCodeAttribute), !_customerSettings.ZipPostalCodeEnabled),
                new PropertyByName<Customer>("City", async p => await _genericAttributeService.GetAttributeAsync<string>(p, NopCustomerDefaults.CityAttribute), !_customerSettings.CityEnabled),
                new PropertyByName<Customer>("County", async p => await _genericAttributeService.GetAttributeAsync<string>(p, NopCustomerDefaults.CountyAttribute), !_customerSettings.CountyEnabled),
                new PropertyByName<Customer>("Country", getCountry, !_customerSettings.CountryEnabled),
                new PropertyByName<Customer>("StateProvince", getStateProvince, !_customerSettings.StateProvinceEnabled),
                new PropertyByName<Customer>("Phone", async p => await _genericAttributeService.GetAttributeAsync<string>(p, NopCustomerDefaults.PhoneAttribute), !_customerSettings.PhoneEnabled),
                new PropertyByName<Customer>("Fax", async p => await _genericAttributeService.GetAttributeAsync<string>(p, NopCustomerDefaults.FaxAttribute), !_customerSettings.FaxEnabled),
                new PropertyByName<Customer>("TimeZone", async p => await _genericAttributeService.GetAttributeAsync<string>(p, NopCustomerDefaults.TimeZoneIdAttribute), !_dateTimeSettings.AllowCustomersToSetTimeZone),
                new PropertyByName<Customer>("AvatarPictureId", async p => await _genericAttributeService.GetAttributeAsync<int>(p, NopCustomerDefaults.AvatarPictureIdAttribute), !_customerSettings.AllowCustomersToUploadAvatars),
                new PropertyByName<Customer>("CustomCustomerAttributes",  GetCustomCustomerAttributesAsync)
            }, _commonSettings);

            return await manager.ExportToXlsxAsync(customers);
        }

        /// <summary>
        /// Export customer list to XML
        /// </summary>
        /// <param name="customers">Customers</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the result in XML format
        /// </returns>
        public virtual async Task<string> ExportCustomersToXmlAsync(IList<Customer> customers)
        {
            var settings = new XmlWriterSettings
            {
                Async = true,
                ConformanceLevel = ConformanceLevel.Auto
            };

            await using var stringWriter = new StringWriter();
            await using var xmlWriter = XmlWriter.Create(stringWriter, settings);

            await xmlWriter.WriteStartDocumentAsync();
            await xmlWriter.WriteStartElementAsync("Customers");
            await xmlWriter.WriteAttributeStringAsync("Version", NopVersion.CURRENT_VERSION);

            foreach (var customer in customers)
            {
                await xmlWriter.WriteStartElementAsync("Customer");
                await xmlWriter.WriteElementStringAsync("CustomerId", null, customer.Id.ToString());
                await xmlWriter.WriteElementStringAsync("CustomerGuid", null, customer.CustomerGuid.ToString());
                await xmlWriter.WriteElementStringAsync("Email", null, customer.Email);
                await xmlWriter.WriteElementStringAsync("Username", null, customer.Username);

                var customerPassword = await _customerService.GetCurrentPasswordAsync(customer.Id);
                await xmlWriter.WriteElementStringAsync("Password", null, customerPassword?.Password);
                await xmlWriter.WriteElementStringAsync("PasswordFormatId", null, (customerPassword?.PasswordFormatId ?? 0).ToString());
                await xmlWriter.WriteElementStringAsync("PasswordSalt", null, customerPassword?.PasswordSalt);

                await xmlWriter.WriteElementStringAsync("Active", null, customer.Active.ToString());

                await xmlWriter.WriteElementStringAsync("IsGuest", null, (await _customerService.IsGuestAsync(customer)).ToString());
                await xmlWriter.WriteElementStringAsync("IsRegistered", null, (await _customerService.IsRegisteredAsync(customer)).ToString());
                await xmlWriter.WriteElementStringAsync("IsAdministrator", null, (await _customerService.IsAdminAsync(customer)).ToString());
                await xmlWriter.WriteElementStringAsync("CreatedOnUtc", null, customer.CreatedOnUtc.ToString(CultureInfo.InvariantCulture));

                await xmlWriter.WriteElementStringAsync("FirstName", null, await _genericAttributeService.GetAttributeAsync<string>(customer, NopCustomerDefaults.FirstNameAttribute));
                await xmlWriter.WriteElementStringAsync("LastName", null, await _genericAttributeService.GetAttributeAsync<string>(customer, NopCustomerDefaults.LastNameAttribute));
                await xmlWriter.WriteElementStringAsync("Gender", null, await _genericAttributeService.GetAttributeAsync<string>(customer, NopCustomerDefaults.GenderAttribute));
                await xmlWriter.WriteElementStringAsync("Company", null, await _genericAttributeService.GetAttributeAsync<string>(customer, NopCustomerDefaults.CompanyAttribute));

                await xmlWriter.WriteElementStringAsync("CountryId", null, (await _genericAttributeService.GetAttributeAsync<int>(customer, NopCustomerDefaults.CountryIdAttribute)).ToString());
                await xmlWriter.WriteElementStringAsync("StreetAddress", null, await _genericAttributeService.GetAttributeAsync<string>(customer, NopCustomerDefaults.StreetAddressAttribute));
                await xmlWriter.WriteElementStringAsync("StreetAddress2", null, await _genericAttributeService.GetAttributeAsync<string>(customer, NopCustomerDefaults.StreetAddress2Attribute));
                await xmlWriter.WriteElementStringAsync("ZipPostalCode", null, await _genericAttributeService.GetAttributeAsync<string>(customer, NopCustomerDefaults.ZipPostalCodeAttribute));
                await xmlWriter.WriteElementStringAsync("City", null, await _genericAttributeService.GetAttributeAsync<string>(customer, NopCustomerDefaults.CityAttribute));
                await xmlWriter.WriteElementStringAsync("County", null, await _genericAttributeService.GetAttributeAsync<string>(customer, NopCustomerDefaults.CountyAttribute));
                await xmlWriter.WriteElementStringAsync("StateProvinceId", null, (await _genericAttributeService.GetAttributeAsync<int>(customer, NopCustomerDefaults.StateProvinceIdAttribute)).ToString());
                await xmlWriter.WriteElementStringAsync("Phone", null, await _genericAttributeService.GetAttributeAsync<string>(customer, NopCustomerDefaults.PhoneAttribute));
                await xmlWriter.WriteElementStringAsync("Fax", null, await _genericAttributeService.GetAttributeAsync<string>(customer, NopCustomerDefaults.FaxAttribute));
                await xmlWriter.WriteElementStringAsync("TimeZoneId", null, await _genericAttributeService.GetAttributeAsync<string>(customer, NopCustomerDefaults.TimeZoneIdAttribute));

                await xmlWriter.WriteElementStringAsync("AvatarPictureId", null, (await _genericAttributeService.GetAttributeAsync<int>(customer, NopCustomerDefaults.AvatarPictureIdAttribute)).ToString());

                var selectedCustomerAttributesString = await _genericAttributeService.GetAttributeAsync<string>(customer, NopCustomerDefaults.CustomCustomerAttributes);

                if (!string.IsNullOrEmpty(selectedCustomerAttributesString))
                {
                    var selectedCustomerAttributes = new StringReader(selectedCustomerAttributesString);
                    var selectedCustomerAttributesXmlReader = XmlReader.Create(selectedCustomerAttributes);
                    await xmlWriter.WriteNodeAsync(selectedCustomerAttributesXmlReader, false);
                }

                await xmlWriter.WriteEndElementAsync();
            }

            await xmlWriter.WriteEndElementAsync();
            await xmlWriter.WriteEndDocumentAsync();
            await xmlWriter.FlushAsync();

            return stringWriter.ToString();
        }

        /// <summary>
        /// Export states to TXT
        /// </summary>
        /// <param name="states">States</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the result in TXT (string) format
        /// </returns>
        public virtual async Task<string> ExportStatesToTxtAsync(IList<StateProvince> states)
        {
            if (states == null)
                throw new ArgumentNullException(nameof(states));

            const char separator = ',';
            var sb = new StringBuilder();
            foreach (var state in states)
            {
                sb.Append((await _countryService.GetCountryByIdAsync(state.CountryId)).TwoLetterIsoCode);
                sb.Append(separator);
                sb.Append(state.Name);
                sb.Append(separator);
                sb.Append(state.Abbreviation);
                sb.Append(separator);
                sb.Append(state.Published);
                sb.Append(separator);
                sb.Append(state.DisplayOrder);
                sb.Append(Environment.NewLine); //new line
            }

            return sb.ToString();
        }

        #endregion
    }
}
