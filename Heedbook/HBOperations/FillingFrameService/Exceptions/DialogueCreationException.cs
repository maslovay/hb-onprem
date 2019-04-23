using System;

namespace FillingFrameService.Exceptions
{
    [Serializable]
    public class DialogueCreationException : Exception
    {
        public DialogueCreationException(string message, Exception innerException) : base(message, innerException)
        {
            
        }
    }
}