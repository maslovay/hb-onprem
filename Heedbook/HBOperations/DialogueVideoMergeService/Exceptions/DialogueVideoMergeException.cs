using System;

namespace DialogueVideoMergeService.Exceptions
{
    [Serializable]
    public class DialogueVideoMergeException : Exception
    {
        public DialogueVideoMergeException(string message, Exception ex) : base(message, ex)
        {
            
        }
    }
}