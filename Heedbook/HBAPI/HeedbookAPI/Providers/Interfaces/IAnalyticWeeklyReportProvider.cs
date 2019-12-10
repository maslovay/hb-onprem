using System;
using System.Collections.Generic;
using HBData.Models;

namespace UserOperations.Providers
{
    public interface IAnalyticWeeklyReportProvider
    {
        Guid GetEmployeeRoleId();
        Guid? GetCompanyId(Guid? userId);
        Guid? GetCorporationId(Guid? companyId);
        List<Guid> GetUserIdsInCorporation(Guid? corporationId, Guid emplyeeRoleId);
        List<Guid> GetUserIdsInCompany(Guid? companyId, Guid employeeRoleId);
        List<VSessionUserWeeklyReport> GetSessionMoreThanBegTime(List<Guid> userIdsInCorporation, DateTime begTime);
        List<VSessionUserWeeklyReport> GetSessionLessThanBegTime(List<Guid> userIdsInCorporation, DateTime begTime);
        List<VWeeklyUserReport> GetDialoguesMoreThanBegTime(List<Guid> userIdsInCorporation, DateTime begTime);
        List<VWeeklyUserReport> GetDialoguesLessThanBegTime(List<Guid> userIdsInCorporation, DateTime begTime);
    }
}