using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.IO;
using Microsoft.AspNetCore.Http;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.Extensions.Configuration;

using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using System.Net.Http;
using System.Net;
using Newtonsoft.Json;
using HBData;
using HBData.Models;
using Microsoft.CodeAnalysis;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.EntityFrameworkCore.Query.Expressions;
using Microsoft.Extensions.DependencyInjection;

namespace TeacherAPI
{
    [Route("teacher/[controller]")]
    [ApiController]
    public class FrameController : Controller
    {
        private readonly RecordsContext _context;
        private readonly IConfiguration _config;
        public FrameController(
            RecordsContext context,
            IConfiguration config
            )
        {
            _context = context;
            _config = config;
        }

        [HttpGet("GetCompanyList")]     
        public IActionResult GetCompanyName([FromQuery(Name = "begTime")] string beg,
                                                    [FromQuery(Name = "endTime")] string end,
                                                    [FromQuery(Name = "companyId")] List<Guid?> companyIds)
        {
            var stringFormat = "yyyyMMdd";
            var begTime = !String.IsNullOrEmpty(beg) ? DateTime.ParseExact(beg, stringFormat, CultureInfo.InvariantCulture) : DateTime.Now.AddDays(-6666).Date;    //if begTime not null, then Beg = begTime, otherwise Beg = DateTime.Now.Day - 6
            var endTime = !String.IsNullOrEmpty(end) ? DateTime.ParseExact(end, stringFormat, CultureInfo.InvariantCulture) : DateTime.Now.Date;                //if endTime not null, then End = endTime, otherwise End = DateTime.Now

            try
            {
                var frames = _context.FileFrames
                    .Include(p => p.ApplicationUser)
                    .Include(p => p.ApplicationUser.Company)
                    .Where(p => p.StatusId == 5 && p.Time >= begTime && p.Time <= endTime)
                    .Select(p => new
                    {
                        CompanyId = p.ApplicationUser.Company.CompanyId,
                        CompanyName = p.ApplicationUser.Company.CompanyName,
                        ApplicationUserId = p.ApplicationUserId,
                        ApplicationUserName = p.ApplicationUser.FullName,
                        FileFrameId = p.FileFrameId
                        
                    })
                    .ToList();

                var res = frames.GroupBy(p => p.CompanyId)
                    .Select(p => new CompanyInfo
                    {
                        CompanyId = p.Key,
                        ApplicationUserInfo = p.GroupBy(q => q.ApplicationUserId)
                            .Select(q => new ApplicationUserInfo
                            {
                                ApplicationUserId = q.Key,
                                FullName = q.First().ApplicationUserName,
                                Frames = q.Count()
                            }).ToList(),
                        CompanyName = p.First().CompanyName,
                        Frames = p.Count()
                    }).ToList();
                return Ok(JsonConvert.SerializeObject(res));
            }
            catch (Exception e)
            {
                
                return BadRequest(e);
            }
        }

        [HttpGet("GetFrames")]
        public IActionResult GetFrames([FromQuery(Name = "companyId")] List<Guid?> companyIds,
                                        [FromQuery(Name = "applicationUserId")] Guid? applicationUserIds,
                                        [FromQuery(Name = "framesRest")] int frameRest)
        {
            var teacherFrameCount = 200; 
            try
            {
                var result = new Publish();
                
                var request = _context.ApplicationUsers
                    .Where(p => !companyIds.Any() || companyIds.Contains(p.CompanyId)).ToList();
                
                Guid? applicationUserId;
                
                if (applicationUserIds == null || applicationUserIds == Guid.Empty)
                {
                    applicationUserId = GetApplicationUserId( frameRest, teacherFrameCount);                                                   
                    if (applicationUserId == Guid.Empty)                                                                        
                    {                                                                                                   
                        result.SAS = new List<string>();                                                                
                        return Ok(JsonConvert.SerializeObject(result));                                                                            //return empty List of string
                    }
                }
                else                                                                                                     //if applicationUserIds not null and not empty. then
                {
                    applicationUserId = applicationUserIds;
                }
                
                var requestFileFrames = _context.FileFrames.Where(f =>
                    f.ApplicationUserId == applicationUserId //Take frameRest Frames
                    && f.Status.StatusId == 5
                    && f.FileExist == true);
                
                var docs = (frameRest != 0) ? requestFileFrames
                    .Take(frameRest*3)
                    .OrderBy(f => f.Time)
                    .ToList() : requestFileFrames.OrderBy(f => f.Time)
                    .Distinct()
                    .ToList();
                                        
                if (docs.Any())                                                                                    //if docs.Count == 0 number of frames of this user
                {                                                                                                       //return empty List of string
                    result.SAS = new List<string>();
                    return Ok(JsonConvert.SerializeObject(result));        
                }
                
                var curdate = DateTime.UtcNow.Date;
                var framePack = new List<string>();                                                                    //declare List for frame.FileNames
                
                //proceed frames not today
                switch (frameRest)
                {
                    case 0:
                        if (docs.First().Time.Date != curdate)
                        {
                            framePack = docs
                                .Where(p => p.Time.Date < curdate)
                                .Take(3 * frameRest)
                                .Where((p, i) => i % 3 == 0)
                                .Select(p => p.FileName)
                                .ToList();
                            Console.WriteLine($"{docs.Count()}");
                            framePack = DublicateFrames(framePack);
                        }
                        else
                        {
                            var curFramePack = docs
                                .Take(3 * frameRest)
                                .Where((p, i) => i % 3 == 0)
                                .Select(p => p.FileName)
                                .ToList();

                            framePack = (curFramePack.Count() == 3 * frameRest) ? DublicateFrames(curFramePack) : framePack;
                        }
                        break;
                    
                    default:
                        framePack = docs
                            .Where((p, i) => i % 3 == 0)
                            .Select(p => p.FileName)
                            .ToList();
                        if (docs.Count() % 3 == 0)
                        {
                            DublicateFrames(framePack);
                        }
                        else
                        {
                            framePack.Insert(0, framePack.First());
                            framePack.Add(docs.Last().FileName);
                            framePack.Add(docs.Last().FileName);
                        }

                        break;
                }
                result.SAS = framePack;
                return Ok(JsonConvert.SerializeObject(result));
            }
            catch (Exception e)
            {
                return BadRequest(e);
            }
        }
        
