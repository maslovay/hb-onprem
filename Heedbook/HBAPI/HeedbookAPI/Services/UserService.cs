using HBData;
using HBData.Models;
using HBData.Repository;
using HBLib;
using HBLib.Utils;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UserOperations.Controllers;
using UserOperations.Models;
using UserOperations.Services;
using UserOperations.Utils;
using UserOperations.Utils.CommonOperations;

namespace UserOperations.Providers
{
    public class UserService
    {
        private readonly IGenericRepository _repository;
        private readonly LoginService _loginService;
        private readonly RequestFilters _requestFilters;
        private readonly SftpClient _sftpClient;
        private readonly FileRefUtils _fileRef;
        private readonly SmtpSettings _smtpSetting;
        private readonly SmtpClient _smtpClient;
        private readonly MailSender _mailSender;
        private readonly string _containerName;

        private readonly int activeStatus;
        private readonly int disabledStatus;

        public UserService(
            IGenericRepository repository, 
            LoginService loginService,
            IConfiguration config,
            RecordsContext context,
            SftpClient sftpClient,
            FileRefUtils fileRef,
            RequestFilters requestFilters,
            SmtpSettings smtpSetting,
            SmtpClient smtpClient,
            MailSender mailSender)
        {
            _repository = repository;
            _loginService = loginService;
            _sftpClient = sftpClient;
            _fileRef = fileRef;
            _requestFilters = requestFilters;
            _mailSender = mailSender;
            _containerName = "useravatars";

            _smtpSetting = smtpSetting;
            _smtpClient = smtpClient;
            activeStatus = 3;
            disabledStatus = 4;
        }



        public async Task<List<ApplicationUser>> GetUsers()
        {
            var roleInToken = _loginService.GetCurrentRoleName();
            var companyIdInToken = _loginService.GetCurrentCompanyId();
            var corporationIdInToken = _loginService.GetCurrentCorporationId();
            var userIdInToken = _loginService.GetCurrentUserId();

            if (roleInToken == "Admin")
                return await GetUsersForAdminAsync();

            if (roleInToken == "Supervisor")
                return await GetUsersForSupervisorAsync((Guid)corporationIdInToken, (Guid)userIdInToken);

            if (roleInToken == "Manager")
                return await GetUsersForManagerAsync(companyIdInToken, userIdInToken);

            return null;
        }

        public async Task<List<ApplicationUser>> GetUsersForDeviceAsync()
        {
            var companyIdInToken = _loginService.GetCurrentCompanyId();
            var employeeRoleId = (await _repository.FindOrExceptionOneByConditionAsync<ApplicationRole>(x => x.Name == "Employee")).Id;
            return await _repository.GetAsQueryable<ApplicationUser>()
                      .Include(p => p.UserRoles).ThenInclude(x => x.Role)
                      .Include(p => p.Company)
                      .Where(p => p.CompanyId == companyIdInToken
                          && (p.StatusId == activeStatus)
                          && p.UserRoles.Select(r => r.RoleId).Contains(employeeRoleId))
                      .ToListAsync();
        }

        public async Task<UserModel> CreateUserWithAvatarAsync(IFormCollection formData)
        {
            var roleInToken = _loginService.GetCurrentRoleName();
            var companyIdInToken = _loginService.GetCurrentCompanyId();
            var corporationIdInToken = _loginService.GetCurrentCorporationId();

            var userDataJson = formData.FirstOrDefault(x => x.Key == "data").Value.ToString();

            PostUser message = JsonConvert.DeserializeObject<PostUser>(userDataJson);
            if (!await CheckUniqueEmailAsync(message.Email))
                throw new NotUniqueException("User email not unique");

            message.CompanyId = message.CompanyId ?? companyIdInToken;
            if (_requestFilters.IsCompanyBelongToUser(corporationIdInToken, companyIdInToken, message.CompanyId, roleInToken) == false)
                throw new AccessException($"Not allowed user company");

            if (await CheckAbilityToCreateOrChangeUserAsync(roleInToken, message.RoleId, null) == false)
                throw new AccessException("Not allowed user role");

            ApplicationUser user = await AddNewUserAsync(message);
            await _repository.SaveAsync();

            ApplicationRole role = await AddOrChangeUserRolesAsync(user.Id, message.RoleId, null);
            await _repository.SaveAsync();

            //---save avatar---
            string avatarUrl = null;
            if (formData.Files.Count != 0)
            {
                FileInfo fileInfo = new FileInfo(formData.Files[0].FileName);
                var fn = user.Id + fileInfo.Extension;
                user.Avatar = fn;
                var memoryStream = formData.Files[0].OpenReadStream();
                await _sftpClient.UploadAsMemoryStreamAsync(memoryStream, $"{_containerName}/", fn, true);
                avatarUrl = _fileRef.GetFileLink(_containerName, fn, default);
            }
            var userForEmail = await GetUserWithRoleAndCompanyByIdAsync(user.Id);
            try
            {
                await _mailSender.SendUserRegisterEmail(userForEmail, message.Password);
            }
            catch { }
            return new UserModel(user, avatarUrl, role);
        }

