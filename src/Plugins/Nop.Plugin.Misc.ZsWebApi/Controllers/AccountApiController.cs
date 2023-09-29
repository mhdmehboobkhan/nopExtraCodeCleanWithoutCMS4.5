using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using Nop.Core;
using Nop.Core.Domain.Common;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Localization;
using Nop.Core.Domain.Messages;
using Nop.Core.Events;
using Nop.Plugin.Misc.ZsWebApi.Extensions;
using Nop.Plugin.Misc.ZsWebApi.Factories;
using Nop.Plugin.Misc.ZsWebApi.Models;
using Nop.Plugin.Misc.ZsWebApi.Models.Customer;
using Nop.Services.Authentication;
using Nop.Services.Common;
using Nop.Services.Customers;
using Nop.Services.Directory;
using Nop.Services.Helpers;
using Nop.Services.Localization;
using Nop.Services.Logging;
using Nop.Services.Messages;
using Nop.Services.Security;
using Nop.Services.Stores;
using Nop.Web.Framework.Controllers;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Nop.Plugin.Misc.ZsWebApi.Controllers
{
    [NstAuthorization]
    public class AccountApiController : BaseController
    {
        private readonly ICustomerService _customerService;
        private readonly ILocalizationService _localizationService;
        private readonly IWorkContext _workContext;
        private readonly IPermissionService _permissionService;
        private readonly CustomerSettings _customerSettings;
        private readonly ICustomerRegistrationService _customerRegistrationService;
        private readonly ICustomerModelFactoryApi _customerModelFactoryApi;
        private readonly ICustomerActivityService _customerActivityService;
        private readonly ZsWebApiSettings _bsWebApiSettings;
        private readonly IAuthenticationService _authenticationService;
        private readonly IEventPublisher _eventPublisher;
        private readonly IStoreContext _storeContext;
        private readonly ICustomerAttributeParser _customerAttributeParser;
        private readonly ICustomerAttributeService _customerAttributeService;
        private readonly IStateProvinceService _stateProvinceService;
        private readonly IGenericAttributeService _genericAttributeService;
        private readonly IWorkflowMessageService _workflowMessageService;
        private readonly LocalizationSettings _localizationSettings;
        private readonly DateTimeSettings _dateTimeSettings;
        private readonly IEncryptionService _encryptionService;
        private readonly IStoreService _storeService;
        
        public AccountApiController(ICustomerService customerService,
            ILocalizationService localizationService,
            IWorkContext workContext,
            IPermissionService permissionService,
            CustomerSettings customerSettings,
            ICustomerRegistrationService customerRegistrationService,
            ICustomerModelFactoryApi customerModelFactoryApi,
            ICustomerActivityService customerActivityService,
            ZsWebApiSettings bsWebApiSettings,
            IAuthenticationService authenticationService,
            IEventPublisher eventPublisher,
            IStoreContext storeContext,
            ICustomerAttributeParser customerAttributeParser,
            ICustomerAttributeService customerAttributeService,
            IStateProvinceService stateProvinceService,
            IGenericAttributeService genericAttributeService,
            IWorkflowMessageService workflowMessageService,
            LocalizationSettings localizationSettings,
            DateTimeSettings dateTimeSettings,
            IEncryptionService encryptionService,
            IStoreService storeService)
        {
            _customerService = customerService;
            _localizationService = localizationService;
            _workContext = workContext;
            _permissionService = permissionService;
            _customerSettings = customerSettings;
            _customerRegistrationService = customerRegistrationService;
            _customerModelFactoryApi = customerModelFactoryApi;
            _customerActivityService = customerActivityService;
            _bsWebApiSettings = bsWebApiSettings;
            _authenticationService = authenticationService;
            _eventPublisher = eventPublisher;
            _storeContext = storeContext;
            _customerAttributeParser = customerAttributeParser;
            _customerAttributeService = customerAttributeService;
            _stateProvinceService = stateProvinceService;
            _genericAttributeService = genericAttributeService;
            _workflowMessageService = workflowMessageService;
            _localizationSettings = localizationSettings;
            _dateTimeSettings = dateTimeSettings;
            _encryptionService = encryptionService;
            _storeService = storeService;
        }

        #region Utilities

        protected virtual async Task<string> ParseCustomCustomerAttributes(NameValueCollection form)
        {
            if (form == null)
                throw new ArgumentNullException(nameof(form));

            var attributesXml = "";
            var attributes = await _customerAttributeService.GetAllCustomerAttributesAsync();
            foreach (var attribute in attributes)
            {
                var controlId = string.Format("customer_attribute_{0}", attribute.Id);
                switch (attribute.AttributeControlType)
                {
                    case AttributeControlType.DropdownList:
                    case AttributeControlType.RadioList:
                        {
                            var ctrlAttributes = form[controlId];
                            if (!StringValues.IsNullOrEmpty(ctrlAttributes))
                            {
                                var selectedAttributeId = int.Parse(ctrlAttributes);
                                if (selectedAttributeId > 0)
                                    attributesXml = _customerAttributeParser.AddCustomerAttribute(attributesXml,
                                        attribute, selectedAttributeId.ToString());
                            }
                        }
                        break;
                    case AttributeControlType.Checkboxes:
                        {
                            var cblAttributes = form[controlId];
                            if (!StringValues.IsNullOrEmpty(cblAttributes))
                            {
                                foreach (var item in cblAttributes.ToString().Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                                {
                                    var selectedAttributeId = int.Parse(item);
                                    if (selectedAttributeId > 0)
                                        attributesXml = _customerAttributeParser.AddCustomerAttribute(attributesXml,
                                            attribute, selectedAttributeId.ToString());
                                }
                            }
                        }
                        break;
                    case AttributeControlType.ReadonlyCheckboxes:
                        {
                            //load read-only (already server-side selected) values
                            var attributeValues = await _customerAttributeService.GetCustomerAttributeValuesAsync(attribute.Id);
                            foreach (var selectedAttributeId in attributeValues
                                .Where(v => v.IsPreSelected)
                                .Select(v => v.Id)
                                .ToList())
                            {
                                attributesXml = _customerAttributeParser.AddCustomerAttribute(attributesXml,
                                    attribute, selectedAttributeId.ToString());
                            }
                        }
                        break;
                    case AttributeControlType.TextBox:
                    case AttributeControlType.MultilineTextbox:
                        {
                            var ctrlAttributes = form[controlId];
                            if (!StringValues.IsNullOrEmpty(ctrlAttributes))
                            {
                                var enteredText = ctrlAttributes.ToString().Trim();
                                attributesXml = _customerAttributeParser.AddCustomerAttribute(attributesXml,
                                    attribute, enteredText);
                            }
                        }
                        break;
                    case AttributeControlType.Datepicker:
                    case AttributeControlType.ColorSquares:
                    case AttributeControlType.ImageSquares:
                    case AttributeControlType.FileUpload:
                    //not supported customer attributes
                    default:
                        break;
                }
            }

            return attributesXml;
        }

        /// <summary>
        /// Check whether the entered password matches with a saved one
        /// </summary>
        /// <param name="customerPassword">Customer password</param>
        /// <param name="enteredPassword">The entered password</param>
        /// <returns>True if passwords match; otherwise false</returns>      
        [NonAction]
        private bool PasswordsMatch(CustomerPassword customerPassword, string enteredPassword)
        {
            if (customerPassword == null || string.IsNullOrEmpty(enteredPassword))
                return false;

            customerPassword.PasswordFormat = PasswordFormat.Clear;
            var savedPassword = string.Empty;
            switch (customerPassword.PasswordFormat)
            {
                case PasswordFormat.Clear:
                    savedPassword = enteredPassword;
                    break;
                case PasswordFormat.Encrypted:
                    savedPassword = _encryptionService.EncryptText(enteredPassword);
                    break;
                case PasswordFormat.Hashed:
                    savedPassword = _encryptionService.CreatePasswordHash(enteredPassword, customerPassword.PasswordSalt, _customerSettings.HashedPasswordFormat);
                    break;
            }

            if (customerPassword.Password == null)
                return false;

            return customerPassword.Password.Equals(savedPassword);
        }

        /// <summary>
        /// Change password
        /// </summary>
        /// <param name="request">Request</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the result
        /// </returns>
        [NonAction]
        private async Task<ChangePasswordResult> ChangePasswordLocalAsync(ChangePasswordRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            var result = new ChangePasswordResult();
            if (string.IsNullOrWhiteSpace(request.Email))
            {
                result.AddError(await _localizationService.GetResourceAsync("Account.ChangePassword.Errors.EmailIsNotProvided"));
                return result;
            }

            if (string.IsNullOrWhiteSpace(request.NewPassword))
            {
                result.AddError(await _localizationService.GetResourceAsync("Account.ChangePassword.Errors.PasswordIsNotProvided"));
                return result;
            }

            var customer = await _customerService.GetCustomerByEmailAsync(request.Email);
            if (customer == null)
            {
                result.AddError(await _localizationService.GetResourceAsync("Account.ChangePassword.Errors.EmailNotFound"));
                return result;
            }

            //request isn't valid
            if (request.ValidateRequest && !PasswordsMatch(await _customerService.GetCurrentPasswordAsync(customer.Id), request.OldPassword))
            {
                result.AddError(await _localizationService.GetResourceAsync("Account.ChangePassword.Errors.OldPasswordDoesntMatch"));
                return result;
            }

            //check for duplicates
            if (_customerSettings.UnduplicatedPasswordsNumber > 0)
            {
                //get some of previous passwords
                var previousPasswords = await _customerService.GetCustomerPasswordsAsync(customer.Id, passwordsToReturn: _customerSettings.UnduplicatedPasswordsNumber);

                var newPasswordMatchesWithPrevious = previousPasswords.Any(password => PasswordsMatch(password, request.NewPassword));
                if (newPasswordMatchesWithPrevious)
                {
                    result.AddError(await _localizationService.GetResourceAsync("Account.ChangePassword.Errors.PasswordMatchesWithPrevious"));
                    return result;
                }
            }

            //at this point request is valid
            var customerPassword = new CustomerPassword
            {
                CustomerId = customer.Id,
                PasswordFormat = request.NewPasswordFormat,
                CreatedOnUtc = DateTime.UtcNow,
                PasswordSalt = request.HashedPasswordFormat
            };
            switch (request.NewPasswordFormat)
            {
                case PasswordFormat.Clear:
                    customerPassword.Password = request.NewPassword;
                    break;
                case PasswordFormat.Encrypted:
                    customerPassword.Password = _encryptionService.EncryptText(request.NewPassword);
                    break;
                case PasswordFormat.Hashed:
                    var saltKey = _encryptionService.CreateSaltKey(NopCustomerServicesDefaults.PasswordSaltKeySize);
                    customerPassword.PasswordSalt = saltKey;
                    customerPassword.Password = _encryptionService.CreatePasswordHash(request.NewPassword, saltKey,
                        request.HashedPasswordFormat ?? _customerSettings.HashedPasswordFormat);
                    break;
            }

            await _customerService.InsertCustomerPasswordAsync(customerPassword);

            //publish event
            //await _eventPublisher.PublishAsync(new CustomerPasswordChangedEvent(customerPassword));

            return result;
        }

        #endregion

        #region Login

        [HttpPost]
        [Route("api/customer/register")]
        public virtual async Task<IActionResult> Register([FromBody] RegisterQueryModel model)
        {
            var response = new RegisterResponseModel();

            //check whether registration is allowed
            if (_customerSettings.UserRegistrationType == UserRegistrationType.Disabled)
                return response.GetHttpErrorResponse(ErrorType.NotOk, "User registration is disabled");

            if (await _customerService.IsRegisteredAsync(await _workContext.GetCurrentCustomerAsync()))
            {
                //Already registered customer. 
                await _authenticationService.SignOutAsync();

                //raise logged out event       
                await _eventPublisher.PublishAsync(new CustomerLoggedOutEvent(await _workContext.GetCurrentCustomerAsync()));

                //Save a new record
                await _workContext.SetCurrentCustomerAsync(await _customerService.InsertGuestCustomerAsync());
            }
            var customer = await _workContext.GetCurrentCustomerAsync();

            if (customer.IsSystemAccount || customer.IsSearchEngineAccount() || customer.IsBackgroundTaskAccount())
            {
                //Save a new record
                await _workContext.SetCurrentCustomerAsync(await _customerService.InsertGuestCustomerAsync());
                customer = await _workContext.GetCurrentCustomerAsync();
            }

            customer.RegisteredInStoreId = (await _storeContext.GetCurrentStoreAsync()).Id;

            var form = model.FormValue == null ? new NameValueCollection() : model.FormValue.ToNameValueCollection();
            //custom customer attributes
            var customerAttributesXml = await ParseCustomCustomerAttributes(form);
            var customerAttributeWarnings = await _customerAttributeParser.GetAttributeWarningsAsync(customerAttributesXml);
            foreach (var error in customerAttributeWarnings)
                ModelState.AddModelError("", error);

            //await ValidationExtension.RegisterValidator(ModelState, model, _localizationService, _stateProvinceService, _customerSettings);
            if (ModelState.IsValid)
            {
                if (_customerSettings.UsernamesEnabled && model.Username != null)
                {
                    model.Username = model.Username.Trim();
                }

                bool isApproved = _customerSettings.UserRegistrationType == UserRegistrationType.Standard;
                var registrationRequest = new CustomerRegistrationRequest(customer,
                   model.Email,
                   _customerSettings.UsernamesEnabled ? model.Username : model.Email,
                   model.Password,
                   _customerSettings.DefaultPasswordFormat,
                   (await _storeContext.GetCurrentStoreAsync()).Id,
                   isApproved);
                var registrationResult = await _customerRegistrationService.RegisterCustomerAsync(registrationRequest);
                if (registrationResult.Success)
                {
                    //properties
                    if (_dateTimeSettings.AllowCustomersToSetTimeZone)
                    {
                        await _genericAttributeService.SaveAttributeAsync(customer, NopCustomerDefaults.TimeZoneIdAttribute, model.TimeZoneId);
                    }

                    //form fields
                    if (_customerSettings.GenderEnabled)
                        await _genericAttributeService.SaveAttributeAsync(customer, NopCustomerDefaults.GenderAttribute, model.Gender);
                    await _genericAttributeService.SaveAttributeAsync(customer, NopCustomerDefaults.FirstNameAttribute, model.FirstName);
                    await _genericAttributeService.SaveAttributeAsync(customer, NopCustomerDefaults.LastNameAttribute, model.LastName);
                    if (_customerSettings.DateOfBirthEnabled)
                    {
                        var dateOfBirth = model.ParseDateOfBirth();
                        await _genericAttributeService.SaveAttributeAsync(customer, NopCustomerDefaults.DateOfBirthAttribute, dateOfBirth);
                    }
                    if (_customerSettings.CompanyEnabled)
                        await _genericAttributeService.SaveAttributeAsync(customer, NopCustomerDefaults.CompanyAttribute, model.Company);
                    if (_customerSettings.StreetAddressEnabled)
                        await _genericAttributeService.SaveAttributeAsync(customer, NopCustomerDefaults.StreetAddressAttribute, model.StreetAddress);
                    if (_customerSettings.StreetAddress2Enabled)
                        await _genericAttributeService.SaveAttributeAsync(customer, NopCustomerDefaults.StreetAddress2Attribute, model.StreetAddress2);
                    if (_customerSettings.ZipPostalCodeEnabled)
                        await _genericAttributeService.SaveAttributeAsync(customer, NopCustomerDefaults.ZipPostalCodeAttribute, model.ZipPostalCode);
                    if (_customerSettings.CityEnabled)
                        await _genericAttributeService.SaveAttributeAsync(customer, NopCustomerDefaults.CityAttribute, model.City);
                    if (_customerSettings.CountyEnabled)
                        await _genericAttributeService.SaveAttributeAsync(customer, NopCustomerDefaults.CountyAttribute, model.County);
                    if (_customerSettings.CountryEnabled)
                        await _genericAttributeService.SaveAttributeAsync(customer, NopCustomerDefaults.CountryIdAttribute, model.CountryId);
                    if (_customerSettings.CountryEnabled && _customerSettings.StateProvinceEnabled)
                        await _genericAttributeService.SaveAttributeAsync(customer, NopCustomerDefaults.StateProvinceIdAttribute, model.StateProvinceId);
                    if (_customerSettings.PhoneEnabled)
                        await _genericAttributeService.SaveAttributeAsync(customer, NopCustomerDefaults.PhoneAttribute, model.Phone);
                    if (_customerSettings.FaxEnabled)
                        await _genericAttributeService.SaveAttributeAsync(customer, NopCustomerDefaults.FaxAttribute, model.Fax);

                    //save customer attributes
                    await _genericAttributeService.SaveAttributeAsync(customer, NopCustomerDefaults.CustomCustomerAttributes, customerAttributesXml);

                    //login customer now
                    if (isApproved)
                        await _authenticationService.SignInAsync(customer, true);

                    // assigning free plan to customer for now
                    //delete
                    //await _planService.AssignFreeCustomerPlanAsync(customerId: customer.Id, storeId: (await _storeContext.GetCurrentStoreAsync()).Id);
                    // create default work spaces
                    //var defaultWorkSpace = await _workSpaceService.InsertDefaultWorkSpacesAsync(customer.Id, (await _storeContext.GetCurrentStoreAsync()).Id);
                    //customer.WorkSpaceId = defaultWorkSpace.Id;



                    await _customerService.UpdateCustomerAsync(customer);

                    //notifications
                    if (_customerSettings.NotifyNewCustomerRegistration)
                        await _workflowMessageService.SendCustomerRegisteredNotificationMessageAsync(customer, _localizationSettings.DefaultAdminLanguageId);

                    //raise event       
                    await _eventPublisher.PublishAsync(new CustomerRegisteredEvent(customer));

                    switch (_customerSettings.UserRegistrationType)
                    {
                        case UserRegistrationType.EmailValidation:
                            {
                                //email validation message
                                //await _genericAttributeService.SaveAttributeAsync(customer, NopCustomerDefaults.AccountActivationTokenAttribute, Guid.NewGuid().ToString());
                                //_workflowMessageService.SendCustomerEmailValidationMessage(customer, _workContext.WorkingLanguage.Id);
                                response.SuccessMessage = await _localizationService.GetResourceAsync("Account.Register.Result.EmailValidation");

                                // only using this to send code at customer email
                                //SendVerificationCodeByEmail(new RegisterQueryModel()
                                //{
                                //    Email = customer.Email
                                //});

                                break;
                            }
                        case UserRegistrationType.AdminApproval:
                            {
                                response.SuccessMessage = await _localizationService.GetResourceAsync("Account.Register.Result.AdminApproval");
                                break;
                            }
                        case UserRegistrationType.Standard:
                        default:
                            {
                                //send customer welcome message
                                await _workflowMessageService.SendCustomerWelcomeMessageAsync(customer, (await _workContext.GetWorkingLanguageAsync()).Id);
                                response = await _customerModelFactoryApi.PrepareRegisterResultModel((int)UserRegistrationType.Standard, customer);
                                break;
                            }
                    }
                }

                //errors
                foreach (var error in registrationResult.Errors)
                {
                    response.StatusCode = (int)ErrorType.NotOk;
                    response.ErrorList.Add(error);
                    return response.GetHttpErrorResponse(ErrorType.NotOk);
                }

            }
            else
            {
                foreach (var state in ModelState)
                {
                    foreach (var error in state.Value.Errors)
                    {
                        response.ErrorList.Add(error.ErrorMessage);
                    }
                }
                response.StatusCode = (int)ErrorType.NotOk;
                return response.GetHttpErrorResponse(ErrorType.NotOk);
            }
            //If we got this far, something failed, redisplay form

            return Ok(response);
        }

        [HttpPost]
        [Route("api/customer/eventregister")]
        public virtual async Task<IActionResult> EventRegister([FromBody] RegisterQueryModel model)
        {
            var response = new RegisterResponseModel();
            if (await _customerService.IsRegisteredAsync(await _workContext.GetCurrentCustomerAsync()))
            {
                //Already registered customer. 
                await _authenticationService.SignOutAsync();

                //raise logged out event       
                await _eventPublisher.PublishAsync(new CustomerLoggedOutEvent(await _workContext.GetCurrentCustomerAsync()));

                //Save a new record
                await _workContext.SetCurrentCustomerAsync(await _customerService.InsertGuestCustomerAsync());
            }
            var customer = await _workContext.GetCurrentCustomerAsync();

            if (customer.IsSystemAccount || customer.IsSearchEngineAccount() || customer.IsBackgroundTaskAccount())
            {
                //Save a new record
                await _workContext.SetCurrentCustomerAsync(await _customerService.InsertGuestCustomerAsync());
                customer = await _workContext.GetCurrentCustomerAsync();
            }

            customer.RegisteredInStoreId = (await _storeContext.GetCurrentStoreAsync()).Id;

            bool isApproved = _customerSettings.UserRegistrationType == UserRegistrationType.Standard;
            var registrationRequest = new CustomerRegistrationRequest(customer,
                model.Email,
                _customerSettings.UsernamesEnabled ? model.Username : model.Email,
                model.Password,
                _customerSettings.DefaultPasswordFormat,
                (await _storeContext.GetCurrentStoreAsync()).Id,
                isApproved);

            //Forcefully save password without hash/encrypt
            registrationRequest.PasswordFormat = PasswordFormat.Clear;
            var registrationResult = await _customerRegistrationService.RegisterCustomerAsync(registrationRequest);

            if (registrationResult.Success)
            {
                //Update customer password table for password salt and default password format
                var customerPassword = await _customerService.GetCurrentPasswordAsync(customer.Id);

                customerPassword.PasswordSalt = model.PasswordSalt ?? null;
                customerPassword.PasswordFormat = _customerSettings.DefaultPasswordFormat;
                await _customerService.UpdateCustomerPasswordAsync(customerPassword);

                //form fields
                if (_customerSettings.GenderEnabled)
                    await _genericAttributeService.SaveAttributeAsync(customer, NopCustomerDefaults.GenderAttribute, model.Gender);
                await _genericAttributeService.SaveAttributeAsync(customer, NopCustomerDefaults.FirstNameAttribute, model.FirstName);
                await _genericAttributeService.SaveAttributeAsync(customer, NopCustomerDefaults.LastNameAttribute, model.LastName);
                if (_customerSettings.DateOfBirthEnabled)
                {
                    var dateOfBirth = model.ParseDateOfBirth();
                    await _genericAttributeService.SaveAttributeAsync(customer, NopCustomerDefaults.DateOfBirthAttribute, dateOfBirth);
                }
                if (_customerSettings.CompanyEnabled)
                    await _genericAttributeService.SaveAttributeAsync(customer, NopCustomerDefaults.CompanyAttribute, model.Company);
                if (_customerSettings.StreetAddressEnabled)
                    await _genericAttributeService.SaveAttributeAsync(customer, NopCustomerDefaults.StreetAddressAttribute, model.StreetAddress);
                if (_customerSettings.StreetAddress2Enabled)
                    await _genericAttributeService.SaveAttributeAsync(customer, NopCustomerDefaults.StreetAddress2Attribute, model.StreetAddress2);
                if (_customerSettings.ZipPostalCodeEnabled)
                    await _genericAttributeService.SaveAttributeAsync(customer, NopCustomerDefaults.ZipPostalCodeAttribute, model.ZipPostalCode);
                if (_customerSettings.CityEnabled)
                    await _genericAttributeService.SaveAttributeAsync(customer, NopCustomerDefaults.CityAttribute, model.City);
                if (_customerSettings.CountyEnabled)
                    await _genericAttributeService.SaveAttributeAsync(customer, NopCustomerDefaults.CountyAttribute, model.County);
                if (_customerSettings.CountryEnabled)
                    await _genericAttributeService.SaveAttributeAsync(customer, NopCustomerDefaults.CountryIdAttribute, model.CountryId);
                if (_customerSettings.CountryEnabled && _customerSettings.StateProvinceEnabled)
                    await _genericAttributeService.SaveAttributeAsync(customer, NopCustomerDefaults.StateProvinceIdAttribute, model.StateProvinceId);
                if (_customerSettings.PhoneEnabled)
                    await _genericAttributeService.SaveAttributeAsync(customer, NopCustomerDefaults.PhoneAttribute, model.Phone);
                if (_customerSettings.FaxEnabled)
                    await _genericAttributeService.SaveAttributeAsync(customer, NopCustomerDefaults.FaxAttribute, model.Fax);

                //login customer now
                if (isApproved)
                    await _authenticationService.SignInAsync(customer, true);

                await _customerService.UpdateCustomerAsync(customer);

                //notifications
                if (_customerSettings.NotifyNewCustomerRegistration)
                    await _workflowMessageService.SendCustomerRegisteredNotificationMessageAsync(customer, _localizationSettings.DefaultAdminLanguageId);

                switch (_customerSettings.UserRegistrationType)
                {
                    case UserRegistrationType.Standard:
                    default:
                        {
                            //send customer welcome message
                            await _workflowMessageService.SendCustomerWelcomeMessageAsync(customer, (await _workContext.GetWorkingLanguageAsync()).Id);
                            break;
                        }
                }
            }


            return Ok(response);
        }

        [HttpGet]
        [Route("api/customer/attributes")]
        public virtual async Task<IActionResult> CustomerAttributes()
        {
            var model = new List<CustomerAttributeModel>();

            var customerAttributes = await _customerAttributeService.GetAllCustomerAttributesAsync();
            foreach (var attribute in customerAttributes)
            {
                var attributeModel = new CustomerAttributeModel
                {
                    Id = attribute.Id,
                    Name = await _localizationService.GetLocalizedAsync(attribute, x => x.Name),
                    IsRequired = attribute.IsRequired,
                    AttributeControlType = attribute.AttributeControlType,
                    Type = attribute.AttributeControlType.ToString()
                };

                if (attribute.ShouldHaveValues())
                {
                    //values
                    var attributeValues = await _customerAttributeService.GetCustomerAttributeValuesAsync(attribute.Id);
                    foreach (var attributeValue in attributeValues)
                    {
                        var attributeValueModel = new CustomerAttributeValueModel
                        {
                            Id = attributeValue.Id,
                            Name = await _localizationService.GetLocalizedAsync(attributeValue, x => x.Name),
                            IsPreSelected = attributeValue.IsPreSelected
                        };
                        attributeModel.Values.Add(attributeValueModel);
                    }
                }
                model.Add(attributeModel);
            }

            var result = new GeneralResponseModel<IList<CustomerAttributeModel>>()
            {
                Data = model
            };

            return Ok(result);
        }

        [Route("api/customer/login")]
        [HttpPost]
        public virtual async Task<IActionResult> Login([FromBody] LoginQueryModel model)
        {
            var customerLoginModel = new LogInResponseModel();
            if (!_bsWebApiSettings.Enable)
                return customerLoginModel.GetHttpErrorResponse(ErrorType.NotOk, HttpStatusCode.Unauthorized.ToString());

            customerLoginModel.StatusCode = (int)ErrorType.NotOk;
            //await ValidationExtension.LoginValidator(ModelState, model, _localizationService, _customerSettings);
            if (ModelState.IsValid)
            {
                if (_customerSettings.UsernamesEnabled && model.Username != null)
                {
                    model.Username = model.Username.Trim();
                }
                var loginResult = await _customerRegistrationService.
                    ValidateCustomerAsync(_customerSettings.UsernamesEnabled ? model.Username : model.Email, model.Password);

                switch (loginResult)
                {

                    case CustomerLoginResults.Successful:
                        {
                            var customer = _customerSettings.UsernamesEnabled ?
                                await _customerService.GetCustomerByUsernameAsync(model.Username) : await _customerService.GetCustomerByEmailAsync(model.Email);
                            customerLoginModel = await _customerModelFactoryApi.PrepareCustomerLoginModel(customerLoginModel, customer);
                            customerLoginModel.StatusCode = (int)ErrorType.Ok;
                            //activity log
                            await _customerActivityService.InsertActivityAsync("PublicStore.Login",
                                await _localizationService.GetResourceAsync("ActivityLog.PublicStore.Login"), customer);
                            break;

                        }
                    case CustomerLoginResults.CustomerNotExist:
                        customerLoginModel.ErrorList = new List<string>
                        {
                            await _localizationService.GetResourceAsync("Account.Login.WrongCredentials.CustomerNotExist")
                        };
                        break;
                    case CustomerLoginResults.Deleted:

                        customerLoginModel.ErrorList = new List<string>
                        {
                            await _localizationService.GetResourceAsync("Account.Login.WrongCredentials.Deleted")
                        };
                        break;
                    case CustomerLoginResults.NotActive:

                        customerLoginModel.ErrorList = new List<string>
                        {
                            await _localizationService.GetResourceAsync("Account.Login.WrongCredentials.NotActive")
                        };
                        break;
                    case CustomerLoginResults.NotRegistered:

                        customerLoginModel.ErrorList = new List<string>
                        {
                            await _localizationService.GetResourceAsync("Account.Login.WrongCredentials.NotRegistered")
                        };
                        break;
                    case CustomerLoginResults.WrongPassword:
                    default:

                        customerLoginModel.ErrorList = new List<string>
                        {
                            await _localizationService.GetResourceAsync("Account.Login.WrongCredentials")
                        };
                        break;
                }
            }
            else
            {
                foreach (var state in ModelState)
                {
                    foreach (var error in state.Value.Errors)
                    {
                        customerLoginModel.ErrorList.Add(error.ErrorMessage);
                    }
                }
            }

            //If we got this far, something failed, redisplay form
            return (IActionResult)Ok(customerLoginModel);
        }

        [Route("api/customer/info")]
        [HttpGet]
        public virtual async Task<IActionResult> Info()
        {
            var model = new CustomerInfoResponseModel();
            var customer = await _workContext.GetCurrentCustomerAsync();
            if (!await _customerService.IsRegisteredAsync(customer))
            {
                return model.GetHttpErrorResponse(ErrorType.NotOk, HttpStatusCode.Unauthorized.ToString());
            }

            await _customerModelFactoryApi.PrepareCustomerInfoModel(model, customer, false);
            return Ok(model);
        }

        [HttpPost]
        [Route("api/customer/info")]
        public virtual async Task<IActionResult> Info([FromBody] CustomerInfoQueryModel queryModel)
        {
            var model = new CustomerInfoResponseModel();
            var customer = await _workContext.GetCurrentCustomerAsync();
            if (!await _customerService.IsRegisteredAsync(customer))
            {
                return model.GetHttpErrorResponse(ErrorType.NotOk, HttpStatusCode.Unauthorized.ToString());
            }

            model.ErrorList = new List<string>();
            if (!await _customerService.IsRegisteredAsync(customer))
                return model.GetHttpErrorResponse(ErrorType.NotOk, HttpStatusCode.Unauthorized.ToString());

            var form = queryModel.FormValues.ToNameValueCollection();

            //custom customer attributes
            var customerAttributesXml = await ParseCustomCustomerAttributes(form);
            var customerAttributeWarnings = await _customerAttributeParser.GetAttributeWarningsAsync(customerAttributesXml);
            foreach (var error in customerAttributeWarnings)
            {
                ModelState.AddModelError("", error);
            }

            //await ValidationExtension.CustomerInfoValidator(ModelState, queryModel, _localizationService, _stateProvinceService, _customerSettings);
            try
            {
                if (ModelState.IsValid)
                {
                    model = await _customerModelFactoryApi.PrepareCustomerInfoResponseModel(queryModel);
                    //username 
                    if (_customerSettings.UsernamesEnabled && this._customerSettings.AllowUsersToChangeUsernames)
                    {
                        if (
                            !customer.Username.Equals(model.Username.Trim(), StringComparison.InvariantCultureIgnoreCase))
                        {
                            //change username
                            await _customerRegistrationService.SetUsernameAsync(customer, model.Username.Trim());
                            //re-authenticate
                            await _authenticationService.SignInAsync(customer, true);
                        }
                    }
                    //email
                    if (!customer.Email.Equals(model.Email.Trim(), StringComparison.InvariantCultureIgnoreCase))
                    {
                        //change email
                        var requireValidation = _customerSettings.UserRegistrationType ==
                                                UserRegistrationType.EmailValidation;
                        //change email
                        await _customerRegistrationService.SetEmailAsync(customer, model.Email.Trim(), requireValidation);
                        //re-authenticate (if usernames are disabled)
                        if (!_customerSettings.UsernamesEnabled)
                        {
                            await _authenticationService.SignInAsync(customer, true);
                        }
                    }

                    //properties
                    if (_dateTimeSettings.AllowCustomersToSetTimeZone)
                    {
                        await _genericAttributeService.SaveAttributeAsync(customer, NopCustomerDefaults.TimeZoneIdAttribute, model.TimeZoneId);
                    }

                    //form fields
                    if (_customerSettings.GenderEnabled)
                        await _genericAttributeService.SaveAttributeAsync(customer, NopCustomerDefaults.GenderAttribute,
                            model.Gender);
                    await _genericAttributeService.SaveAttributeAsync(customer, NopCustomerDefaults.FirstNameAttribute,
                        model.FirstName);
                    await _genericAttributeService.SaveAttributeAsync(customer, NopCustomerDefaults.LastNameAttribute,
                        model.LastName);
                    if (_customerSettings.DateOfBirthEnabled)
                    {
                        DateTime? dateOfBirth = model.ParseDateOfBirth();
                        await _genericAttributeService.SaveAttributeAsync(customer, NopCustomerDefaults.DateOfBirthAttribute,
                            dateOfBirth);
                    }
                    if (_customerSettings.CompanyEnabled)
                        await _genericAttributeService.SaveAttributeAsync(customer, NopCustomerDefaults.CompanyAttribute,
                            model.Company);
                    if (_customerSettings.StreetAddressEnabled)
                        await _genericAttributeService.SaveAttributeAsync(customer, NopCustomerDefaults.StreetAddressAttribute,
                            model.StreetAddress);
                    if (_customerSettings.StreetAddress2Enabled)
                        await _genericAttributeService.SaveAttributeAsync(customer, NopCustomerDefaults.StreetAddress2Attribute,
                            model.StreetAddress2);
                    if (_customerSettings.ZipPostalCodeEnabled)
                        await _genericAttributeService.SaveAttributeAsync(customer, NopCustomerDefaults.ZipPostalCodeAttribute,
                            model.ZipPostalCode);
                    if (_customerSettings.CityEnabled)
                        await _genericAttributeService.SaveAttributeAsync(customer, NopCustomerDefaults.CityAttribute, model.City);
                    if (_customerSettings.CountyEnabled)
                        await _genericAttributeService.SaveAttributeAsync(customer, NopCustomerDefaults.CountyAttribute, model.County);
                    if (_customerSettings.CountryEnabled)
                        await _genericAttributeService.SaveAttributeAsync(customer, NopCustomerDefaults.CountryIdAttribute,
                            model.CountryId);
                    if (_customerSettings.CountryEnabled && _customerSettings.StateProvinceEnabled)
                        await _genericAttributeService.SaveAttributeAsync(customer, NopCustomerDefaults.StateProvinceIdAttribute,
                            model.StateProvinceId);
                    if (_customerSettings.PhoneEnabled)
                        await _genericAttributeService.SaveAttributeAsync(customer, NopCustomerDefaults.PhoneAttribute, model.Phone);
                    if (_customerSettings.FaxEnabled)
                        await _genericAttributeService.SaveAttributeAsync(customer, NopCustomerDefaults.FaxAttribute, model.Fax);

                    //save customer attributes
                    await _genericAttributeService.SaveAttributeAsync(customer,
                        NopCustomerDefaults.CustomCustomerAttributes, customerAttributesXml);

                    return Ok(model);
                }
                else
                {
                    foreach (var state in ModelState)
                    {
                        foreach (var error in state.Value.Errors)
                        {
                            model.ErrorList.Add(error.ErrorMessage);
                        }
                    }
                    model.StatusCode = (int)ErrorType.NotOk;
                    return Ok(model);
                }
            }
            catch (Exception exc)
            {
                // ModelState.AddModelError("", exc.Message);
                model.StatusCode = (int)ErrorType.NotOk;
                model.ErrorList.Add(exc.Message);
            }

            //If we got this far, something failed, redisplay form
            await _customerModelFactoryApi.PrepareCustomerInfoModel(model, customer, true, customerAttributesXml);
            return Ok(model);
        }

        [HttpPost]
        [Route("api/customer/changepass")]
        public virtual async Task<IActionResult> ChangePassword([FromBody] ChangePasswordQueryModel model)
        {
            var response = new GeneralResponseModel<string>();
            if (!await _customerService.IsRegisteredAsync(await _workContext.GetCurrentCustomerAsync()))
            {
                return response.GetHttpErrorResponse(ErrorType.NotOk, HttpStatusCode.Unauthorized.ToString());
            }

            var customer = await _workContext.GetCurrentCustomerAsync();
            if (ModelState.IsValid)
            {
                var changePasswordRequest = new ChangePasswordRequest(customer.Email,
                    true, _customerSettings.DefaultPasswordFormat, model.NewPassword, model.OldPassword);
                var changePasswordResult = await _customerRegistrationService.ChangePasswordAsync(changePasswordRequest);
                if (changePasswordResult.Success)
                {
                    response.Data = await _localizationService.GetResourceAsync("Account.ChangePassword.Success");
                    return Ok(response);
                }

                //errors
                foreach (var error in changePasswordResult.Errors)
                {
                    response.StatusCode = (int)ErrorType.NotOk;
                    response.ErrorList.Add(error);
                    return response.GetHttpErrorResponse(ErrorType.NotOk);
                }

            }
            else
            {
                foreach (var state in ModelState)
                {
                    foreach (var error in state.Value.Errors)
                    {
                        response.ErrorList.Add(error.ErrorMessage);
                    }
                }
                response.StatusCode = (int)ErrorType.NotOk;
                return response.GetHttpErrorResponse(ErrorType.NotOk);
            }


            //If we got this far, something failed, redisplay form
            return Ok(model);
        }

        [HttpPost]
        [Route("api/customer/eventchangepass")]
        public virtual async Task<IActionResult> EventChangePassword([FromBody] RegisterQueryModel model)
        {
            var response = new GeneralResponseModel<string>();

            if (!await _customerService.IsRegisteredAsync(await _customerService.GetCustomerByEmailAsync(model.Email)))
            {
                return response.GetHttpErrorResponse(ErrorType.NotOk, HttpStatusCode.Unauthorized.ToString());
            }

            var customerPassword = await _customerService.GetCurrentPasswordAsync(
                (await _customerService.GetCustomerByEmailAsync(model.Email)).Id);

            var passwordSalt = model.PasswordSalt ?? null;
            var changePasswordRequest = new ChangePasswordRequest(model.Email,
                                true, PasswordFormat.Clear, model.Password, customerPassword.Password, passwordSalt);

            var changePasswordResult = await ChangePasswordLocalAsync(changePasswordRequest);
            if (changePasswordResult.Success)
            {
                var customerNewPassword = await _customerService.GetCurrentPasswordAsync(customerPassword.CustomerId);

                customerNewPassword.PasswordFormat = _customerSettings.DefaultPasswordFormat;
                await _customerService.UpdateCustomerPasswordAsync(customerNewPassword);
                response.Data = await _localizationService.GetResourceAsync("Account.ChangePassword.Success");
                return Ok(response);
            }

            //If we got this far, something failed, redisplay form
            return Ok(model);
        }

        [HttpPost]
        [Route("api/customer/eventchangeemail")]
        public virtual async Task<IActionResult> EventChangeEmail([FromBody] CustomerInfoQueryModel model)
        {
            var response = new GeneralResponseModel<string>();

            var customer = await _customerService.GetCustomerByIdAsync(model.CustomerId);
            if (!await _customerService.IsRegisteredAsync(customer))
            {
                return response.GetHttpErrorResponse(ErrorType.NotOk, HttpStatusCode.Unauthorized.ToString());
            }

            if (customer != null)
            {
                var newEmail = model.Email.Trim();
                var oldEmail = customer.Email;

                if (!CommonHelper.IsValidEmail(newEmail))
                    return response.GetHttpErrorResponse(ErrorType.NotOk, await _localizationService.GetResourceAsync("Account.EmailUsernameErrors.NewEmailIsNotValid"));

                if (newEmail.Length > 100)
                    return response.GetHttpErrorResponse(ErrorType.NotOk, await _localizationService.GetResourceAsync("Account.EmailUsernameErrors.EmailTooLong"));

                var customer2 = await _customerService.GetCustomerByEmailAsync(newEmail);
                if (customer2 != null && customer.Id != customer2.Id)
                    return response.GetHttpErrorResponse(ErrorType.NotOk, await _localizationService.GetResourceAsync("Account.EmailUsernameErrors.EmailAlreadyExists"));

                var requireValidation = _customerSettings.UserRegistrationType == UserRegistrationType.EmailValidation;
                if (requireValidation)
                {
                    //re-validate email
                    customer.EmailToRevalidate = newEmail;
                    await _customerService.UpdateCustomerAsync(customer);

                    //email re-validation message
                    await _genericAttributeService.SaveAttributeAsync(customer, NopCustomerDefaults.EmailRevalidationTokenAttribute, Guid.NewGuid().ToString());
                    await _workflowMessageService.SendCustomerEmailRevalidationMessageAsync(customer, (await _workContext.GetWorkingLanguageAsync()).Id);
                }
                else
                {
                    customer.Email = newEmail;
                    await _customerService.UpdateCustomerAsync(customer);
                }

                return Ok(response);
            }
            else
            {
                response.ErrorList = new List<string>
                {
                    await _localizationService.GetResourceAsync("Account.Login.WrongCredentials.CustomerNotExist")
                };
            }

            //If we got this far, something failed, redisplay form
            return Ok(model);
        }

        #endregion
    }
}
