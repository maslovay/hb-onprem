using System;
using System.Collections.Generic;
using System.Linq;
using UserOperations.Utils;
using System.Threading.Tasks;
using HBData.Repository;
using HBData.Models;
using UserOperations.Models;

namespace UserOperations.Services
{
    public class ClientService 
    {
        private readonly LoginService _loginService;
        private readonly DBOperations _dbOperation;
        private readonly RequestFilters _requestFilters;
        private readonly IGenericRepository _repository;

        public ClientService(
            LoginService loginService,
            DBOperations dbOperation,
            RequestFilters requestFilters,
            IGenericRepository repository
            )
        {
            _loginService = loginService;
            _dbOperation = dbOperation;
            _requestFilters = requestFilters;
            _repository = repository;
        }
        public async Task<List<GetClient>> GetAll( string beg, string end, List<string> genders, List<Guid> companyIds, int begAge, int endAge)
        {
            var role = _loginService.GetCurrentRoleName();
            var companyId = _loginService.GetCurrentCompanyId();
            var begTime = _requestFilters.GetBegDate(beg);
            var endTime = _requestFilters.GetEndDate(end);
            _requestFilters.CheckRolesAndChangeCompaniesInFilter(ref companyIds, null, role, companyId);

            var data = _repository.GetAsQueryable<Client>()
                .Where(c => 
                    (c.Age >= begAge)
                    &&  (c.Age <= endAge)
                    && c.Dialogues.Any(d => d.BegTime >= begTime && d.EndTime <= endTime)
                    && (!genders.Any() || genders.Contains(c.Gender))
                    && (!companyIds.Any() ||companyIds.Contains(c.CompanyId)))
                .Select( c => new GetClient () {
                    ClientId = c.ClientId,
                    Age = c.Age,
                    Avatar = c.Avatar,
                    CompanyId = c.CompanyId,
                    CorporationId = c.CorporationId,
                    Email = c.Email,
                    Gender = c.Gender,
                    Phone = c.Phone,
                    StatusId = c.StatusId,
                    ClientNotes = c.ClientNotes,
                    DialogueIds =  c.Dialogues.Where(d => d.BegTime >= begTime && d.EndTime <= endTime).Select(d => d.DialogueId)
                });
            return data.ToList();
            }
        }
        //private async Task<List<Guid?>> GetPersondIdsAsync(DateTime begTime, DateTime endTime, List<Guid> companyIds)
        //{
        //    var persondIds = await GetDialogues(begTime, endTime, companyIds)
        //            .Where ( p => p.PersonId != null )
        //            .Select(p => p.PersonId).Distinct()
        //            .ToListAsyncSafe();
        //    return persondIds;
        //}
        //private IQueryable<Dialogue> GetDialogues(DateTime begTime, DateTime endTime, List<Guid> companyIds = null, List<Guid> applicationUserIds = null, List<Guid> workerTypeIds = null)
        //{
        //    var data = _repository.GetAsQueryable<Dialogue>()
        //            .Where(p => p.BegTime >= begTime &&
        //                p.EndTime <= endTime &&
        //                p.StatusId == 3 &&
        //                p.InStatistic == true &&
        //                (companyIds == null || (!companyIds.Any() || companyIds.Contains((Guid)p.ApplicationUser.CompanyId))) &&
        //                (applicationUserIds == null || (!applicationUserIds.Any() || applicationUserIds.Contains((Guid)p.ApplicationUserId))) &&
        //                (workerTypeIds == null || (!workerTypeIds.Any() || workerTypeIds.Contains((Guid)p.ApplicationUser.WorkerTypeId)))).AsQueryable();
        //    return data;
        //}
        //private IQueryable<Dialogue> GetDialoguesIncludedClientProfile(DateTime begTime, DateTime endTime, List<Guid> companyIds, List<Guid> applicationUserIds, List<Guid> workerTypeIds)
        //{
        //    var data = _repository.GetAsQueryable<Dialogue>()
        //        .Include(p => p.DialogueClientProfile)
        //        .Include(p => p.ApplicationUser)
        //        .Where(p => p.BegTime >= begTime &&
        //            p.EndTime <= endTime &&
        //            p.StatusId == 3 &&
        //            p.InStatistic == true &&
        //            (!companyIds.Any() || companyIds.Contains((Guid)p.ApplicationUser.CompanyId)) &&
        //            (!applicationUserIds.Any() || applicationUserIds.Contains((Guid)p.ApplicationUserId)) &&
        //            (!workerTypeIds.Any() || workerTypeIds.Contains((Guid)p.ApplicationUser.WorkerTypeId))).AsQueryable();
        //    return data;
        //}
    }