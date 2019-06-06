using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.IO;
using Microsoft.AspNetCore.Http;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.Extensions.Configuration;
using HBData.Models;
using HBData.Models.AccountViewModels;
using HBData;
using UserOperations.Services;
using UserOperations.Models.AnalyticModels;
using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using System.Net.Http;
using System.Net;
using Newtonsoft.Json;
using Microsoft.Extensions.DependencyInjection;
using UserOperations.Utils;
using Swashbuckle.AspNetCore.Annotations;
// using Microsoft.Extensions.Caching.Distributed;
using StackExchange.Redis;

namespace UserOperations.Utils
{
    public class RedisProvider
    {
        //   private readonly RecordsContext _context;
        //    private readonly IConfiguration _config;
        // private readonly IDistributedCache _distributedCache;

        //  public RedisProvider(RecordsContext context, IConfiguration config, IDistributedCache distributedCache)
        public RedisProvider()//(IDistributedCache distributedCache)
        {
            //      _context = context; 
            //     _config = config;
            // _distributedCache = distributedCache;
        }

        public string Get()
        {
            var redis = StackExchange.Redis.ConnectionMultiplexer.Connect("52.236.81.14:6379");
            IDatabase db = redis.GetDatabase();
            string value = db.StringGet("mykey");
            if (value == null)
            {
                value = "hello";
                db.StringSet("mykey", value);
            }
            return value;
        }
        public async Task<string> GetAsync()
        {
            var redis = StackExchange.Redis.ConnectionMultiplexer.Connect("52.236.81.14:6379");
            IDatabase db = redis.GetDatabase();
            string value = await db.StringGetAsync("mykey");
            if (value == null)
            {
                value = "abcdefg";
                await db.StringSetAsync("mykey", value);
            }
            return value;
        }
    }
}