        public async Task<UserModel> EditUserWithAvatarAsync(IFormCollection formData)
        {
            var roleInToken = _loginService.GetCurrentRoleName();
            var companyIdInToken = _loginService.GetCurrentCompanyId();
            var corporationIdInToken = _loginService.GetCurrentCorporationId();
            var userIdInToken = _loginService.GetCurrentUserId();

            var userDataJson = formData.FirstOrDefault(x => x.Key == "data").Value.ToString();
            UserModel message = JsonConvert.DeserializeObject<UserModel>(userDataJson);
            var user = await GetUserWithRoleAndCompanyByIdAsync(message.Id);
            if (user == null) throw new NoFoundException("No such user");

            _requestFilters.IsCompanyBelongToUser(corporationIdInToken, companyIdInToken, user?.CompanyId, roleInToken);

            if (await CheckAbilityToCreateOrChangeUserAsync(roleInToken, message.RoleId, user.UserRoles.FirstOrDefault().RoleId) == false)
                throw new AccessException($"Not allowed user role");

            if (message.Email != null && user.Email != message.Email)
                throw new AccessException("Not allowed change email");

            ApplicationRole newRole = null;
            Type userType = user.GetType();
            foreach (var p in typeof(UserModel).GetProperties())
            {
                var val = p.GetValue(message, null);
                if (val != null && val.ToString() != Guid.Empty.ToString() && p.Name != "Role")
                {
                    if (p.Name == "EmployeeId")//---its a mistake in DB
                        userType.GetProperty("EmpoyeeId").SetValue(user, val);
                    else if (p.Name == "RoleId")
                        newRole = await AddOrChangeUserRolesAsync(user.Id, message.RoleId, user.UserRoles.FirstOrDefault().RoleId);
                    else
                        userType.GetProperty(p.Name).SetValue(user, val);
                }
            }

            string avatarUrl = null;
            if (formData.Files.Count != 0)
            {
                await Task.Run(() => _sftpClient.DeleteFileIfExistsAsync($"{_containerName}/{user.Id}"));
                FileInfo fileInfo = new FileInfo(formData.Files[0].FileName);
                var fn = user.Id + fileInfo.Extension;
                var memoryStream = formData.Files[0].OpenReadStream();
                await _sftpClient.UploadAsMemoryStreamAsync(memoryStream, $"{_containerName}/", fn, true);
                user.Avatar = fn;
            }
            if (user.Avatar != null)
            {
                avatarUrl = _fileRef.GetFileLink(_containerName, user.Avatar, default);
            }
            await _repository.SaveAsync();
            return new UserModel(user, avatarUrl, newRole);
        }

        public async Task<string> DeleteUserWithAvatarAsync(Guid applicationUserId)
        {
            var roleInToken = _loginService.GetCurrentRoleName();
            var companyIdInToken = _loginService.GetCurrentCompanyId();
            var corporationIdInToken = _loginService.GetCurrentCorporationId();

            ApplicationUser user = await GetUserWithRoleAndCompanyByIdAsync(applicationUserId);
            if (user == null) throw new NoFoundException("No such user");

            if (!await CheckAbilityToDeleteUserAsync(roleInToken, user.UserRoles.FirstOrDefault().RoleId))
                throw new AccessException($"Not allowed user role");
            if (!_requestFilters.IsCompanyBelongToUser(corporationIdInToken, companyIdInToken, user.CompanyId, roleInToken))
                throw new AccessException($"Not allowed user company");

            user.StatusId = disabledStatus;
            await _repository.SaveAsync();
            try
            {
                await DeleteUserWithRolesAsync(user);
                await _repository.SaveAsync();
                await _sftpClient.DeleteFileIfExistsAsync($"{_containerName}/{user.Id}");
                return "Deleted";
            }
            catch
            {
                return "Disabled Status";
            }
        }




        //---PRIVATE---
        private async Task<List<ApplicationUser>> GetUsersForAdminAsync()
        {
            return await _repository.GetAsQueryable<ApplicationUser>().Include(p => p.UserRoles).ThenInclude(x => x.Role)
                        .Where(p => p.StatusId == activeStatus || p.StatusId == disabledStatus).ToListAsync();     //2 active, 3 - disabled     
        }

        private async Task<List<ApplicationUser>> GetUsersForSupervisorAsync(Guid corporationIdInToken, Guid userIdInToken)
        {
            return await _repository.GetAsQueryable<ApplicationUser>()
                        .Include(p => p.UserRoles).ThenInclude(x => x.Role)
                        .Include(p => p.Company)
                        .Where(p => p.Company.CorporationId == corporationIdInToken
                            && (p.StatusId == activeStatus || p.StatusId == disabledStatus)
                            && p.Id != userIdInToken)
                        .ToListAsync();
        }

