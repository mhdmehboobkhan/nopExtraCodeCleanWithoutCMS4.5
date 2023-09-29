using System;
using System.Collections.Generic;
using Nop.Plugin.Misc.ZsWebApi.Models.Common;

namespace Nop.Plugin.Misc.ZsWebApi.Models.Customer
{
    public partial record CustomerInfoQueryModel : BaseResponse
    {
        public CustomerInfoQueryModel()
        {
           FormValues= new List<KeyValueApi>();
        }

        public int CustomerId { get; set; }
        public string Email { get; set; }
      
        public string Username { get; set; }

        public string Gender { get; set; }

        public string FirstName { get; set; }
       
        public string LastName { get; set; }
       
        public int? DateOfBirthDay { get; set; }
     
        public int? DateOfBirthMonth { get; set; }
     
        public int? DateOfBirthYear { get; set; }

        public DateTime? ParseDateOfBirth()
        {
            if (!DateOfBirthYear.HasValue || !DateOfBirthMonth.HasValue || !DateOfBirthDay.HasValue)
                return null;

            DateTime? dateOfBirth = null;
            try
            {
                dateOfBirth = new DateTime(DateOfBirthYear.Value, DateOfBirthMonth.Value, DateOfBirthDay.Value);
            }
            catch { }
            return dateOfBirth;
        }

        public string Company { get; set; }

        public string StreetAddress { get; set; }

        public string StreetAddress2 { get; set; }

        public string ZipPostalCode { get; set; }

        public string County { get; set; }

        public string City { get; set; }

        public int CountryId { get; set; }
      
        public int StateProvinceId { get; set; }
       
        public string Phone { get; set; }

        public string Fax { get; set; }
        
        public List<KeyValueApi> FormValues { get; set; }
    }
}