using System;
using System.Collections.Generic;
using System.Linq;
using UserOperations.Utils;
using System.Threading.Tasks;
using UserOperations.Models.Get.AnalyticClientProfileController;
using HBData.Repository;
using HBData.Models;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace UserOperations.Services
{
    public class AnalyticClientProfileService
    {
        private readonly LoginService _loginService;
        private readonly RequestFilters _requestFilters;
        private readonly List<AgeBoarder> _ageBoarders;
        private readonly IGenericRepository _repository;

        public AnalyticClientProfileService(
            LoginService loginService,
            RequestFilters requestFilters,
            IGenericRepository repository
            )
        {
            _loginService = loginService;
            _requestFilters = requestFilters;
            _ageBoarders = new List<AgeBoarder>{
                new AgeBoarder{
                    BegAge = 0,
                    EndAge = 21
                },
                new AgeBoarder {
                    BegAge = 21,
                    EndAge = 35
                },
                new AgeBoarder {
                    BegAge = 35,
                    EndAge = 55
                },
                new AgeBoarder {
                    BegAge = 55,
                    EndAge = 100
                }};
            _repository = repository;
        }
        public async Task<string> EfficiencyDashboard(string beg, string end, List<Guid?> applicationUserIds, List<Guid> companyIds,
                                                      List<Guid> corporationIds, List<Guid> deviceIds)
        {
                var role = _loginService.GetCurrentRoleName();
                var companyId = _loginService.GetCurrentCompanyId();
                var begTime = _requestFilters.GetBegDate(beg);
                var endTime = _requestFilters.GetEndDate(end);
                var begYearTime = endTime.AddYears(-1);
                _requestFilters.CheckRolesAndChangeCompaniesInFilter(ref companyIds, corporationIds, role, companyId);
    
                var persondIdsPerYear = await GetPersondIdsAsync(begYearTime, begTime, companyIds);
                var data = GetDialoguesIncludedClientProfile(begTime, endTime, companyIds, applicationUserIds, deviceIds)
                    .Select(p => new
                    {
                        p.DialogueClientProfile.FirstOrDefault().Age,
                        p.DialogueClientProfile.FirstOrDefault().Gender,
                        p.PersonId,
                        p.DialogueId
                    }).ToList();

                var result = new List<GenderAgeStructureResult>();
                result = _ageBoarders.Select(p => 
                        {
                            var dataBoarders = data.Where(b => b.Age > p.BegAge && b.Age <= p.EndAge);
                            return new GenderAgeStructureResult
                            {
                                Age = $"{p.BegAge}-{p.EndAge}",
                                MaleCount = dataBoarders
                                    .Where(d => d.Gender == "male")
                                    .Count(),
                                FemaleCount = dataBoarders
                                    .Where(d => d.Gender == "female")
                                    .Count(),
                                MaleAverageAge = dataBoarders
                                    .Where(d => d.Gender == "male")
                                    .Select(d => d.Age)
                                    .Average(),
                                FemaleAverageAge = dataBoarders
                                    .Where(d => d.Gender == "female")
                                    .Select(d => d.Age)
                                    .Average()
                            };
                        })
                    .ToList();
                var objToReturn = new Dictionary<string, object>();
                objToReturn["allClients"] = data.Select(p => p.DialogueId).Distinct().Count();
                objToReturn["uniquePerYearClients"] = data
                    .Where(p => p.PersonId != null && !persondIdsPerYear.Contains(p.PersonId))
                    .Select(p => p.PersonId).Distinct().Count() + data.Where(p => p.PersonId == null).Select(p => p.DialogueId).Distinct().Count();
                objToReturn["genderAge"] = result;
                return JsonConvert.SerializeObject(objToReturn);
        }

        //---PRIVATE---
        private async Task<List<Guid?>> GetPersondIdsAsync(DateTime begTime, DateTime endTime, List<Guid> companyIds)
        {
            var persondIds = await GetDialogues(begTime, endTime, companyIds)
                    .Where ( p => p.PersonId != null )
                    .Select(p => p.PersonId).Distinct()
                    .ToListAsyncSafe();
            return persondIds;
        }
        private IQueryable<Dialogue> GetDialogues(
            DateTime begTime, DateTime endTime, 
            List<Guid> companyIds = null, 
            List<Guid> applicationUserIds = null, 
            List<Guid> deviceIds = null)
        {
            var data = _repository.GetAsQueryable<Dialogue>()
                    .Where(p => p.BegTime >= begTime &&
                        p.EndTime <= endTime &&
                        p.StatusId == 3 &&
                        p.InStatistic == true &&
                        (companyIds == null || (!companyIds.Any() || companyIds.Contains(p.Device.CompanyId))) &&
                        (applicationUserIds == null || (!applicationUserIds.Any() || applicationUserIds.Contains((Guid)p.ApplicationUserId))) &&
                        (deviceIds == null || (!deviceIds.Any() || deviceIds.Contains(p.DeviceId)))).AsQueryable();
            return data;
        }
        private IQueryable<Dialogue> GetDialoguesIncludedClientProfile(
            DateTime begTime, DateTime endTime, 
            List<Guid> companyIds, 
            List<Guid?> applicationUserIds, 
            List<Guid> deviceIds)
        {
            var data = _repository.GetAsQueryable<Dialogue>()
                .Include(p => p.DialogueClientProfile)
                .Include(p => p.ApplicationUser)
                .Where(p => p.BegTime >= begTime &&
                    p.EndTime <= endTime &&
                    p.StatusId == 3 &&
                    p.InStatistic == true &&
                    (!companyIds.Any() || companyIds.Contains(p.Device.CompanyId)) &&
                    (!applicationUserIds.Any() || applicationUserIds.Contains((Guid)p.ApplicationUserId)) &&
                    (!deviceIds.Any() || deviceIds.Contains(p.DeviceId))).AsQueryable();
            return data;
        }
    }
}