        [HttpPost("SetDialogueMarkUp")]
        public IActionResult SetDialogueMarkUp([FromBody] object body)
        {
            var data = JsonConvert.DeserializeObject<MarkupTeacher_Root>(Convert.ToString(body));
           
            if (data.body == null)
                return Ok("body is empty");

            if (NullOrEmpty(data.body.begFrame))
                return Ok("begFrame is Empty");
            
            if (NullOrEmpty(data.body.endFrame))
                return Ok("endFrame is Empty");
            
            var beg = data.body.begFrame;
            var end = data.body.endFrame;
            var markup = data.body.markup;
            var teacherId = (data.body.teacherId == null) ? "teacher": data.body.teacherId;
            
            var applicationUserId = GetApplicationUserId(end);
            var languageId = GetLanguageId(applicationUserId);
            
            if (IsOdd(markup.Count))                                                                               
            {
                return BadRequest("Failed Teacher");
            }
            
            var minDate = DateTime.SpecifyKind(GetTime(markup[0]), DateTimeKind.Utc);   
            var maxDate = DateTime.SpecifyKind(GetTime(markup[markup.Count - 1]), DateTimeKind.Utc);
            
            var minDateMarkup = DateTime.SpecifyKind(GetTimeFromFileName(beg), DateTimeKind.Utc);
            var maxDateMarkup = DateTime.SpecifyKind(GetTimeFromFileName(end), DateTimeKind.Utc);
         
            if (markup.Count == 0)
            {
                var collectionTeacherMarkup = _context.DialogueMarkups.Add(new DialogueMarkup
                    {
                        ApplicationUserId = applicationUserId,
                        CreationTime = DateTime.Now,
                        BegTime = minDateMarkup,
                        EndTime = maxDateMarkup,
                        BegTimeMarkup = minDateMarkup,
                        EndTimeMarkup = maxDateMarkup,
                        IsDialogue = false,
                        StatusId = 6,
                        TeacherId = teacherId
                    });
                _context.SaveChanges();
                
                _context.FileFrames
                    .Include(p => p.Status)
                    .Where(p => p.Time <= maxDateMarkup
                                && p.ApplicationUserId == applicationUserId
                                && p.StatusId == 5)
                    .ToList()
                    .ForEach(p=>p.StatusId = 6);
                _context.SaveChanges();  
                return Ok("Ok");  
            }
            
            try
            {
                if (markup.Count == 2 && minDate == minDateMarkup && maxDate == maxDateMarkup)            
                {
                    var collectionTeacherMarkup = _context.DialogueMarkups.Add(new DialogueMarkup{
                            ApplicationUserId = applicationUserId,
                            CreationTime = DateTime.Now,
                            BegTime = minDate,
                            EndTime = maxDate,
                            BegTimeMarkup = minDateMarkup,
                            EndTimeMarkup = maxDateMarkup,
                            IsDialogue = true,
                            StatusId = 6,
                            TeacherId = teacherId
                        });

                    _context.FileFrames
                        .Include(p=>p.Status)
                        .Where(f => f.Time <= maxDateMarkup
                            && f.ApplicationUserId == applicationUserId
                            && f.StatusId == 5)
                        .ToList()
                        .ForEach(p=>p.StatusId = 6);
                    _context.SaveChanges();

                    if (CheckStatus(applicationUserId, minDate, maxDate))
                        SendMessage(applicationUserId, languageId, minDate, maxDate, 0);
                    else
                        SendMessage(applicationUserId, languageId, minDate, maxDate, 4);
                    return Ok("Ok");
                }
                else
                {
                    var markupList = new List<DialogueMarkup>();

                    if (minDate != minDateMarkup)
                        markupList.Add(CreateMarkup(minDateMarkup, minDate, false, 6, applicationUserId, teacherId, minDateMarkup, maxDateMarkup));
                    for (int i = 0; i < markup.Count - 2; i++)
                    {
                        if (IsOdd(i))
                            markupList.Add(CreateMarkup(GetTime(markup[i]), GetTime(markup[i + 1]), false, 6, applicationUserId, teacherId, minDateMarkup, maxDateMarkup));
                        else
                        {
                            markupList.Add(CreateMarkup(GetTime(markup[i]), GetTime(markup[i + 1]), true, 6, applicationUserId, teacherId, minDateMarkup, maxDateMarkup));
                            if (CheckStatus(applicationUserId, GetTime(markup[i]), GetTime(markup[i + 1])))
                                SendMessage(applicationUserId, languageId, GetTime(markup[i]), GetTime(markup[i + 1]), 0);
                            else
                                SendMessage(applicationUserId, languageId, GetTime(markup[i]), GetTime(markup[i + 1]), 4);
                        }
                    }
                    if (maxDate != maxDateMarkup)
                    {
                        if (CheckStatus(applicationUserId, GetTime(markup[markup.Count - 2]),GetTime(markup[markup.Count - 1])))
                            SendMessage(applicationUserId, languageId, GetTime(markup[markup.Count - 2]), GetTime(markup[markup.Count - 1]), 0);
                        else
                            SendMessage(applicationUserId, languageId, GetTime(markup[markup.Count - 2]), GetTime(markup[markup.Count - 1]), 4);
                        
                        markupList.Add(CreateMarkup(GetTime(markup[markup.Count - 2]), GetTime(markup[markup.Count - 1]), true, 6, applicationUserId, teacherId, minDateMarkup, maxDateMarkup));
                        markupList.Add(CreateMarkup(maxDate, maxDateMarkup, false, 6, applicationUserId, teacherId, minDateMarkup, maxDateMarkup));
                        
                        _context.FileFrames.Where(f => f.Time <= maxDateMarkup
                                && f.ApplicationUserId == applicationUserId
                                && f.StatusId == 5)
                            .ToList()
                            .ForEach(f=>f.StatusId = 6);
                        _context.SaveChanges();
                    }
                    else
                    {
                        _context.FileFrames
                            .Where(f => f.Time < GetTime(markup[markup.Count - 2])
                                && f.ApplicationUserId ==
                                applicationUserId
                                && f.StatusId == 5)
                            .ToList()
                            .ForEach(f=>f.StatusId=6);
                        _context.SaveChanges();  
                    }
                    _context.DialogueMarkups.AddRange(markupList);                                                     //Add markupList into DialogueMarkups table
                    _context.SaveChanges();                                                                            //And save
                    return Ok("Markup max date");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }
        
        private string GetImageName(Guid Guid, DateTime dateTime)
        {
            string imageName = Guid.ToString() + "_" + dateTime.ToString("yyyyMMddhhmmss") + ".jpg";  
            return imageName;          
        }

        private string GetVideoName(Guid Guid)
        {
            string videoName = Guid.ToString() + "_" + DateTime.Now.ToString("yyyyMMddhhmmss") + ".mp4";  
            return videoName; 
        }
        
        private int OldFrames(IGrouping<Guid, FileFrame> frames)
        {
            var curDate = DateTime.UtcNow;
            return frames.Where(p => p.Time < curDate).Count();
        }

        private int NewFrames(IGrouping<Guid, FileFrame> frames)
        {
            var curDate = DateTime.UtcNow;
            return frames.Where(p => p.Time >= curDate).Count();
        }
        private Guid? GetApplicationUserId(int framesRest, int TeacherFrameCount)
        {
            var curdate = DateTime.UtcNow.Date;

            var result = _context.FileFrames
                .Where(f => f.Status.StatusId == 5)
                .GroupBy(p => p.ApplicationUserId)
                .Select(p => new
                {
                    ApplicationUserId = p.Key,
                    OldFrames = OldFrames(p),
                    NewFrames = NewFrames(p)
                })
                .Where(p => (p.OldFrames > 0) || (p.NewFrames > 0))
                .ToList();

            if (!result.Any()) 
                return Guid.Empty;
            if (framesRest == 0) 
                return result.First().ApplicationUserId;

            var resultId = result.Where(p => p.OldFrames != 0 || p.NewFrames >= 3 * framesRest)
                .FirstOrDefault().ApplicationUserId;
            return (resultId == null) ? Guid.Empty : resultId;
        }

        private bool NullOrEmpty(string prop)
        {
            if (prop == null || prop == "")
            {
                return true;
            }
            return false;
        }

        private int? GetLanguageId(Guid ApplicationUserId)
        {
            return _context.ApplicationUsers.Include(a => a.Company)
                .Where(a => a.Id == ApplicationUserId)
                .ToList()
                .First().Company.LanguageId;
        }
        public class MarkupTeacher_Root
        {
            public MarkupTeacher body { get; set; }
        }
        
        private DateTime GetTime(string str)
        {
            return DateTime.ParseExact(str, "yyyyMMddHHmmss", CultureInfo.InvariantCulture);
        }
        private DateTime GetTimeFromFileName(string filename)
        {
            var split = filename.Split('_');
            var date = split[1].Split('.');
            return DateTime.ParseExact(date[0], "yyyyMMddHHmmss", CultureInfo.InvariantCulture);
        }
        
        private DialogueMarkup CreateMarkup(DateTime beg, DateTime end, bool isDialogue, int status, Guid applicationUserId, string teacherId, DateTime begMarkup, DateTime endMarkup)
        {
            var markup = new DialogueMarkup {
                DialogueMarkUpId = Guid.NewGuid(),
                ApplicationUserId = applicationUserId,
                CreationTime = DateTime.Now,
                BegTime = beg,
                EndTime = end,
                BegTimeMarkup = begMarkup,
                EndTimeMarkup = endMarkup,
                IsDialogue = isDialogue,
                StatusId = status,
                TeacherId = teacherId};
            return markup;
        }
        
        private void SendMessage(Guid applicationUserId, int? languageId, DateTime begTime, DateTime endTime, int status)        //HeedBookMesenger is deleted
        {
            var guid = Guid.NewGuid();
            var sb = new MessageStructure();
            sb.DialogueId = guid;
            sb.ApplicationUserId = applicationUserId;
            sb.LanguageId = languageId;
            sb.BegTime = begTime.ToString("yyyyMMddHHmmss");
            sb.EndTime = endTime.ToString("yyyyMMddHHmmss");
            sb.IsNN = false;
            sb.Avatar = String.Empty;
            
            var emp = new Dialogue();
            emp.DialogueId = guid;
            emp.ApplicationUserId = applicationUserId;
            emp.BegTime = begTime;
            emp.EndTime = endTime;
            emp.LanguageId = languageId;
            emp.CreationTime = DateTime.Now;
            emp.InStatistic = true;
            if (status != 0)
            {
                emp.StatusId = status;
            }
            _context.Dialogues.Add(emp);
            _context.SaveChanges();
        }
        
        public class MessageStructure
        {
            public Guid DialogueId { get; set; }
            public Guid ApplicationUserId { get; set; }
            public string BegTime { get; set; }
            public string EndTime { get; set; }
            public int? LanguageId { get; set; }
            public bool IsNN { get; set; }
            public string Avatar { get; set; }
        }
        
        private bool CheckStatus(Guid ApplicationUserId, DateTime Beg, DateTime End)
        {
            var result = _context.Dialogues
                .Where(p => p.ApplicationUserId == ApplicationUserId
                            && p.StatusId == 3
                            && (p.BegTime < Beg && p.EndTime > Beg
                                || p.BegTime < End && p.EndTime > End
                                || p.BegTime > Beg && p.EndTime < End))
                .ToList();
            return result.Any() ? false : true;
        }
        private bool IsOdd(int value)
        {
            return value % 2 != 0;
        }
        
        private Guid GetApplicationUserId(string filename)
        {
            var split = filename.Split('_');
            return Guid.Parse(split[0]);
        }
        
        private List<string> DublicateFrames(List<string> framePack)
        {
            framePack.Insert(0, framePack.First());
            framePack.Add(framePack.Last());
            return framePack;
        }
        public class CompanyInfo
        {
            public string CompanyName { get; set; }
            public Guid CompanyId { get; set; }
            public int Frames { get; set; }
            public int Videos { get; set; }
            public List<ApplicationUserInfo> ApplicationUserInfo { get; set; }
        }

        public class ApplicationUserInfo
        {
            public Guid ApplicationUserId { get; set; }
            public string FullName { get; set; }
            public int Frames { get; set; }
            public int Videos { get; set; }
        }
        
        public class Publish
        {
            public List<string> SAS { get; set; }
        }
        
        public class MarkupTeacher
        {
            public string teacherId { get; set; }
            public List<string> markup { get; set; }
            public string begFrame { get; set; }
            public string endFrame { get; set; }
        }
        
    }    
}