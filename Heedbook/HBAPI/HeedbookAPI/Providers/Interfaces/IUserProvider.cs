﻿using HBData;
using HBData.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace UserOperations.Providers
{
    public interface IUserProvider
    {
        Task<ApplicationUser> GetUserWithRoleAndCompany(Guid userId);
        Task<List<ApplicationUser>> GetUsersForAdmin();

        Task<List<ApplicationUser>> GetUsersForSupervisor(Guid corporationIdInToken, Guid userIdInToken);

        Task<List<ApplicationUser>> GetUsersForManager(Guid companyIdInToken, Guid userIdInToken);
        Task<ApplicationRole> AddOrChangeUserRoles(Guid userId, Guid? newUserRoleId, Guid? oldUserRoleId = null);
        Task<bool> CheckAbilityToCreateOrChangeUser(string roleInToken, Guid? newUserRoleId, Guid? oldUserRoleId);

        Task<bool> CheckAbilityToDeleteUser(string roleInToken, Guid deletedUserRoleId);
    }
}