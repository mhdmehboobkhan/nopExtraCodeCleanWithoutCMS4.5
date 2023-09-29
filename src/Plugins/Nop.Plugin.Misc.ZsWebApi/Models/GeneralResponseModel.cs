using System;
using System.Collections.Generic;
using System.Text;

namespace Nop.Plugin.Misc.ZsWebApi.Models
{
    //ErrorCode=0 no error
    //ErrorCode=1 authentication error 
    //ErrorCode=2 Error message Show From api 
    //ErrorCode=3 unknown error 
    public record GeneralResponseModel<TResult> : BaseResponse
    {
        public TResult Data { get; set; }
    }
}
