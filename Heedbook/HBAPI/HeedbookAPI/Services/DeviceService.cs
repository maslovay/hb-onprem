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
            if (!_loginService.CheckDeviceLogin(message.DeviceName, message.Code))
            throw new Exception("Error in device name or code");

            var device = GetDeviceIncludeCompany(message.DeviceName);
            if (device.StatusId != GetStatusId("Active")) throw new Exception("Device not activated");

            return _loginService.CreateTokenForDevice(device);
        }

        public async Task<List<GetDevice>> GetAll(List<Guid> companyIds)
        {
            var role = _loginService.GetCurrentRoleName();
            var companyId = _loginService.GetCurrentCompanyId();
            _requestFilters.CheckRolesAndChangeCompaniesInFilter(ref companyIds, null, role, companyId);

            var data = _repository.GetAsQueryable<Device>()
                .Where(c => (!companyIds.Any() || companyIds.Contains(c.CompanyId)))
                .Select(c => new GetDevice
                                 {
                                    CompanyId = c.CompanyId,
                                    DeviceId = c.DeviceId,
                                    DeviceTypeId = c.DeviceTypeId,
                                    Name = c.Name,
                                    StatusId = c.StatusId
                                 });
               
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
                Code = _loginService.GeneratePasswordHash(device.Code),
                CompanyId = newDeviceCompanyId,
                DeviceTypeId = device.DeviceTypeId,
                Name = device.Name,
                StatusId = GetStatusId("Active")
            };
            await _repository.CreateAsync<Device>(newDevice);
            await _repository.SaveAsync();
            return newDevice;
        }

        public async Task<string> Update(PutDevice device)
        {
            var role = _loginService.GetCurrentRoleName();
            var companyId = _loginService.GetCurrentCompanyId();
            var corporationId = _loginService.GetCurrentCorporationId();
            Device deviceEntity = await _repository.FindOrExceptionOneByConditionAsync<Device>(c => c.DeviceId == device.DeviceId);
            _requestFilters.IsCompanyBelongToUser(corporationId, companyId, deviceEntity.CompanyId, role);

            deviceEntity.Code = device.Code != null ? _loginService.GeneratePasswordHash(device.Code) : deviceEntity.Code;
            deviceEntity.DeviceTypeId = device.DeviceTypeId ?? deviceEntity.DeviceTypeId;
            deviceEntity.Name = device.Name ?? deviceEntity.Name;
            deviceEntity.StatusId = device.StatusId != 0? device.StatusId : deviceEntity.StatusId;
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