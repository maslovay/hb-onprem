namespace MessengerReporterService.Models
{
    public class AlarmSenderChat
    {
        public string Name { get; set; }

        public AlarmSenderChat(string name) 
            => Name = name;
    }
}