using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using HBData.Models;
using HBData.Repository;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http;
using System.Net.Http.Headers;
using Newtonsoft.Json;
using HBLib;
using HBData;

namespace hb_asr_service.Controllers
{
    [Route("asr/[controller]")]
    [ApiController]
    public class AudioRecognizeController : ControllerBase
    {
        private readonly IGenericRepository _repository;

        private readonly ConcurrentQueue<FileAudioDialogue> _globalQueue;

        private readonly ElasticClientFactory _elasticClientFactory;
        private readonly RecordsContext _context;
        public AudioRecognizeController(
            RecordsContext context,
            ConcurrentQueue<FileAudioDialogue> globalQueue,
            ElasticClientFactory elasticClientFactory)
        {
            _context = context;
            _globalQueue = globalQueue;
            _elasticClientFactory = elasticClientFactory;
        }

        //asr/audiorecognize/filename
        [HttpGet("{dialogueId}")]
        public async Task<IActionResult> Post(Guid dialogueId)
        {
            var log = _elasticClientFactory.GetElasticClient();
            try
            {
                log.Info("Added audio dialogue to queue");
                System.Console.WriteLine("Added audio dialogue to queue");
                var fileAudioDialogue = _context.FileAudioDialogues.Where(item => item.DialogueId == dialogueId).FirstOrDefault();
                if (fileAudioDialogue != null)
                {
                    System.Console.WriteLine("Added");
                    _globalQueue.Enqueue(fileAudioDialogue);
                    return Ok();
                }
                else
                {
                    return BadRequest("No such dialogue");
                }
                
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                log.Fatal(e.ToString());
                throw;
            }
        }
    }
}