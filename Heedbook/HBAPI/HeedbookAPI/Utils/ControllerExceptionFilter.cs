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
                //case AccessException access:
                ////    code = 403; break;//Forbidden
                case NoFoundException notFound:
                    code = 400; break;
                //case DeletedException deleted:
                //    code = 410; break;//Gone
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
        public NoFoundException(string message, Exception innerException = null) : base(message, innerException)
        {

        }
    }
}