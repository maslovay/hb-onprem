
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace UserOperations.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class HelpController : Controller
    {


        public HelpController(
            )
        {
        }
        [HttpGet("Test")]
        public async Task<ActionResult> Test([FromQuery]int skip, int take)
        {
            int counter = 0;
            //var dialogues = _context.Dialogues.Include(x => x.DialogueClientProfile).Skip(skip).Take(take).ToList();
            //        var activeStatusId = _context.Statuss
            //                        .Where(x => x.StatusName == "Active")
            //                        .Select(x => x.StatusId)
            //                        .FirstOrDefault();
            //foreach (var curDialogue in dialogues)
            //{
            //    //   var curDialogue = _context.Dialogues.Include(x => x.DialogueClientProfile).FirstOrDefault(x => x.DialogueId.ToString() == id);
            //    try
            //    {
            //        if (curDialogue.ClientId != null) continue;
            //        if(_context.Clients.Where(x => x.ClientId == curDialogue.PersonId).Any())
            //        {
            //            curDialogue.ClientId = curDialogue.PersonId;
            //            _context.SaveChanges();
            //            continue;
            //        }

            //        var company = _context.ApplicationUsers
            //                      .Where(x => x.Id == curDialogue.ApplicationUserId)
            //                      .Select(x => x.Company)
            //                      .FirstOrDefault();

            //        Guid? personId = curDialogue.PersonId ?? Guid.NewGuid();

            //        var dialogueClientProfile = curDialogue.DialogueClientProfile.FirstOrDefault();
            //        if (dialogueClientProfile == null) continue;


            //        double[] faceDescr = new double[0];
            //        try
            //        {
            //            faceDescr = JsonConvert.DeserializeObject<double[]>(curDialogue.PersonFaceDescriptor);
            //        }
            //        catch { }
            //        if (dialogueClientProfile.Age == null || dialogueClientProfile.Gender == null) continue;

            //        Client client = new Client
            //        {
            //            ClientId = (Guid)personId,
            //            CompanyId = (Guid)company?.CompanyId,
            //            CorporationId = company?.CorporationId,
            //            FaceDescriptor = faceDescr,
            //            Age = (int)dialogueClientProfile?.Age,
            //            Avatar = dialogueClientProfile?.Avatar,
            //            Gender = dialogueClientProfile?.Gender,
            //            StatusId = activeStatusId
            //        };
            //        _context.Clients.Add(client);
            //        //  _context.SaveChanges();

            //        curDialogue.ClientId = personId;
            //       // _context.SaveChanges();
            //        counter++;
            //    }
            //    catch (Exception ex)
            //    {
            //        var m = ex.Message;
            //        //return null;
            //    }
            //}
            //_context.SaveChanges();
            return Ok(counter);
        }
    }
}