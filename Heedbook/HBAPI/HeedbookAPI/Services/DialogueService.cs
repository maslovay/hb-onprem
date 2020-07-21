using HBData;
using HBData.Models;
using HBData.Repository;
using HBLib;
using HBLib.Utils;
using HBLib.Utils.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UserOperations.Models;

namespace UserOperations.Services
{
    public class DialogueService
    {
        private readonly IGenericRepository _repository;
        private readonly ILoginService _loginService;
        private readonly IRequestFilters _requestFilters;
        private readonly IFileRefUtils _fileRef;
        private readonly string _containerName;

        private readonly int activeStatus;
        private readonly int disabledStatus;

        public DialogueService(
            IGenericRepository repository,
            ILoginService loginService,
            IConfiguration config,
            IFileRefUtils fileRef,
            IRequestFilters requestFilters)
        {
            _repository = repository;
            _loginService = loginService;
            _fileRef = fileRef;
            _requestFilters = requestFilters;
            _containerName = "useravatars";

            activeStatus = 3;
            disabledStatus = 4;
        }



        public async Task<List<DialogueGetModel>> GetAllDialogues(string beg, string end,
                                                             List<Guid?> applicationUserIds,
                                                             List<Guid> deviceIds, List<Guid> companyIds,
                                                             List<Guid> corporationIds, List<Guid> phraseIds,
                                                             List<Guid> phraseTypeIds,
                                                             Guid? clientId,
                                                             bool? inStatistic)
        {
            var begTime = _requestFilters.GetBegDate(beg);
            var endTime = _requestFilters.GetEndDate(end);
            _requestFilters.CheckRolesAndChangeCompaniesInFilter(ref companyIds, corporationIds);
            inStatistic = inStatistic ?? true;

            var dialogues = _repository.GetAsQueryable<Dialogue>()
            .Where(p =>
                p.BegTime >= begTime &&
                p.EndTime <= endTime &&
                p.StatusId == activeStatus &&
                p.InStatistic == inStatistic &&
                (!applicationUserIds.Any() || (p.ApplicationUserId != null && applicationUserIds.Contains(p.ApplicationUserId))) &&
                (!deviceIds.Any() || (p.DeviceId != null && deviceIds.Contains(p.DeviceId))) &&
                (!companyIds.Any() || companyIds.Contains(p.Device.CompanyId)) &&
                (!phraseIds.Any() || p.DialoguePhrase.Any(q => phraseIds.Contains((Guid)q.PhraseId))) &&
                (!phraseTypeIds.Any() || p.DialoguePhrase.Any(q => phraseTypeIds.Contains((Guid)q.PhraseTypeId))) &&
                (clientId == null || p.ClientId == clientId)
            )
            .Select(p => new DialogueGetModel
            {
                DialogueId = p.DialogueId,
                Avatar = (p.DialogueClientProfile.FirstOrDefault() == null) ? null : _fileRef.GetFileUrlFast($"clientavatars/{p.DialogueClientProfile.FirstOrDefault().Avatar}"),
                ApplicationUserId = p.ApplicationUserId,
                FullName = p.ApplicationUser != null ? p.ApplicationUser.FullName : null,
                DialogueHints = p.DialogueHint.Count() != 0 ? "YES" : null,
                BegTime = p.BegTime,
                EndTime = p.EndTime,
                Duration = p.EndTime.Subtract(p.BegTime),
                StatusId = p.StatusId,
                InStatistic = p.InStatistic,
                MeetingExpectationsTotal = p.DialogueClientSatisfaction.FirstOrDefault().MeetingExpectationsTotal,
                DeviceId = p.DeviceId,
                DeviceName = p.Device.Name
            }).ToList();

            return dialogues;
        }