        private async Task<List<ApplicationUser>> GetUsersForManagerAsync(Guid companyIdInToken, Guid? userIdInToken)
        {
            return await _repository.GetAsQueryable<ApplicationUser>()
                      .Include(p => p.UserRoles).ThenInclude(x => x.Role)
                      .Include(p => p.Company)
                      .Where(p => p.CompanyId == companyIdInToken
                          && (p.StatusId == activeStatus)
                          && p.Id != userIdInToken)
                      .ToListAsync();
        }
        private async Task<bool> CheckUniqueEmailAsync(string email)
        {
            return await _repository.FindOrNullOneByConditionAsync<ApplicationUser>(x => x.NormalizedEmail == email.ToUpper()) == null;
        }

        private async Task<ApplicationUser> GetUserWithRoleAndCompanyByIdAsync(Guid userId)
        {
            return await _repository.GetAsQueryable<ApplicationUser>()
                 .Where(p => p.Id == userId && (p.StatusId == activeStatus || p.StatusId == disabledStatus))
                 .Include(x => x.Company)
                 .Include(p => p.UserRoles)
                 .ThenInclude(x => x.Role)
                 .FirstOrDefaultAsync();
        }

        private async Task<bool> CheckAbilityToCreateOrChangeUserAsync(string roleInToken, Guid? newUserRoleId, Guid? oldUserRoleId)
        {
            if (newUserRoleId == null || newUserRoleId == oldUserRoleId)//---create Employee or role do not changed
                return true;

            List<Guid> allowedEmployeeRoles = await GetAllowedRolesAsync(roleInToken);
            if (allowedEmployeeRoles.Count() == 0 || !allowedEmployeeRoles.Any(p => p == newUserRoleId))
                return false;
            return true;
        }

        private async Task<bool> CheckAbilityToDeleteUserAsync(string roleInToken, Guid deletedUserRoleId)
        {
            var deletedUserRoleName = (await _repository.FindOrNullOneByConditionAsync<ApplicationRole>(x => x.Id == deletedUserRoleId)).Name;

            if (roleInToken == "Admin") return true;
            if (roleInToken == "Supervisor") return deletedUserRoleName != "Admin" ? true : false;
            if (roleInToken == "Manager") return deletedUserRoleName != "Admin" && deletedUserRoleName != "Supervisor" ? true : false;
            return false;
        }

        private async Task<ApplicationRole> AddOrChangeUserRolesAsync(Guid userId, Guid? newUserRoleId, Guid? oldUserRoleId = null)
        {
            Guid? settedRoleId = newUserRoleId;
            if (newUserRoleId == null && oldUserRoleId == null)//---for create user
                settedRoleId = (await _repository.FindOrNullOneByConditionAsync<ApplicationRole>(x => x.Name == "Employee")).Id;
            else if (newUserRoleId == oldUserRoleId)//---for create user
                return null;//---no changes in user

            if (oldUserRoleId != null)//---for edit user
            {
                var role = await _repository.FindByConditionAsync<ApplicationUserRole>(x => x.UserId == userId);
                _repository.Delete<ApplicationUserRole>(x => x.UserId == userId);
            }

            var userRole = new ApplicationUserRole()
            {
                UserId = userId,
                RoleId = (Guid)settedRoleId
            };
            await _repository.CreateAsync<ApplicationUserRole>(userRole);
            return await _repository.FindOrNullOneByConditionAsync<ApplicationRole>(x => x.Id == userRole.RoleId);
        }

        private async Task<ApplicationUser> AddNewUserAsync(PostUser message)
        {
            var user = new ApplicationUser
            {
                UserName = message.Email,
                NormalizedUserName = message.Email.ToUpper(),
                Email = message.Email,
                NormalizedEmail = message.Email.ToUpper(),
                CompanyId = (Guid)message.CompanyId,
                CreationDate = DateTime.UtcNow,
                FullName = message.FullName,
                PasswordHash = _loginService.GeneratePasswordHash(message.Password),
                StatusId = activeStatus,//3
                EmpoyeeId = message.EmployeeId
            };
            _repository.Create<ApplicationUser>(user);
            return user;
        }

        private async Task DeleteUserWithRolesAsync(ApplicationUser user)
        {
            if (user.UserRoles != null && user.UserRoles.Count() != 0)
                _repository.Delete<ApplicationUserRole>(user.UserRoles);
            _repository.Delete<ApplicationUser>(user);
        }

        private async Task<List<Guid>> GetAllowedRolesAsync(string roleInToken)
        {
            List<ApplicationRole> allRoles = (await _repository.FindAllAsync<ApplicationRole>()).ToList();

            if (roleInToken == "Admin") return allRoles.Where(p => p.Name != "Admin").Select(x => x.Id).ToList();
            if (roleInToken == "Supervisor") return allRoles.Where(p => p.Name != "Manager" && p.Name != "Teacher" && p.Name != "Supervisor").Select(x => x.Id).ToList();
            if (roleInToken == "Manager") return allRoles.Where(p => p.Name != "Admin" && p.Name != "Teacher" && p.Name != "Supervisor" && p.Name != "Manager").Select(x => x.Id).ToList();
            return null;
        }
    }
}
