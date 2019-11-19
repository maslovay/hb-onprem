using HBData;
using HBData.Models;
using HBData.Repository;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UserOperations.Models;
using UserOperations.Models.AnalyticModels;
using UserOperations.Services;

namespace UserOperations.Providers
{
    public class UserProvider : IUserProvider
    {
        private readonly IGenericRepository _repository;
        private readonly ILoginService _loginService;
        private readonly int activeStatus;
        private readonly int disabledStatus;

        public UserProvider(IGenericRepository repository, ILoginService loginService)
        {
            _repository = repository;
            _loginService = loginService;
            activeStatus = 3;
            disabledStatus = 4;
        }


        public async Task<ApplicationUser> GetUserWithRoleAndCompany(Guid userId)
        {
           return await _repository.GetAsQueryable<ApplicationUser>()
                .Where(p => p.Id == userId && (p.StatusId == activeStatus || p.StatusId == disabledStatus))
                .Include(x => x.Company)
                .Include(p => p.UserRoles)
                .ThenInclude(x => x.Role)
                .FirstOrDefaultAsync();
        }

        public async Task<List<ApplicationUser>> GetUsersForAdmin()
        {
            return await _repository.GetAsQueryable<ApplicationUser>().Include(p => p.UserRoles).ThenInclude(x => x.Role)
                        .Where(p => p.StatusId == activeStatus || p.StatusId == disabledStatus).ToListAsync();     //2 active, 3 - disabled     
        }

        public async Task<List<ApplicationUser>> GetUsersForSupervisor(Guid corporationIdInToken, Guid userIdInToken)
        {
                return await _repository.GetAsQueryable<ApplicationUser>()
                            .Include(p => p.UserRoles).ThenInclude(x => x.Role)
                            .Include(p => p.Company)
                            .Where(p => p.Company.CorporationId == corporationIdInToken
                                && (p.StatusId == activeStatus || p.StatusId == disabledStatus)
                                && p.Id != userIdInToken)
                            .ToListAsync();
        }

        public async Task<List<ApplicationUser>> GetUsersForManager(Guid companyIdInToken, Guid userIdInToken)
        {
            return await _repository.GetAsQueryable<ApplicationUser>()
                      .Include(p => p.UserRoles).ThenInclude(x => x.Role)
                      .Include(p => p.Company)
                      .Where(p => p.CompanyId == companyIdInToken
                          && (p.StatusId == activeStatus)
                          && p.Id != userIdInToken)
                      .ToListAsync();
        }

        public async Task<bool> CheckUniqueEmail(string email)
        {
            return await _repository.FindOneByConditionAsync<ApplicationUser>(x => x.NormalizedEmail == email.ToUpper()) == null;
        }

        public async Task<bool> CheckAbilityToCreateOrChangeUser(string roleInToken, Guid? newUserRoleId, Guid? oldUserRoleId)
        {
            if (newUserRoleId == null || newUserRoleId == oldUserRoleId)//---create Employee or role do not changed
                return true;

            List<Guid> allowedEmployeeRoles = await GetAllowedRoles(roleInToken);
            if (allowedEmployeeRoles.Count() == 0 || !allowedEmployeeRoles.Any(p => p == newUserRoleId))
                return false;
            return true;
        }

        public async Task<bool> CheckAbilityToDeleteUser(string roleInToken, Guid deletedUserRoleId)
        {
            var deletedUserRoleName = (await _repository.FindOneByConditionAsync<ApplicationRole>(x => x.Id == deletedUserRoleId)).Name;

            if (roleInToken == "Admin") return true;
            if (roleInToken == "Supervisor") return deletedUserRoleName != "Admin" ? true : false;
            if (roleInToken == "Manager") return deletedUserRoleName != "Admin" && deletedUserRoleName != "Supervisor" ? true : false;
            return false;
        }

        public async Task<ApplicationRole> AddOrChangeUserRoles(Guid userId, Guid? newUserRoleId, Guid? oldUserRoleId = null)
        {
            Guid? settedRoleId = newUserRoleId;
            if (newUserRoleId == null && oldUserRoleId == null)//---for create user
                settedRoleId = (await _repository.FindOneByConditionAsync<ApplicationRole>(x => x.Name == "Employee")).Id;
            else if (newUserRoleId == oldUserRoleId)//---for create user
                return null;//---no changes in user

            if (oldUserRoleId != null)//---for edit user
            {
                var role = await _repository.FindByConditionAsync<ApplicationUserRole>(x => x.UserId == userId);
                _repository.Delete<ApplicationUserRole>(x => x.UserId == userId);
            }

            var userRole = new ApplicationUserRole()
            {
                UserId = userId,
                RoleId = (Guid)settedRoleId
            };
            await _repository.CreateAsync<ApplicationUserRole>(userRole);
            await _repository.SaveAsync();
            return await _repository.FindOneByConditionAsync<ApplicationRole>(x => x.Id == (Guid)settedRoleId);
        }

        public async Task<ApplicationUser> AddNewUser(PostUser message)
        {
            var user = new ApplicationUser
            {
                UserName = message.Email,
                NormalizedUserName = message.Email.ToUpper(),
                Email = message.Email,
                NormalizedEmail = message.Email.ToUpper(),
                CompanyId = (Guid)message.CompanyId,
                CreationDate = DateTime.UtcNow,
                FullName = message.FullName,
                PasswordHash = _loginService.GeneratePasswordHash(message.Password),
                StatusId = activeStatus,//3
                EmpoyeeId = message.EmployeeId,
                WorkerTypeId = message.WorkerTypeId ??(await _repository.FindOneByConditionAsync<WorkerType>(x => x.WorkerTypeName == "Employee")).WorkerTypeId
            };
            _repository.Create<ApplicationUser>(user);
            await _repository.SaveAsync();
            return user;
        }
        private async Task<List<Guid>> GetAllowedRoles(string roleInToken)
        {
            List<ApplicationRole> allRoles = (await _repository.FindAllAsync<ApplicationRole>()).ToList();

            if (roleInToken == "Admin") return allRoles.Where(p => p.Name != "Admin").Select(x => x.Id).ToList();
            if (roleInToken == "Supervisor") return allRoles.Where(p => p.Name != "Manager" && p.Name != "Teacher" && p.Name != "Supervisor").Select(x => x.Id).ToList();
            if (roleInToken == "Manager") return allRoles.Where(p => p.Name != "Admin" && p.Name != "Teacher" && p.Name != "Supervisor" && p.Name != "Manager").Select(x => x.Id).ToList();
            return null;
        }

    }
}
