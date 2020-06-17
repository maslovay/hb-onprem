using System;

namespace PersonDetectionAkBarsService.Models
{
    public class AkBarsRegistrationResponceModel
    {
        public AkBarsNewUserResult result { get; set; }
        public Boolean success { get; set; }
        public string message { get; set; }
    }
    public class AkBarsNewUserResult
    {
        public string customerToken { get; set; }
        public Boolean isNewUser { get; set; }
    }
}