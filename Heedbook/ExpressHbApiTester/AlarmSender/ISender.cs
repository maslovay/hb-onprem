namespace AlarmSender
{
    public interface ISender
    {
        void Send(string message);

        void ReceiveCommands();
    }
}