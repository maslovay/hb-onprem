using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HBData.Models;

namespace UserOperations.Providers
{
    public interface IAnalyticClientProfileProvider
    {
        Task<List<Guid?>> GetPersondIdsAsync(DateTime begTime, DateTime endTime, List<Guid> companyIds);        
        IQueryable<Dialogue> GetDialoguesIncludedClientProfile(
            DateTime begTime, 
            DateTime endTime, 
            List<Guid> companyIds, 
            List<Guid> applicationUserIds, 
            List<Guid> workerTypeIds);
    }
}