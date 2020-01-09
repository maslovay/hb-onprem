using System;
using System.Collections.Generic;
using System.Linq;
using UserOperations.Utils;
using System.Threading.Tasks;
using HBData.Repository;
using HBData.Models;
using UserOperations.AccountModels;
using UserOperations.Models;
using UserOperations.Controllers;

namespace UserOperations.Services
{
    public class DeviceService 
    {
        private readonly LoginService _loginService;
        private readonly RequestFilters _requestFilters;
        private readonly IGenericRepository _repository;

        public DeviceService(
            LoginService loginService,
            RequestFilters requestFilters,
            IGenericRepository repository
            )
        {
            _loginService = loginService;
            _requestFilters = requestFilters;
            _repository = repository;
        }

        public async Task<string> GenerateToken(DeviceAuthorization message)
        {
            var device = GetDeviceIncludeCompany(message.DeviceName);
            if (device.StatusId != GetStatusId("Active")) throw new Exception("Device not activated");

            if (_loginService.CheckDeviceLogin(device.Name, device.Code))
                return _loginService.CreateTokenForDevice(device);
            throw new UnauthorizedAccessException("Error in device name or code");
        }

        public async Task<List<Device>> GetAll(List<Guid> companyIds)
        {
            var role = _loginService.GetCurrentRoleName();
            var companyId = _loginService.GetCurrentCompanyId();
            _requestFilters.CheckRolesAndChangeCompaniesInFilter(ref companyIds, null, role, companyId);

            var data = _repository.GetAsQueryable<Device>()
                .Where(c => (!companyIds.Any() || companyIds.Contains(c.CompanyId)));
               
            return data.ToList();
        }

        public async Task<Device> Create(PostDevice device)
        {
            var role = _loginService.GetCurrentRoleName();
            var companyId = _loginService.GetCurrentCompanyId();
            var corporationId = _loginService.GetCurrentCorporationId();
            var userId = _loginService.GetCurrentUserId();
            var newDeviceCompanyId = device.CompanyId ?? companyId;
            _requestFilters.IsCompanyBelongToUser(corporationId, companyId, newDeviceCompanyId, role);
            CheckUniqueDeviceName(device.Name, newDeviceCompanyId);

            Device newDevice = new Device
            {
                Code = device.Code,
                CompanyId = newDeviceCompanyId,
                DeviceTypeId = device.DeviceTypeId,
                Name = device.Name,
                StatusId = GetStatusId("Active")
            };
            await _repository.CreateAsync<Device>(newDevice);
            await _repository.SaveAsync();
            return newDevice;
        }

        public async Task<string> Update(Device device)
        {
            var role = _loginService.GetCurrentRoleName();
            var companyId = _loginService.GetCurrentCompanyId();
            var corporationId = _loginService.GetCurrentCorporationId();
            Device deviceEntity = await _repository.FindOrExceptionOneByConditionAsync<Device>(c => c.DeviceId == device.DeviceId);
            _requestFilters.IsCompanyBelongToUser(corporationId, companyId, deviceEntity.CompanyId, role);

            Type deviceType = deviceEntity.GetType();
            foreach (var p in typeof(Device).GetProperties())
            {
                var val = p.GetValue(device, null);
                if (val != null && val.ToString() != Guid.Empty.ToString() && p.Name != "DeviceId")
                {
                    deviceType.GetProperty(p.Name).SetValue(deviceEntity, val);
                }
            }
            _repository.Update<Device>(deviceEntity);
            await _repository.SaveAsync();
            return "Saved";
        }

        public async Task<string> Delete(Guid deviceId)
        {
            var role = _loginService.GetCurrentRoleName();
            var companyId = _loginService.GetCurrentCompanyId();
            var corporationId = _loginService.GetCurrentCorporationId();
            Device deviceEntity = await _repository.FindOrExceptionOneByConditionAsync<Device>(c => c.DeviceId == deviceId);
            _requestFilters.IsCompanyBelongToUser(corporationId, companyId, deviceEntity?.CompanyId, role);

            if (_repository.GetAsQueryable<Dialogue>().Any(x => x.DeviceId == deviceId))
            {
                deviceEntity.StatusId = GetStatusId("Inactive");
                await _repository.SaveAsync();
                return "Set inactive";
            }
            _repository.Delete(deviceEntity);
            await _repository.SaveAsync();
            return "Deleted";
        }

        //---PRIVATE---
        private int GetStatusId(string statusName)
        {
            return _repository.GetAsQueryable<Status>().FirstOrDefault(p => p.StatusName == statusName).StatusId;
        }

        private void CheckUniqueDeviceName(string deviceName, Guid companyId)
        {
            if (_repository.GetAsQueryable<Device>().Any(x => x.Name == deviceName && x.CompanyId == companyId))
                throw new NotUniqueException();
        }

        private Device GetDeviceIncludeCompany(string name)
        {
            var device = _repository.GetWithIncludeOne<Device>(p => p.Name.ToLower() == name.ToLower(), o => o.Company);
            if (device is null) throw new Exception("No such user");
            return device;
        }
    }
}