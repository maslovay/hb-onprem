using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System;

namespace UserOperations.Controllers
{
    public class ControllerExceptionFilter : ExceptionFilterAttribute
    {
        public ControllerExceptionFilter()
        {
        }

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

            context.Result = context.Result = new ObjectResult(context.Exception.Message)
            {
                StatusCode = code
            };

            base.OnException(context);
        }
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