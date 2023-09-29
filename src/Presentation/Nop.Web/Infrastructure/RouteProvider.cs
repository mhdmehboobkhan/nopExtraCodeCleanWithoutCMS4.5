using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Nop.Services.Installation;
using Nop.Web.Framework.Mvc.Routing;

namespace Nop.Web.Infrastructure
{
    /// <summary>
    /// Represents provider that provided basic routes
    /// </summary>
    public partial class RouteProvider : BaseRouteProvider, IRouteProvider
    {
        #region Methods

        /// <summary>
        /// Register routes
        /// </summary>
        /// <param name="endpointRouteBuilder">Route builder</param>
        public void RegisterRoutes(IEndpointRouteBuilder endpointRouteBuilder)
        {
            //get language pattern
            //it's not needed to use language pattern in AJAX requests and for actions returning the result directly (e.g. file to download),
            //use it only for URLs of pages that the user can go to
            var lang = GetLanguageRoutePattern();

            //areas
            endpointRouteBuilder.MapControllerRoute(name: "areaRoute",
                pattern: $"{{area:exists}}/{{controller=Home}}/{{action=Index}}/{{id?}}");

            //home page
            endpointRouteBuilder.MapControllerRoute(name: "Homepage", 
                pattern: $"{lang}",
                defaults: new { controller = "Home", action = "Index" });

            //login
            endpointRouteBuilder.MapControllerRoute(name: "Login",
                pattern: $"{lang}/login/",
                defaults: new { controller = "Customer", action = "Login" });

            // multi-factor verification digit code page
            endpointRouteBuilder.MapControllerRoute(name: "MultiFactorVerification",
                pattern: $"{lang}/multi-factor-verification/",
                defaults: new { controller = "Customer", action = "MultiFactorVerification" });

            //register
            endpointRouteBuilder.MapControllerRoute(name: "Register",
                pattern: $"{lang}/register/",
                defaults: new { controller = "Customer", action = "Register" });

            //logout
            endpointRouteBuilder.MapControllerRoute(name: "Logout",
                pattern: $"{lang}/logout/",
                defaults: new { controller = "Customer", action = "Logout" });

            //customer account links
            endpointRouteBuilder.MapControllerRoute("DashBoard", "customer/dashboard",
                new { controller = "Home", action = "DashBoard" });

            endpointRouteBuilder.MapControllerRoute(name: "CustomerInfo",
                pattern: $"{lang}/customer/info",
                defaults: new { controller = "Customer", action = "Info" });

            //contact us
            endpointRouteBuilder.MapControllerRoute(name: "ContactUs",
                pattern: $"{lang}/contactus",
                defaults: new { controller = "Common", action = "ContactUs" });

            //change language
            endpointRouteBuilder.MapControllerRoute(name: "ChangeLanguage",
                pattern: $"{lang}/changelanguage/{{langid:min(0)}}",
                defaults: new { controller = "Common", action = "SetLanguage" });

            //login page for checkout as guest
            endpointRouteBuilder.MapControllerRoute(name: "LoginCheckoutAsGuest",
                pattern: $"{lang}/login/checkoutasguest",
                defaults: new { controller = "Customer", action = "Login", checkoutAsGuest = true });

            //register result page
            endpointRouteBuilder.MapControllerRoute(name: "RegisterResult",
                pattern: $"{lang}/registerresult/{{resultId:min(0)}}",
                defaults: new { controller = "Customer", action = "RegisterResult" });

            //check username availability (AJAX)
            endpointRouteBuilder.MapControllerRoute(name: "CheckUsernameAvailability",
                pattern: $"customer/checkusernameavailability",
                defaults: new { controller = "Customer", action = "CheckUsernameAvailability" });

            //passwordrecovery
            endpointRouteBuilder.MapControllerRoute(name: "PasswordRecovery",
                pattern: $"{lang}/passwordrecovery",
                defaults: new { controller = "Customer", action = "PasswordRecovery" });

            //password recovery confirmation
            endpointRouteBuilder.MapControllerRoute(name: "PasswordRecoveryConfirm",
                pattern: $"{lang}/passwordrecovery/confirm",
                defaults: new { controller = "Customer", action = "PasswordRecoveryConfirm" });

            //topics (AJAX)
            endpointRouteBuilder.MapControllerRoute(name: "TopicPopup",
                pattern: $"t-popup/{{SystemName}}",
                defaults: new { controller = "Topic", action = "TopicDetailsPopup" });

            endpointRouteBuilder.MapControllerRoute(name: "CustomerChangePassword",
                pattern: $"{lang}/customer/changepassword",
                defaults: new { controller = "Customer", action = "ChangePassword" });

            endpointRouteBuilder.MapControllerRoute(name: "CustomerAvatar",
                pattern: $"{lang}/customer/avatar",
                defaults: new { controller = "Customer", action = "Avatar" });

            endpointRouteBuilder.MapControllerRoute(name: "AccountActivation",
                pattern: $"{lang}/customer/activation",
                defaults: new { controller = "Customer", action = "AccountActivation" });

            endpointRouteBuilder.MapControllerRoute(name: "EmailRevalidation",
                pattern: $"{lang}/customer/revalidateemail",
                defaults: new { controller = "Customer", action = "EmailRevalidation" });

            endpointRouteBuilder.MapControllerRoute(name: "CustomerAddressEdit",
                pattern: $"{lang}/customer/addressedit/{{addressId:min(0)}}",
                defaults: new { controller = "Customer", action = "AddressEdit" });

            endpointRouteBuilder.MapControllerRoute(name: "CustomerAddressAdd",
                pattern: $"{lang}/customer/addressadd",
                defaults: new { controller = "Customer", action = "AddressAdd" });

            endpointRouteBuilder.MapControllerRoute(name: "CustomerMultiFactorAuthenticationProviderConfig",
                pattern: $"{lang}/customer/providerconfig",
                defaults: new { controller = "Customer", action = "ConfigureMultiFactorAuthenticationProvider" });

            //customer multi-factor authentication settings 
            endpointRouteBuilder.MapControllerRoute(name: "MultiFactorAuthenticationSettings",
                pattern: $"{lang}/customer/multifactorauthentication",
                defaults: new { controller = "Customer", action = "MultiFactorAuthentication" });

            //get state list by country ID (AJAX)
            endpointRouteBuilder.MapControllerRoute(name: "GetStatesByCountryId", 
                pattern: $"country/getstatesbycountryid/",
                defaults: new { controller = "Country", action = "GetStatesByCountryId" });

            //EU Cookie law accept button handler (AJAX)
            endpointRouteBuilder.MapControllerRoute(name: "EuCookieLawAccept",
                pattern: $"eucookielawaccept",
                defaults: new { controller = "Common", action = "EuCookieLawAccept" });

            //authenticate topic (AJAX)
            endpointRouteBuilder.MapControllerRoute(name: "TopicAuthenticate",
                pattern: $"topic/authenticate",
                defaults: new { controller = "Topic", action = "Authenticate" });

            //topic watch (AJAX)
            endpointRouteBuilder.MapControllerRoute(name: "TopicWatch",
                pattern: $"boards/topicwatch/{{id:min(0)}}",
                defaults: new { controller = "Boards", action = "TopicWatch" });

            endpointRouteBuilder.MapControllerRoute(name: "TopicSlug",
                pattern: $"{lang}/boards/topic/{{id:min(0)}}/{{slug?}}",
                defaults: new { controller = "Boards", action = "Topic" });

            endpointRouteBuilder.MapControllerRoute(name: "TopicSlugPaged",
                pattern: $"{lang}/boards/topic/{{id:min(0)}}/{{slug?}}/page/{{pageNumber:int}}",
                defaults: new { controller = "Boards", action = "Topic" });

            //robots.txt (file result)
            endpointRouteBuilder.MapControllerRoute(name: "robots.txt",
                pattern: $"robots.txt",
                defaults: new { controller = "Common", action = "RobotsTextFile" });

            //sitemap
            endpointRouteBuilder.MapControllerRoute(name: "Sitemap",
                pattern: $"{lang}/sitemap",
                defaults: new { controller = "Common", action = "Sitemap" });

            //sitemap.xml (file result)
            endpointRouteBuilder.MapControllerRoute(name: "sitemap.xml",
                pattern: $"sitemap.xml",
                defaults: new { controller = "Common", action = "SitemapXml" });

            endpointRouteBuilder.MapControllerRoute(name: "sitemap-indexed.xml",
                pattern: $"sitemap-{{Id:min(0)}}.xml",
                defaults: new { controller = "Common", action = "SitemapXml" });

            //store closed
            endpointRouteBuilder.MapControllerRoute(name: "StoreClosed",
                pattern: $"{lang}/storeclosed",
                defaults: new { controller = "Common", action = "StoreClosed" });

            //install
            endpointRouteBuilder.MapControllerRoute(name: "Installation",
                pattern: $"{NopInstallationDefaults.InstallPath}",
                defaults: new { controller = "Install", action = "Index" });

            //error page
            endpointRouteBuilder.MapControllerRoute(name: "Error",
                pattern: $"error",
                defaults: new { controller = "Common", action = "Error" });

            //page not found
            endpointRouteBuilder.MapControllerRoute(name: "PageNotFound",
                pattern: $"{lang}/page-not-found",
                defaults: new { controller = "Common", action = "PageNotFound" });
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets a priority of route provider
        /// </summary>
        public int Priority => 0;

        #endregion
    }
}