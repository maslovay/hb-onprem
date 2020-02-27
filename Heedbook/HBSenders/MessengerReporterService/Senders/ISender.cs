namespace MessengerReporterService.Senders
{
    public interface ISender
    {
        void Send(string message, string chatName, bool processCallback = true);

        void ReceiveCommands();
    }
}