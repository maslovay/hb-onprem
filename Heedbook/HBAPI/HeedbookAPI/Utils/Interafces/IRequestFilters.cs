using System;
using System.Collections.Generic;

namespace UserOperations.Utils.Interfaces
{
    public interface IRequestFilters
    {
        void CheckRolesAndChangeCompaniesInFilter(ref List<Guid> companyIdsInFilter, List<Guid> corporationIdsInFilter);
        void CheckRolesAndChangeCompaniesInFilter(ref List<Guid> companyIdsInFilter, List<Guid> corporationIdsInFilter, string role, Guid companyIdInToken);
        DateTime GetBegDate(string beg);
        DateTime GetEndDate(string end);
        bool IsCompanyBelongToUser(Guid? corporationIdInToken, Guid? companyIdInToken, Guid? companyIdInParams, string roleInToken);
        bool IsCompanyBelongToUser(Guid companyIdInParams);
    }
}