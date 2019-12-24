using System;
using System.Collections.Generic;
using System.Linq;
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
            _repository = repository;
        }
        public IEnumerable<Country> CountrysGet()
        {
            return _repository.GetAsQueryable<Country>().ToList();
        }
        
        public IEnumerable<ApplicationRole> RolesGet()
        {
            return _repository.GetAsQueryable<ApplicationRole>().ToList();
        }
        public IEnumerable<object> WorkerTypeGet()
        {
            try
            {
                var companyId = _loginService.GetCurrentCompanyId();
                return _repository.GetAsQueryable<WorkerType>().Where(p => p.CompanyId == companyId).Select(p => new { p.WorkerTypeId, p.WorkerTypeName }).ToList();
            }
            catch
            {
                throw new UnauthorizedAccessException();
            }
        }
        public IEnumerable<CompanyIndustry> IndustryGet()
        {
            return _repository.GetAsQueryable<CompanyIndustry>().ToList();
        }
        public IEnumerable<Language> LanguageGet()
        {
            return _repository.GetAsQueryable<Language>().ToList();
        }
        public IEnumerable<PhraseType> PhraseTypeGet()
        {
            return _repository.GetAsQueryable<PhraseType>().ToList();
        }
        public IEnumerable<AlertType> AlertTypeGet()
        {
            return _repository.GetAsQueryable<AlertType>().ToList();
        }
    }
}
