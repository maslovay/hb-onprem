using System;
using System.Collections.Generic;
using System.Threading.Tasks;


namespace UserOperations.Utils
{
    public interface IRequestFilters
    {
        DateTime GetBegDate(string beg);
        DateTime GetEndDate(string end);  

        bool IsCompanyBelongToUser(Guid? corporationIdInToken, Guid? companyIdInToken, Guid? companyIdInParams, string roleInToken);

        void CheckRolesAndChangeCompaniesInFilter(ref List<Guid> companyIdsInFilter, List<Guid> corporationIdsInFilter, string role, Guid companyIdInToken);      
    }
}