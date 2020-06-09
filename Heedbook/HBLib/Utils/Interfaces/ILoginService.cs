using System;
using System.Collections.Generic;
using HBData.Models;

namespace HBLib.Utils.Interfaces
{
    public interface ILoginService
    {
        bool CheckDeviceLogin(string deviceName, string code);
        bool CheckToken(string token, string sign = "");
        bool CheckUserLogin(string login, string password);
        string CreateTokenEmpty();
        string CreateTokenForDevice(Device device);
        string CreateTokenForUser(ApplicationUser user);
        string GeneratePass(int x);
        string GeneratePasswordHash(string password);
        string GetAvatar(string avatarPath);
        Guid GetCurrentCompanyId();
        Guid? GetCurrentCorporationId();
        Guid? GetCurrentDeviceId();
        int GetCurrentLanguagueId();
        string GetCurrentRoleName();
        Guid GetCurrentUserId();
        bool GetDataFromToken(string token, out Dictionary<string, string> claims, string sign = null);
        bool GetIsExtended();
        bool IsAdmin();
        bool SaveErrorLoginHistory(Guid userId, string type);
        bool SavePasswordHistory(Guid userId, string passwordHash);
    }
}