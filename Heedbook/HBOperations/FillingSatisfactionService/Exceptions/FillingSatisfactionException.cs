using System;


namespace FillingSatisfactionService.Exceptions
{
    [Serializable]
    public class FillingSatisfactionException : Exception
    {
        public FillingSatisfactionException(string message, Exception innerException) : base(message, innerException)
        {
            
        }
    }
}