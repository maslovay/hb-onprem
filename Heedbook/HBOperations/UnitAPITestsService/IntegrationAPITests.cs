using System;
using System.Threading.Tasks;
using HBLib;
using UnitAPITestsService.CommandHandler;
using RabbitMqEventBus;

namespace UnitAPITestsService
{
    public class UnitTests
    {
        private readonly CommandManager _commandManager;
        public UnitTests(CommandManager commandManager)
        {
            _commandManager = commandManager;
        }
        public async Task Run(String command)
        {
            try
            {
                if(command != null)
                {
                    _commandManager.RunCommand(command);
                }
            }
            catch (Exception e)
            {
                System.Console.WriteLine($"exception: {e}");
            }
        }
    }
}