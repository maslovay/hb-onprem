using HBData;
using HBData.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UserOperations.Models;

namespace UserOperations.Providers
{
    public interface IUserProvider
    {
        Task<ApplicationUser> GetUserWithRoleAndCompanyAsync(Guid userId);
        Task<List<ApplicationUser>> GetUsersForAdminAsync();
        Task<List<ApplicationUser>> GetUsersForSupervisorAsync(Guid corporationIdInToken, Guid userIdInToken);
        Task<List<ApplicationUser>> GetUsersForManagerAsync(Guid companyIdInToken, Guid userIdInToken);

        Task<ApplicationRole> AddOrChangeUserRolesAsync(Guid userId, Guid? newUserRoleId, Guid? oldUserRoleId = null);
        Task<ApplicationUser> AddNewUserAsync(PostUser message);

        Task<bool> CheckUniqueEmailAsync(string email);
        Task<bool> CheckAbilityToCreateOrChangeUserAsync(string roleInToken, Guid? newUserRoleId, Guid? oldUserRoleId);
        Task<bool> CheckAbilityToDeleteUserAsync(string roleInToken, Guid deletedUserRoleId);
        Task SetUserInactiveAsync(ApplicationUser user);
        Task DeleteUserWithRolesAsync(ApplicationUser user);
        //---COMPANY----
        Task<IEnumerable<Company>> GetCompaniesForAdminAsync();
        Task<IEnumerable<Company>> GetCompaniesForSupervisorAsync(string corporationIdInToken);
    }
}
