using System;
using System.Collections.Generic;
using System.Threading.Tasks;


namespace UserOperations.Utils
{
    public interface IRequestFilters
    {
        DateTime GetBegDate(string beg);
        DateTime GetEndDate(string end);

        Task<bool> AddOrChangeUserRoles(Guid userId, string roleInToken, Guid? newUserRoleId, Guid? oldUserRoleId = null);
        bool CheckAbilityToCreateOrChangeUser(string roleInToken, Guid? newUserRoleId);

        bool CheckAbilityToDeleteUser(string roleInToken, Guid deletedUserRoleId);

        bool IsCompanyBelongToUser(Guid? corporationIdInToken, Guid? companyIdInToken, Guid? companyIdInParams, string roleInToken);

        void CheckRolesAndChangeCompaniesInFilter(ref List<Guid> companyIdsInFilter, List<Guid> corporationIdsInFilter, string role, Guid companyIdInToken);      
    }
}