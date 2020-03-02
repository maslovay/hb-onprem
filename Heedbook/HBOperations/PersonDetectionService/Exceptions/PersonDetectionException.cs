using System;

namespace PersonDetectionService.Exceptions
{
    [Serializable]
    public class PersonDetectionException : Exception
    {
        public PersonDetectionException(string message, Exception innerException = null) : base(message, innerException)
        {
            
        }
    }
}