using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using HBData.Models;
using HBData.Models.AccountViewModels;
using HBData.Repository;
using HBLib;
using HBLib.Utils;
using HBLib.Utils.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;
using UserOperations.AccountModels;
using UserOperations.Models.AnalyticModels;
using UserOperations.Services;
using UserOperations.Services.Interfaces;
using UserOperations.Utils.Interfaces;

namespace ApiTests
{
    delegate void CheckRolesAndChangeCompaniesInFilter(ref List<Guid> companyIds, List<Guid> corporationIds, string role, Guid token);
    public abstract class ApiServiceTest : IDisposable
    {
        protected Mock<AccountService> accountServiceMock;
        protected Mock<IConfiguration> configMock;
        protected Mock<IMailSender> mailSenderMock;
        protected Mock<ILoginService> moqILoginService;
        protected Mock<IGenericRepository> repositoryMock;
        protected Mock<ICompanyService> companyServiceMock;
        protected Mock<ISalesStageService> salesStageServiceMock;
        protected Mock<ISpreadsheetDocumentUtils> spreadSheetDocumentUtils;
        protected Mock<IFileRefUtils> fileRefUtils;
        protected Mock<HttpContextAccessor> httpContextAccessor;
        protected Mock<IRequestFilters> requestFiltersMock;
        protected Mock<IAnalyticHomeUtils> analyticHomeUtils;
        protected Mock<IDBOperations> dBOperations;
        protected Mock<IAnalyticOfficeUtils> analyticOfficeUtils;
        protected Mock<IAnalyticRatingUtils> analyticRatingUtils;
        protected Mock<IAnalyticReportUtils> analyticReportUtils;
        protected Mock<IAnalyticServiceQualityUtils> analyticServiceQualityUtils;
        protected Mock<IAnalyticSpeechUtils> analyticSpeechUtils;
        protected Mock<IAnalyticWeeklyReportUtils> analyticWeeklyReportUtils;
        protected Mock<ISftpClient> sftpClient;
        protected Mock<IdentityDbContext> recordContextMock;
        protected Mock<IElasticLogger> elasticClient;

        public void Setup()
        {
            BaseInit();
            InitData();
            // InitServices();
        }
        protected void BaseInit()
        {
            TestData.beg = "20191001";
            TestData.end = "20191002";
            TestData.begDate = (new DateTime(2019, 10, 03)).Date;
            TestData.endDate = (new DateTime(2019, 10, 05)).Date;
            TestData.prevDate = (new DateTime(2019, 10, 01)).Date;
            TestData.token = "Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiJhbm5zYW1vbHVrX3Rlc3RAZ21haWwuY29tIiwianRpIjoiZmY3Yjc4NGQtMTEzMi00ZmY0LThlN2ItODU4YTBhMDVhMzE3IiwiYXBwbGljYXRpb25Vc2VySWQiOiJhNmI2NjgzNS1hNDEyLTRjMjAtODBiNy0yZGNhN2VhZTRjZDYiLCJhcHBsaWNhdGlvblVzZXJOYW1lIjoiYW5uc2Ftb2x1a190ZXN0QGdtYWlsLmNvbSIsImNvbXBhbnlOYW1lIjoiYW5uc2Ftb2x1a190ZXN0QGdtYWlsLmNvbSIsImNvbXBhbnlJZCI6ImJkNGM0MmIwLWRlNmEtNDkxNS1hYzY5LWE1ZjAzNjAxOWM5ZCIsImNvcnBvcmF0aW9uSWQiOiIiLCJsYW5ndWFnZUNvZGUiOiIxIiwicm9sZSI6Ik1hbmFnZXIiLCJmdWxsTmFtZSI6ImFubnNhbW9sdWtfdGVzdEBnbWFpbC5jb20iLCJhdmF0YXIiOiIiLCJleHAiOjE1NzQ5MzQxNDUsImlzcyI6Imh0dHBzOi8vaGVlZGJvb2suY29tIiwiYXVkIjoiaHR0cHM6Ly9oZWVkYm9vay5jb20ifQ.rRRAcst-r0mD4jkn80L8yKLf9xGPhGxVaNy0tRgKUXM";
            TestData.tokenclaims = TestData.GetClaims();
            TestData.companyIds = TestData.GetCompanyIds();
            TestData.email = $"test1@heedbook.com";

            accountServiceMock = new Mock<AccountService>();
            configMock = new Mock<IConfiguration>();
            mailSenderMock = new Mock<IMailSender>();
            fileRefUtils = new Mock<IFileRefUtils>();
            httpContextAccessor = new Mock<HttpContextAccessor>();
            repositoryMock = new Mock<IGenericRepository>();
            moqILoginService = new Mock<ILoginService>();
            companyServiceMock = new Mock<ICompanyService>();
            salesStageServiceMock = new Mock<ISalesStageService>();
            spreadSheetDocumentUtils = new Mock<ISpreadsheetDocumentUtils>();
            requestFiltersMock = new Mock<IRequestFilters>();
            analyticHomeUtils = new Mock<IAnalyticHomeUtils>();
            dBOperations = new Mock<IDBOperations>();
            analyticOfficeUtils = new Mock<IAnalyticOfficeUtils>();
            analyticRatingUtils = new Mock<IAnalyticRatingUtils>();
            analyticReportUtils = new Mock<IAnalyticReportUtils>();
            analyticServiceQualityUtils = new Mock<IAnalyticServiceQualityUtils>();
            analyticSpeechUtils = new Mock<IAnalyticSpeechUtils>();
            analyticWeeklyReportUtils = new Mock<IAnalyticWeeklyReportUtils>();
            sftpClient = new Mock<ISftpClient>();
            recordContextMock = new Mock<IdentityDbContext>();
            elasticClient = new Mock<IElasticLogger>();
        }
        protected void InitData()
        {
            InitMockILoginService();
            InitMockIMailSender();
            InitMockAccountService();
            InitMockIHelpProvider();
            InitMockIRequestFiltersProvider();
            InitMockIDBOperations();
        }

        protected virtual void InitServices()
        {

        }       

        public void InitMockILoginService()
        {
            
        }
        public void InitMockIMailSender()
        {
            
        }
        public void InitMockAccountService()
        {
        }
        public void InitMockIHelpProvider()
        {
        
        }
        public void InitMockIRequestFiltersProvider()
        {
            
        }
        public void InitMockIDBOperations()
        {
            
        }
        public void Dispose()
        {
            
        }
    }
}