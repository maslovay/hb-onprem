using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common;
using Microsoft.Extensions.Configuration;
using NUnit.Framework;

namespace MemoryCacheService.Tests
{
    public class Tests : ServiceTest
    {
        private readonly Dictionary<Guid,FirstTestType> testValues1 
            = new Dictionary<Guid, FirstTestType>();

        private readonly Dictionary<Guid, SecondTestType> testValues2 
            = new Dictionary<Guid, SecondTestType>();
        
        private IMemoryCache memCache;
        
        [SetUp]
        public async Task Setup()
        {
            await base.Setup(() => {}, true);
            
            var memDbHost = Config.GetSection("MemoryCacheDb").GetValue<string>("Host");
            var memDbPort = Config.GetSection("MemoryCacheDb").GetValue<int>("Port");
            var memAllowAdmin = Config.GetSection("MemoryCacheDb").GetValue<bool>("AllowAdmin");
            var connString = $"{memDbHost}:{memDbPort}, allowAdmin:{memAllowAdmin}";

            memCache = new RedisMemoryCache(connString);
            memCache.Clear();
        }

        [Test]
        public async Task CheckAllDataEnqueued()
        {
            FillDatabase();

            Assert.AreEqual(memCache.Count(), testValues1.Count() + testValues2.Count());
        }


        [Test]
        public async Task CheckDataDequeue()
        {
            FillDatabase();
            int originalCount = memCache.Count();

            var kvp1 = memCache.Dequeue<FirstTestType>();
            Assert.IsFalse(kvp1.Key == Guid.Empty);
            
            var kvp2 = memCache.Dequeue<SecondTestType>();
            Assert.IsFalse(kvp2.Key == Guid.Empty);
            
            Assert.AreEqual( memCache.Count(), originalCount - 2 );
        }

        
        [Test]
        public async Task CheckDataDequeueWithCondition()
        {
            var originalCount = testValues1.Count(x => x.Value.Status >= 5);
            
            FillDatabase();

            var dequeued = memCache.Dequeue<FirstTestType>(x => x.Status >= 5);
            
            Assert.AreNotEqual(dequeued.Key, Guid.Empty);
        }

        
        protected override async Task PrepareTestData()
        {
            var rand = new Random(DateTime.Now.Millisecond);
            
            for ( var i = 0; i < 100; ++i )
                testValues1[Guid.NewGuid()] = new FirstTestType() {  Id = Guid.NewGuid(), Status = rand.Next() % 10};
            
            
            for ( var i = 0; i < 150; ++i )
                testValues2[Guid.NewGuid()] = new SecondTestType() {  Id = Guid.NewGuid(), Name = $"Name {i}"};
        }

        protected override async Task CleanTestData()
        {
            memCache.Clear();
        }

        protected override void InitServices()
        {
        }
        
        private void FillDatabase()
        {
            memCache.Clear();
            
            foreach (var key in testValues1.Keys)
                memCache.Enqueue(key, testValues1[key]);

            foreach (var key in testValues2.Keys)
                memCache.Enqueue(key, testValues2[key]);
        }
    }
}