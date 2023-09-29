using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nop.Plugin.Misc.ZsWebApi.Models.Customer
{
    public class LoginQueryModel
    {
        public string Username { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
    }

    public record LogInResponseModel : BaseResponse
    {
        public LogInResponseModel()
        {
        }
        public int CustomerId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string Username { get; set; }
        public string StreetAddress { get; set; }
        public string StreetAddress2 { get; set; }
        public string City { get; set; }
        public string Phone { get; set; }
        public int CountryId { get; set; }
        public int StateProvinceId { get; set; }
        public string Token { get; set; }
    }
}
