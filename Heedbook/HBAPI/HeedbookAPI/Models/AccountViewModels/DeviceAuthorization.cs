using System;
using System.ComponentModel.DataAnnotations;
using Swashbuckle.AspNetCore.Annotations;

namespace UserOperations.AccountModels
{
    [SwaggerTag("Data for creating JWT token on device")]
    public class DeviceAuthorization
    {
        public String DeviceName { get; set; }

        public String Code { get; set; }
    }
}