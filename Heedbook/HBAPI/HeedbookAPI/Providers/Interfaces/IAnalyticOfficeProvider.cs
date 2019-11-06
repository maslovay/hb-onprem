using HBData;
using HBData.Models;
using HBData.Models.AccountViewModels;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UserOperations.AccountModels;
using UserOperations.Models.AnalyticModels;

namespace UserOperations.Providers
{
    public interface IAnalyticOfficeProvider
    {
        List<SessionInfo> GetSessionsInfo(
            DateTime prevBeg,
            DateTime endTime,
            List<Guid> companyIds,
            List<Guid> applicationUserIds,
            List<Guid> workerTypeIds);
        List<DialogueInfo> GetDialoguesInfo(
            DateTime prevBeg,
            DateTime endTime,
            List<Guid> companyIds,
            List<Guid> applicationUserIds,
            List<Guid> workerTypeIds);
    }
}