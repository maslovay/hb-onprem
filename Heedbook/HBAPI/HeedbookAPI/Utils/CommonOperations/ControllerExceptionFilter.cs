using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System;
using System.Collections.Generic;
using System.Linq;

namespace UserOperations.Utils
{
    public class ControllerExceptionFilter : ExceptionFilterAttribute
    {

        public override void OnException(ExceptionContext context)
        {
            var code = 400;
            switch (context.Exception)
            {
                case UnauthorizedAccessException unauthorizedAccess:
                    code = 401; break;//Unauthorized 
                case AccessException access:
                    code = 403; break;//Forbidden
                case NoFoundException notFound:
                    code = 400; break;
                case NotUniqueException notUnique:
                    code = 400; break;
                case NoDataException noData:
                    code = 400; break;
                default:
                    code = 400; break;
            }

            context.Result = new ObjectResult(
                new ErrorsResult
                {
                    Code = code,
                    Message = context.Exception.Message
                })
                {
                    StatusCode = code
                };

            base.OnException(context);
        }
    }

    class ErrorsResult
    {
        public int Code { get; set; }
        public string Message { get; set; }
        public Dictionary<string, string> Errors { get; set; }
    }

    class Error
    {
        public string Field { get; set; }
        public string Message { get; set; }
    }

    public class NoFoundException : Exception
    {
        public NoFoundException(string message, Exception innerException = null) : base(message, innerException) {   }
    }

    public class AccessException : Exception
    {
        public AccessException(string message, Exception innerException = null) : base(message, innerException) { }
    }

    public class NotUniqueException : Exception
    {
        public NotUniqueException(string message, Exception innerException = null) : base(message, innerException) { }
        public NotUniqueException(Exception innerException = null) : base("Not unique", innerException) { }
    }

    public class NoDataException : Exception
    {
        public NoDataException(string message, Exception innerException = null) : base(message, innerException) { }
        public NoDataException(Exception innerException = null) : base("No data", innerException) { }
    }
}