using Microsoft.AspNetCore.Mvc.ModelBinding;
using Nop.Core.Domain.Common;
using Nop.Core.Domain.Customers;
using Nop.Plugin.Misc.ZsWebApi.Models;
using Nop.Plugin.Misc.ZsWebApi.Models.Customer;
using Nop.Services.Directory;
using Nop.Services.Localization;
using NUglify.Helpers;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Nop.Plugin.Misc.ZsWebApi.Extensions
{
    public static class ValidationExtension
    {
        #region Utility

        private static bool IsNotValidEmail(this string strIn)
        {
            // Return true if strIn is in valid e-mail format.
            return !string.IsNullOrEmpty(strIn) && !Regex.IsMatch(strIn, @"^([\w-\.]+)@((\[[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.)|(([\w-]+\.)+))([a-zA-Z]{2,4}|[0-9]{1,3})(\]?)$");
        }

        private static bool IsNull(this object value)
        {
            return value == null;
        }

        private static bool IsNotRightLength(this string value, int min, int max)
        {
            if (value == null)
                return false;

            return !(value.Length >= min && value.Length <= max);
        }

        private static bool IsNotEqual(this string value, string checkValue)
        {
            if (checkValue == null)
                return false;

            return !value.Equals(checkValue);
        }

        private static void WithMessage(this bool flag, ModelStateDictionary modelState, string message)
        {
            if (flag == true)
            {
                modelState.AddModelError("", message);
            }
        }

        #endregion

        #region Methods

        public static async Task RegisterValidator(ModelStateDictionary modelState, RegisterQueryModel model, 
            ILocalizationService localizationService, IStateProvinceService stateProvinceService, CustomerSettings customerSettings)
        {
            model.FirstName.IsNullOrWhiteSpace()
                .WithMessage(modelState, await localizationService.GetResourceAsync("Address.Fields.FirstName.Required"));
            model.LastName.IsNullOrWhiteSpace()
                .WithMessage(modelState, await localizationService.GetResourceAsync("Address.Fields.LastName.Required"));
            model.Email.IsNullOrWhiteSpace().
                WithMessage(modelState, await localizationService.GetResourceAsync("Address.Fields.Email.Required"));
            model.Email.IsNotValidEmail().
                WithMessage(modelState, await localizationService.GetResourceAsync("Common.WrongEmail"));

            if (customerSettings.UsernamesEnabled && customerSettings.AllowUsersToChangeUsernames)
            {
                model.Username.IsNullOrWhiteSpace().WithMessage(modelState, await localizationService.GetResourceAsync("Account.Fields.Username.Required"));
            }


            model.Password.IsNullOrWhiteSpace().WithMessage(modelState, await localizationService.GetResourceAsync("Account.Fields.Password.Required"));
            model.Password.IsNotRightLength(customerSettings.PasswordMinLength, 999).WithMessage(modelState, string.Format(await localizationService.GetResourceAsync("Account.Fields.Password.LengthValidation"), customerSettings.PasswordMinLength));
            model.ConfirmPassword.IsNullOrWhiteSpace().WithMessage(modelState, await localizationService.GetResourceAsync("Account.Fields.ConfirmPassword.Required"));
            model.ConfirmPassword.IsNotEqual(model.Password).WithMessage(modelState, await localizationService.GetResourceAsync("Account.Fields.Password.EnteredPasswordsDoNotMatch"));

            if (customerSettings.CountryEnabled && customerSettings.CountryRequired)
            {
                model.CountryId.Equals(0)
                    .WithMessage(modelState, await localizationService.GetResourceAsync("Address.Fields.Country.Required"));
            }
            if (customerSettings.CountryEnabled &&
                customerSettings.StateProvinceEnabled &&
                customerSettings.StateProvinceRequired)
            {

                var hasStates = (await stateProvinceService.GetStateProvincesByCountryIdAsync(model.CountryId)).Count > 0;
                if (hasStates)
                {
                    //if yes, then ensure that state is selected
                    if (model.StateProvinceId == 0)
                    {
                        modelState.AddModelError("StateProvinceId", await localizationService.GetResourceAsync("Address.Fields.StateProvince.Required"));
                    }
                }
            }
            if (customerSettings.DateOfBirthRequired && customerSettings.DateOfBirthEnabled)
            {
                var dateOfBirth = model.ParseDateOfBirth();
                if (dateOfBirth == null)
                {
                    modelState.AddModelError("", await localizationService.GetResourceAsync("Account.Fields.DateOfBirth.Required"));
                }

            }

            if (customerSettings.CompanyRequired && customerSettings.CompanyEnabled)
            {
                model.Company.IsNullOrWhiteSpace()
                    .WithMessage(modelState, await localizationService.GetResourceAsync("Address.Fields.Company.Required"));
            }
            if (customerSettings.StreetAddressRequired && customerSettings.StreetAddressEnabled)
            {
                model.StreetAddress.IsNullOrWhiteSpace().
                    WithMessage(modelState, await localizationService.GetResourceAsync("Address.Fields.StreetAddress.Required"));

            }
            if (customerSettings.StreetAddress2Required && customerSettings.StreetAddress2Enabled)
            {
                model.StreetAddress2.IsNullOrWhiteSpace().WithMessage(modelState, await localizationService.GetResourceAsync("Address.Fields.StreetAddress2.Required"));

            }
            if (customerSettings.ZipPostalCodeRequired && customerSettings.ZipPostalCodeEnabled)
            {
                model.ZipPostalCode.IsNullOrWhiteSpace().WithMessage(modelState, await localizationService.GetResourceAsync("Address.Fields.ZipPostalCode.Required"));
            }
            if (customerSettings.CityRequired && customerSettings.CityEnabled)
            {
                model.City.IsNullOrWhiteSpace().WithMessage(modelState, await localizationService.GetResourceAsync("Address.Fields.City.Required"));
            }
            if (customerSettings.CountyRequired && customerSettings.CountyEnabled)
            {
                model.County.IsNullOrWhiteSpace().WithMessage(modelState, await localizationService.GetResourceAsync("Account.Fields.County.Required"));
            }
            if (customerSettings.PhoneRequired && customerSettings.PhoneEnabled)
            {
                model.Phone.IsNullOrWhiteSpace().WithMessage(modelState, await localizationService.GetResourceAsync("Address.Fields.Phone.Required"));
            }
            if (customerSettings.FaxRequired && customerSettings.FaxEnabled)
            {
                model.Fax.IsNullOrWhiteSpace().WithMessage(modelState, await localizationService.GetResourceAsync("Address.Fields.Fax.Required"));
            }
        }

        public static async Task LoginValidator(ModelStateDictionary modelState, LoginQueryModel model, ILocalizationService localizationService,
            CustomerSettings customerSettings)
        {
            if (!customerSettings.UsernamesEnabled)
            {
                //login by email
                model.Email.IsNullOrWhiteSpace().WithMessage(modelState, await localizationService.GetResourceAsync("Account.Login.Fields.Email.Required"));
                model.Email.IsNotValidEmail().WithMessage(modelState, await localizationService.GetResourceAsync("Common.WrongEmail"));
            }
        }

        public static async Task CustomerInfoValidator(ModelStateDictionary modelState, CustomerInfoQueryModel model, ILocalizationService localizationService,
            IStateProvinceService stateProvinceService,
            CustomerSettings customerSettings)
        {
            model.FirstName.IsNullOrWhiteSpace()
                .WithMessage(modelState, await localizationService.GetResourceAsync("Address.Fields.FirstName.Required"));
            model.LastName.IsNullOrWhiteSpace()
                .WithMessage(modelState, await localizationService.GetResourceAsync("Address.Fields.LastName.Required"));
            model.Email.IsNullOrWhiteSpace().
                WithMessage(modelState, await localizationService.GetResourceAsync("Address.Fields.Email.Required"));
            model.Email.IsNotValidEmail().
                WithMessage(modelState, await localizationService.GetResourceAsync("Common.WrongEmail"));

            if (customerSettings.UsernamesEnabled && customerSettings.AllowUsersToChangeUsernames)
            {
                model.Username.IsNullOrWhiteSpace().WithMessage(modelState, await localizationService.GetResourceAsync("Account.Fields.Username.Required"));
            }

            //form fields
            if (customerSettings.CountryEnabled && customerSettings.CountryRequired)
            {
                model.CountryId.Equals(0)
                    .WithMessage(modelState, await localizationService.GetResourceAsync("Address.Fields.Country.Required"));
            }
            if (customerSettings.CountryEnabled &&
                customerSettings.StateProvinceEnabled &&
                customerSettings.StateProvinceRequired)
            {

                var hasStates = (await stateProvinceService.GetStateProvincesByCountryIdAsync(model.CountryId)).Count > 0;

                if (hasStates)
                {
                    //if yes, then ensure that state is selected
                    if (model.StateProvinceId == 0)
                    {
                        modelState.AddModelError("StateProvinceId", await localizationService.GetResourceAsync("Address.Fields.StateProvince.Required"));
                    }
                }
            }
            if (customerSettings.DateOfBirthRequired && customerSettings.DateOfBirthEnabled)
            {
                var dateOfBirth = model.ParseDateOfBirth();
                if (dateOfBirth == null)
                {
                    modelState.AddModelError("", await localizationService.GetResourceAsync("Account.Fields.DateOfBirth.Required"));
                }
            }

            if (customerSettings.CompanyRequired && customerSettings.CompanyEnabled)
            {
                model.Company.IsNullOrWhiteSpace()
                    .WithMessage(modelState, await localizationService.GetResourceAsync("Address.Fields.Company.Required"));
            }
            if (customerSettings.StreetAddressRequired && customerSettings.StreetAddressEnabled)
            {
                model.StreetAddress.IsNullOrWhiteSpace().
                    WithMessage(modelState, await localizationService.GetResourceAsync("Address.Fields.StreetAddress.Required"));
            }
            if (customerSettings.StreetAddress2Required && customerSettings.StreetAddress2Enabled)
            {
                model.StreetAddress2.IsNullOrWhiteSpace().WithMessage(modelState, await localizationService.GetResourceAsync("Address.Fields.StreetAddress2.Required"));
            }
            if (customerSettings.ZipPostalCodeRequired && customerSettings.ZipPostalCodeEnabled)
            {
                model.ZipPostalCode.IsNullOrWhiteSpace().WithMessage(modelState, await localizationService.GetResourceAsync("Address.Fields.ZipPostalCode.Required"));
            }
            if (customerSettings.CityRequired && customerSettings.CityEnabled)
            {
                model.City.IsNullOrWhiteSpace().WithMessage(modelState, await localizationService.GetResourceAsync("Address.Fields.City.Required"));
            }
            if (customerSettings.PhoneRequired && customerSettings.PhoneEnabled)
            {
                model.Phone.IsNullOrWhiteSpace().WithMessage(modelState, await localizationService.GetResourceAsync("Address.Fields.Phone.Required"));
            }
            if (customerSettings.FaxRequired && customerSettings.FaxEnabled)
            {
                model.Fax.IsNullOrWhiteSpace().WithMessage(modelState, await localizationService.GetResourceAsync("Address.Fields.Fax.Required"));
            }
        }
        
        #endregion
    }
}