        public async Task<string> GetAllDialoguesPaginated(string beg, string end,
                                                          List<Guid?> applicationUserIds,
                                                          List<Guid> deviceIds, List<Guid> companyIds,
                                                          List<Guid> corporationIds, List<Guid> phraseIds,
                                                          List<Guid> phraseTypeIds,
                                                          Guid? clientId,
                                                          bool? inStatistic,
                                                          int limit = 10, int page = 0,
                                                          string orderBy = "BegTime", string orderDirection = "desc")
        {

            var dialogues = await GetAllDialogues(beg, end, applicationUserIds, deviceIds, companyIds,
                                                        corporationIds, phraseIds, phraseTypeIds, clientId, inStatistic);
            if (dialogues.Count() == 0) return "";

            ////---PAGINATION---
            var pageCount = (int)Math.Ceiling((double)dialogues.Count() / limit);//---round to the bigger 

            Type dialogueType = dialogues.First().GetType();
            PropertyInfo prop = dialogueType.GetProperty(orderBy);
            List<DialogueGetModel> dialoguesList = null;
            if (orderDirection == "asc")
                dialoguesList = dialogues.OrderBy(p => prop.GetValue(p)).Skip(page * limit).Take(limit).ToList();
            else
                dialoguesList = dialogues.OrderByDescending(p => prop.GetValue(p)).Skip(page * limit).Take(limit).ToList();

            return JsonConvert.SerializeObject(new { dialoguesList, pageCount, orderBy, limit, page });
        }

        public async Task<Dictionary<string, object>> DialogueGet(Guid dialogueId)
        {
            var dialogue = _repository.GetAsQueryable<Dialogue>()
                .Include(p => p.DialogueAudio)
                .Include(p => p.DialogueClientProfile)
                .Include(p => p.DialogueClientSatisfaction)
                .Include(p => p.DialogueFrame)
                .Include(p => p.DialogueInterval)
                .Include(p => p.DialoguePhrase)
                .Include(p => p.DialoguePhraseCount)
                .Include(p => p.DialogueSpeech)
                .Include(p => p.DialogueVisual)
                .Include(p => p.DialogueWord)
                .Include(p => p.ApplicationUser)
                .Include(p => p.DialogueHint)
                .Include(p => p.Device)
                .Where(p => p.StatusId == 3 && p.DialogueId == dialogueId).FirstOrDefault();


            if (dialogue == null) throw new NoDataException("No such dialogue or user does not have permission for dialogue");
            dialogue.PersonFaceDescriptor = null;
            dialogue.DialogueWord = dialogue.DialogueWord.GroupBy(p => p.IsClient).Select(p => p.FirstOrDefault()).ToList();

            var begTime = DateTime.UtcNow.AddDays(-30);
            var companyId = dialogue.Device.CompanyId;
            var corporationId = _repository.GetAsQueryable<Company>().Where(x => x.CompanyId == companyId)
                            .Select(x => x.CorporationId).FirstOrDefault();

            var avgDialogueTime = _repository.GetAsQueryable<Dialogue>().Where(p =>
                    p.BegTime >= begTime &&
                    p.StatusId == activeStatus &&
                    p.Device.CompanyId == companyId).Count() != 0 ? _repository.GetAsQueryable<Dialogue>().Where(p =>
                    p.BegTime >= begTime &&
                    p.StatusId == activeStatus &&
                    p.Device.CompanyId == companyId)
                .Average(p => p.EndTime.Subtract(p.BegTime).Minutes) : 0;

            var phraseIds = dialogue.DialoguePhrase.Where(x => x.PhraseId != null).Select(x => (Guid)x.PhraseId).ToList();

            var salesStages = _repository.GetAsQueryable<SalesStage>()
               .Select(x =>
                   new
                   {
                       SalesStagesId = x.SalesStageId,
                       SalesStageSequenceNumber = x.SequenceNumber,
                       SalesStageName = x.Name,
                       IsScored = phraseIds.Intersect(x.SalesStagePhrases
                                        .Where(s => s.CompanyId == companyId || (corporationId!= null && s.CorporationId == corporationId))
                                        .Select(s => s.PhraseId)).Any()
                   }).ToList();


                //• SalesStagesId, Guid(Id этапа продажи, к() которому относится фраза) 
                //• SalesStageSequenceNumber, Int(порядковый номер этапа продажи) 
                //• isScored, bool(1 –если одна и более фраз, относящихся к этапу присутствует диалоге, 0 –в других случаях) 
                //• SalesStageName, String(название этапа продажи, к которому относится фраза)

            var jsonDialogue = JsonConvert.DeserializeObject<Dictionary<string, object>>(JsonConvert.SerializeObject(dialogue));

            jsonDialogue["DeviceName"] = dialogue.Device.Name;
            jsonDialogue["FullName"] = dialogue.ApplicationUser?.FullName;
            jsonDialogue["Avatar"] = (dialogue.DialogueClientProfile.FirstOrDefault() == null) ? null : _fileRef.GetFileUrlFast($"clientavatars/{dialogue.DialogueClientProfile.FirstOrDefault().Avatar}");
            jsonDialogue["Video"] = dialogue == null ? null : _fileRef.GetFileUrlFast($"dialoguevideos/{dialogue.DialogueId}.mp4");
            jsonDialogue["DialogueAvgDurationLastMonth"] = avgDialogueTime;
            jsonDialogue["SalesStages"] = salesStages;

            return jsonDialogue;
        }

