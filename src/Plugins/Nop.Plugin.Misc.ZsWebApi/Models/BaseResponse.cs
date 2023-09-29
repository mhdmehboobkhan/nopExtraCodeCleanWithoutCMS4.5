using Microsoft.AspNetCore.Mvc;
using Nop.Plugin.Misc.ZsWebApi.Extensions;
using System;
using System.Collections.Generic;
using System.Text;

namespace Nop.Plugin.Misc.ZsWebApi.Models
{
    public record BaseResponse
    {
        public BaseResponse()
        {
            StatusCode = (int)ErrorType.Ok;
            ErrorList = new List<string>();
        }

        public string SuccessMessage { get; set; }
        
        public int StatusCode { get; set; }

        public List<string> ErrorList { get; set; }

        public JsonResult GetHttpErrorResponse(ErrorType errorType, string msg)
        {
            // Added the status-code properly without changing the previous response model of the API
            this.ErrorList.Add(msg);
            this.StatusCode = (int)errorType;
            return new JsonResult(this)
            {
                //StatusCode = (int)errorType
                // The APP does not have necessary code to handle not-200 HTTP responses
                // Hence, making all the responses to have 200 status even though these were caused by some erroneous requests
                StatusCode = (int)ErrorType.Ok
            };
        }

        public JsonResult GetHttpErrorResponse(ErrorType errorType)
        {
            // Added the status-code properly without changing the previous response model of the API
            this.StatusCode = (int)errorType;
            return new JsonResult(this)
            {
                //StatusCode = (int)errorType
                // The APP does not have necessary code to handle not-200 HTTP responses
                // Hence, making all the responses to have 200 status even though these were caused by some erroneous requests
                StatusCode = (int)ErrorType.Ok
            };
        }
    }
}