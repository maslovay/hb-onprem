using System;

namespace FillSlideShowDialogueService.Exceptions
{
    [Serializable]
    public class FillSlideShowDialogueException : Exception
    {
        public FillSlideShowDialogueException(string message, Exception innerException = null) : base(message, innerException)
        {
            
        }
    }
}