using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace Nop.Plugin.Misc.ZsWebApi.Extensions
{
    public enum ErrorType
    {
        Ok = 200,
        NotOk = 400,
        AuthenticationError = 600
    }
}
