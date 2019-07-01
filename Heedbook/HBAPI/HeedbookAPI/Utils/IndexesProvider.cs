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
using Npgsql;

namespace UserOperations.Utils
{
    public class IndexesProvider
    {
         private readonly IConfiguration _config;
        public IndexesProvider(IConfiguration config)
        {
            _config = config;
        }

        public void GetData(Guid companyId)
        {
            string connectionString = _config["ConnectionStrings:DefaultConnection"];
            NpgsqlConnection connection = new NpgsqlConnection(connectionString);
            try
            {
                connection.Open();
                var LoadIndexAvgBranch = GetLoadIndex("LoadIndexAvgBranch", companyId, connection);                
                var LoadIndexMaxBranch = GetLoadIndex("LoadIndexMaxBranch", companyId, connection);
                var loadindexavgtotal = GetLoadIndex("loadindexavgtotal", companyId, connection);
                Console.WriteLine(LoadIndexAvgBranch);
                Console.WriteLine(LoadIndexMaxBranch);
                Console.WriteLine(loadindexavgtotal);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {
                // закрываем подключение
                connection.Close();
                Console.WriteLine("Подключение закрыто...");
            }
        }
        private double GetLoadIndex(string sqlExpression, Guid companyId, NpgsqlConnection connection)
        {
            NpgsqlCommand command = new NpgsqlCommand(sqlExpression, connection);
            command.CommandType = System.Data.CommandType.StoredProcedure;
            NpgsqlParameter nameParam = new NpgsqlParameter
            {
                ParameterName = "company",
                Value = companyId
            };
            command.Parameters.Add(nameParam);
            var result = command.ExecuteScalar();
            return Convert.ToDouble(result);
        }
    }
}