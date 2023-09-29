using JWT;
using Microsoft.IdentityModel.Tokens;
using Nop.Core;
using Nop.Core.Domain.Customers;
using Nop.Plugin.Misc.ZsWebApi.Extensions;
using Nop.Plugin.Misc.ZsWebApi.Infrastructure;
using Nop.Services.Common;
using Nop.Services.Customers;
using Nop.Services.Logging;
using Nop.Services.Stores;
using Nop.Web.Framework.Controllers;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Text;

namespace Nop.Plugin.Misc.ZsWebApi.Controllers
{
    [NstAuthorization]
    [TokenAuthorize]
    [CustomerLastActivityAuthorize]
    public class BaseApiController : BaseController
    {
    }
}
