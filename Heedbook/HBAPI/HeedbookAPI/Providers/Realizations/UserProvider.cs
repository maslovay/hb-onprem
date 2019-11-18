using HBData;
using HBData.Models;
using HBData.Repository;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UserOperations.Models.AnalyticModels;

namespace UserOperations.Providers
{
    public class UserProvider : IUserProvider
    {
        private readonly IGenericRepository _repository;
        private readonly int activeStatus;
        private readonly int disabledStatus;

        public UserProvider(IGenericRepository repository)
        {
            _repository = repository;
            activeStatus = 3;
            disabledStatus = 4;
        }


        public async Task<List<ApplicationUser>> GetUsersForAdmin()
        {
            return _repository.GetAsQueryable<ApplicationUser>().Include(p => p.UserRoles).ThenInclude(x => x.Role)
                        .Where(p => p.StatusId == activeStatus || p.StatusId == disabledStatus).ToList();     //2 active, 3 - disabled     
        }

        public async Task<List<ApplicationUser>> GetUsersForSupervisor(Guid corporationIdInToken, Guid userIdInToken)
        {
                return _repository.GetAsQueryable<ApplicationUser>()
                            .Include(p => p.UserRoles).ThenInclude(x => x.Role)
                            .Include(p => p.Company)
                            .Where(p => p.Company.CorporationId == corporationIdInToken
                                && (p.StatusId == activeStatus || p.StatusId == disabledStatus)
                                && p.Id != userIdInToken)
                            .ToList();
        }

        public async Task<List<ApplicationUser>> GetUsersForManager(Guid companyIdInToken, Guid userIdInToken)
        {
            return _repository.GetAsQueryable<ApplicationUser>()
                      .Include(p => p.UserRoles).ThenInclude(x => x.Role)
                      .Include(p => p.Company)
                      .Where(p => p.CompanyId == companyIdInToken
                          && (p.StatusId == activeStatus)
                          && p.Id != userIdInToken)
                      .ToList();
        }
    }
}
