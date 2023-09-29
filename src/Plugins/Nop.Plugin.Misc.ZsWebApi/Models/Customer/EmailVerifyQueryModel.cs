using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using Nop.Web.Framework;
using Nop.Web.Framework.Mvc;

namespace Nop.Plugin.Misc.ZsWebApi.Models.Customer
{

    public partial class EmailVerifyQueryModel
    {
        public EmailVerifyQueryModel()
        {
        }

        public string Email { get; set; }
    }
}