        public async Task<bool> ChangeInStatistic(DialoguePut message)
        {
            var companyIdInToken = _loginService.GetCurrentCompanyId();
            List<Dialogue> dialogues;
            if (message.DialogueIds != null)
                dialogues = _repository.GetAsQueryable<Dialogue>().Where(x => message.DialogueIds.Contains(x.DialogueId)).ToList();
            else
                dialogues = _repository.GetAsQueryable<Dialogue>().Where(p => p.DialogueId == message.DialogueId).ToList();
            foreach (var dialogue in dialogues)
            {
                dialogue.InStatistic = message.InStatistic;
            }
            await _repository.SaveAsync();
            return message.InStatistic;
        }

        public async Task<string> SatisfactionChangeByTeacher(DialogueSatisfactionPut message)
        {
            var roleInToken = _loginService.GetCurrentRoleName();
            var companyIdInToken = _loginService.GetCurrentCompanyId();
            var corporationIdInToken = _loginService.GetCurrentCorporationId();
            if (roleInToken != "Admin" && roleInToken != "Supervisor")
                throw new AccessException("Not allowed access(role)"); 
            if (!_requestFilters.IsCompanyBelongToUser(companyIdInToken))
                throw new AccessException ($"Not allowed user company");

            var dialogueClientSatisfaction = await _repository.FindOrExceptionOneByConditionAsync<DialogueClientSatisfaction>(x => x.DialogueId == message.DialogueId);
            dialogueClientSatisfaction.MeetingExpectationsByTeacher = message.Satisfaction;
            dialogueClientSatisfaction.BegMoodByTeacher = message.BegMoodTotal;
            dialogueClientSatisfaction.EndMoodByTeacher = message.EndMoodTotal;
            dialogueClientSatisfaction.Age = message.Age;
            dialogueClientSatisfaction.Gender = message.Gender;
            await _repository.SaveAsync();
            return (JsonConvert.SerializeObject(dialogueClientSatisfaction));
        }

        public async Task<object> GetAlerts(string beg, string end, List<Guid?> applicationUserIds,
                                                       List<Guid> alertTypeIds, List<Guid> deviceIds)
        {
            var companyIdInToken = _loginService.GetCurrentCompanyId();

            var begTime = _requestFilters.GetBegDate(beg);
            var endTime = _requestFilters.GetEndDate(end);

            var dialogues = _repository.GetAsQueryable<Dialogue>()
                    .Where(p => p.BegTime >= begTime
                            && p.EndTime <= endTime
                            // && p.StatusId == 3
                            // && p.InStatistic == true
                            // && (!applicationUserIds.Any() || applicationUserIds.Contains(p.ApplicationUserId))
                            // && (!workerTypeIds.Any() || workerTypeIds.Contains((Guid)p.ApplicationUser.WorkerTypeId))
                            )
                    .Select(p => new
                    {
                        p.DialogueId,
                        p.ApplicationUserId,
                        p.BegTime,
                        p.EndTime,
                        p.DeviceId
                    }).ToList();

            var alerts = _repository.GetAsQueryable<Alert>()
                        .Where(p => p.CreationDate >= begTime
                                && p.CreationDate <= endTime
                                && p.Device.CompanyId == companyIdInToken
                                && (!alertTypeIds.Any() || alertTypeIds.Contains(p.AlertTypeId))
                                && (!applicationUserIds.Any() || applicationUserIds.Contains(p.ApplicationUserId))
                                && (!deviceIds.Any() || deviceIds.Contains(p.DeviceId)))
                        .Select(x => new
                        {
                            x.AlertId,
                            x.AlertTypeId,
                            x.ApplicationUserId,
                            x.CreationDate,
                            dialogueId =
                                    (Guid?)dialogues.FirstOrDefault(p => p.DeviceId == x.DeviceId
                                        && p.BegTime <= x.CreationDate
                                        && p.EndTime >= x.CreationDate).DialogueId,
                            x.DeviceId
                        })
                        .OrderByDescending(x => x.CreationDate)
                        .ToList();
            return alerts;
        }



            //---PRIVATE---
        }
}