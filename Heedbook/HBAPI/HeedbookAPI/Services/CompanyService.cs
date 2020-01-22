using HBData;
using HBData.Models;
using HBData.Repository;
using HBLib;
using HBLib.Utils;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UserOperations.Controllers;
using UserOperations.Models;
using UserOperations.Services;
using UserOperations.Utils;
using UserOperations.Utils.CommonOperations;

namespace UserOperations.Providers
{
    public class CompanyService
    {
        private readonly IGenericRepository _repository;
        private readonly LoginService _loginService;
        private readonly RequestFilters _requestFilters;
        private readonly SftpClient _sftpClient;
        private readonly FileRefUtils _fileRef;
        private readonly SmtpSettings _smtpSetting;
        private readonly SmtpClient _smtpClient;
        private readonly MailSender _mailSender;
        private readonly string _containerName;

        private readonly int activeStatus;
        private readonly int disabledStatus;

        public CompanyService(
            IGenericRepository repository, 
            LoginService loginService,
            IConfiguration config,
            RecordsContext context,
            SftpClient sftpClient,
            FileRefUtils fileRef,
            RequestFilters requestFilters,
            SmtpSettings smtpSetting,
            SmtpClient smtpClient,
            MailSender mailSender)
        {
            _repository = repository;
            _loginService = loginService;
            _sftpClient = sftpClient;
            _fileRef = fileRef;
            _requestFilters = requestFilters;
            _mailSender = mailSender;
            _containerName = "useravatars";

            _smtpSetting = smtpSetting;
            _smtpClient = smtpClient;
            activeStatus = 3;
            disabledStatus = 4;
        }

        //---COMPANY----
        public async Task<Company> GetCompanyByIdAsync(Guid companyId)
        {
            return await _repository.FindOrExceptionOneByConditionAsync<Company>(p => p.CompanyId == companyId);
        }
        

        public async Task<IEnumerable<Company>> GetCompaniesForAdminAsync()
        {
            var companies =  await _repository.FindByConditionAsync<Company>(p => p.StatusId == activeStatus || p.StatusId == disabledStatus);
            return companies ?? new List<Company>();
        }

        public async Task<IEnumerable<Company>> GetCompaniesForSupervisorAsync(Guid? corporationId)
        {
            if (corporationId == null || corporationId == Guid.Empty) return new List<Company>();
            var companies = await _repository.FindByConditionAsync<Company>(p => 
                        p.CorporationId == corporationId 
                        && (p.StatusId == activeStatus || p.StatusId == disabledStatus));
            return companies ?? new List<Company>();
        }

        public async Task<IEnumerable<Corporation>> GetCorporationsForAdminAsync()
        {
            return await _repository.FindAllAsync<Corporation>();
        }
        public async Task<Company> UpdateCompanAsync(Company entity, Company companyInParams)
        {
            foreach (var p in typeof(Company).GetProperties())
            {
                var val = p.GetValue(companyInParams, null);
                if (val != null && val.ToString() != Guid.Empty.ToString())
                    p.SetValue(entity, p.GetValue(companyInParams, null), null);
            }
            await _repository.SaveAsync();
            return entity;
        }

        public async Task<Company> AddNewCompanyAsync(Company company, Guid? corporationId = null)
        {
            company.CreationDate = DateTime.UtcNow;
            company.StatusId = activeStatus;
            company.CorporationId = corporationId;
            _repository.Create<Company>(company);
            await _repository.SaveAsync();
            return company;
        }

        //---PRIVATE---
    }
}
