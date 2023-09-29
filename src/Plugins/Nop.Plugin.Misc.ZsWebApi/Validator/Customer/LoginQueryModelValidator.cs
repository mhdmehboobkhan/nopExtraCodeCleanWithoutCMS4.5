using FluentValidation;
using Nop.Core.Domain.Customers;
using Nop.Plugin.Misc.ZsWebApi.Models.Customer;
using Nop.Services.Localization;
using Nop.Web.Framework.Validators;

namespace Nop.Plugin.Misc.ZsWebApi.Validator.Customer
{
    public partial class LoginQueryModelValidator : BaseNopValidator<LoginQueryModel>
    {
        public LoginQueryModelValidator(ILocalizationService localizationService, CustomerSettings customerSettings)
        {
            if (!customerSettings.UsernamesEnabled)
            {
                //login by email
                RuleFor(x => x.Email).NotEmpty().WithMessageAwait(localizationService.GetResourceAsync("Account.Login.Fields.Email.Required"));
                RuleFor(x => x.Email).EmailAddress().WithMessageAwait(localizationService.GetResourceAsync("Common.WrongEmail"));
            }
        }
    }
}