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
    public interface IUserProvider
    {
        Task<List<ApplicationUser>> GetUsersForAdmin();

        Task<List<ApplicationUser>> GetUsersForSupervisor(Guid corporationIdInToken, Guid userIdInToken);

        Task<List<ApplicationUser>> GetUsersForManager(Guid companyIdInToken, Guid userIdInToken);
    }
}
