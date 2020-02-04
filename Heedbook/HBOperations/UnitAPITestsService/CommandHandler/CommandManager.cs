using System;
using UnitAPITestsService.Tasks;
using Microsoft.Extensions.Configuration;
using NLog;

namespace UnitAPITestsService.CommandHandler
{
    public class CommandManager
    {
        private readonly Checker _checker;
        public CommandManager(
            Checker checker)
        {
            _checker = checker;
        }

        public void RunCommand(string command)
            => CommandWorker(command);
        
        private void CommandWorker(string command)
        {
            Console.WriteLine($"CommandWorker(): command '{command}'");
            switch (command.Trim())
            {
                case "api_unit_tests":
                    Console.WriteLine($"CommandWorker(): running {command}");
                    _checker.Check();
                    break;
            }
        }
    }
}