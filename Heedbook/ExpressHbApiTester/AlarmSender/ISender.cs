namespace AlarmSender
{
    public interface ISender
    {
        void Send(string message, bool processCallback = true);

        void ReceiveCommands();
    }
}