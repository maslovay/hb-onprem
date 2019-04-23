using System;
using System.Configuration;
using System.Linq;
using System.Threading.Tasks;
using Common;
using FillingSatisfactionService.Helper;
using HBData;
using HBData.Models;
using HBData.Repository;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using NUnit.Framework.Internal;

namespace FillingSatisfactionService.Tests
{
    public class FillingSatisfactionServiceTests : ServiceTest
    {
        private IGenericRepository _repository;
        private FillingSatisfaction _fillingSatisfactionService;
        private Startup startup;
        
        
        [SetUp]
        public void Setup()
        {
            base.Setup(() =>
            {
                startup = new Startup(Config);
                startup.ConfigureServices(Services);
            });
        }

        protected override void InitServices()
        {
            _repository = ServiceProvider.GetService<IGenericRepository>();
            _fillingSatisfactionService = ServiceProvider.GetService<FillingSatisfaction>();
        }

        [Test]
        public async Task EnsureCreatesSatisfactionRecord()
        {
            var dialog = _repository.Get<Dialogue>().FirstOrDefault();

            var satisfaction = _repository.Get<DialogueClientSatisfaction>()
                .FirstOrDefault(s => s.DialogueId == dialog.DialogueId);

            if (satisfaction != null)
            {
                _repository.Delete(satisfaction);
                _repository.Save();
            }

            await _fillingSatisfactionService.Run(dialog.DialogueId);        
            
            Assert.IsTrue(_repository.Get<DialogueClientSatisfaction>()
                .Any(s => s.DialogueId == dialog.DialogueId));
        }
    }
}