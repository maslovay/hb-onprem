using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UserOperations.Models;

namespace UserOperations.Services
{
    public interface ISalesStageService
    {
        Task CreateSalesStageForNewAccount(Guid? companyId, Guid? corporationId);
        Task<List<GetSalesStage>> GetAll(Guid? companyId);
    }
}