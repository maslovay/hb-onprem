using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using HBData.Models;
using HBData.Repository;

namespace UserOperations.Services
{
    public class CatalogueService
    {
        private readonly LoginService _loginService;
        private readonly IGenericRepository _repository;


        public CatalogueService(
            LoginService loginService,
            IGenericRepository repository
            )
        {
            _loginService = loginService;
            // _log = log;
            _repository = repository;
        }
        public IEnumerable<Country> CountrysGet()
        {
            // _log.Info("Catalogue/Country GET");
            return _repository.GetAsQueryable<Country>().ToList();
        }
        
        public IEnumerable<ApplicationRole> RolesGet()
        {
            // _log.Info("Catalogue/Role GET");
            return _repository.GetAsQueryable<ApplicationRole>().ToList();
        }
        public IEnumerable<object> WorkerTypeGet([FromHeader] string Authorization)
        {
            // _log.Info("Catalogue/WorkerType GET");
            if (!_loginService.GetDataFromToken(Authorization, out var userClaims))
                return null;
            var companyId = Guid.Parse(userClaims["companyId"]);
            return _repository.GetAsQueryable<WorkerType>().Where(p => p.CompanyId == companyId).Select(p => new { p.WorkerTypeId, p.WorkerTypeName }).ToList();
        }
        public IEnumerable<CompanyIndustry> IndustryGet()
        {
            // _log.Info("Catalogue/Industry GET");
            return _repository.GetAsQueryable<CompanyIndustry>().ToList();
        }
        public IEnumerable<Language> LanguageGet()
        {
            // _log.Info("Catalogue/Language GET");
            return _repository.GetAsQueryable<Language>().ToList();
        }
        public IEnumerable<PhraseType> PhraseTypeGet()
        {
            // _log.Info("Catalogue/PhraseType GET");
            return _repository.GetAsQueryable<PhraseType>().ToList();
        }
        public IEnumerable<AlertType> AlertTypeGet()
        {
            return _repository.GetAsQueryable<AlertType>().ToList();
        }
    }
}
