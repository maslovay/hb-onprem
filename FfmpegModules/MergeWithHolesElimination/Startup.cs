using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

using System.IO;

 
using System.Text;  
using MongoDB.Driver;  
using MongoDB.Bson; 
using System.Diagnostics;



namespace MergeWithHolesElimination
{
  public class Startup
  {
    public void ConfigureServices(IServiceCollection services)
    {
    }

    public void Configure(IApplicationBuilder app, IHostingEnvironment env)
    {
      if (env.IsDevelopment())
      {
        app.UseDeveloperExceptionPage();
      }

      app.Run(async (context) =>
      {



        DirectoryInfo dirUnmerVideos = Directory.CreateDirectory("unmerged_videos");
        DirectoryInfo dirUnmerFrames = Directory.CreateDirectory("unmerged_frames");

        var dialogueBegTime = "2018-09-11 11:01:30";
        var dialogueEndTime = "2018-09-11 13:04:30";
        CloudStorageAccount storageAccount;
        string connectionString = "DefaultEndpointsProtocol=https;AccountName=testingsergeyfunctions;AccountKey=Cd7so5PcRDGQQ396B0f9mwXC/dkwiCxIWXU+HdifxCi6bkwbHX5x3JcJ2CU1RZbRvVQm6rEPRhe5c5GTLqFWSg==;EndpointSuffix=core.windows.net"; 

        // ffmpeg Merge Command contain some number of parts and assemble in next loop
        // exaple of line that we need assemble:
        //ffmpeg -i unmerged_videos/178bd1e8-e98a-4ed9-ab2c-ac74734d1903_201809111.mkv -loop 1 -framerate 24 -t 10 -i frame10.jpg -i unmerged_videos/178bd1e8-e98a-4ed9-ab2c-ac74734d1903_201809112.mkv -i unmerged_videos/178bd1e8-e98a-4ed9-ab2c-ac74734d1903_201809113.mkv -f lavfi -t 0.1 -i anullsrc=channel_layout=stereo:sample_rate=44100 -filter_complex \"[0:v][0:a][1:v][4:a][2:v][2:a][3:v][3:a]concat=n=4:v=1:a=1\" reconstructed_video_without_holes.mkv

        var ffmpegMergeCommandPartVideoList = String.Empty;
        var ffmpegMergeCommandPartLayerList = String.Empty;
        var ffmpegMergeCommandPartLayerListFrame = String.Empty;
        string ffmpegMergeCommandPartFfmpegGeneralParametrs = "-f lavfi -t 0.1 -i anullsrc=channel_layout=stereo:sample_rate=44100 -filter_complex";

        string ffmpegMergeCommandPartNameOutputVideo = "reconstructed_video_without_holes.mkv";
        string ffmpegMergeCommandPartSpec = "\"";
        int i = 0;
        int framesNumber = 0;
   
        if (CloudStorageAccount.TryParse(connectionString, out storageAccount))
        {   
          Console.WriteLine("####################################");
          Console.WriteLine("DEBUG INFORMATION.\n");

          CloudBlobClient client = storageAccount.CreateCloudBlobClient();
          var containerVideos = client.GetContainerReference("containervideoblobstestmerge");
          var containerFrames = client.GetContainerReference("containerforframes");
       
          // START WORK WITH MONGO(CosmosDB)
          // set dialog time range
          var mongoClient = new MongoClient("mongodb://sergeymongocosmosdb:7CFdCpQHR4Ou4wBeTwxBiisKPPbqCdsvNiFZjw9hNT7rCN8lQkPcSQhxZKsrlqw6TvFtQC0lp8xKsDvWJFXktw==@sergeymongocosmosdb.documents.azure.com:10255/?ssl=true&replicaSet=globaldb");  

          var myDb = mongoClient.GetDatabase("testmydb");  
          var blobVideoColl = myDb.GetCollection<BsonDocument>("blob.videos_test"); 
          var framesInfoColl = myDb.GetCollection<BsonDocument>("frames.info_copy");

          var mongoQuery = new BsonDocument { { "$and", new BsonArray {
            new BsonDocument { {"EndTime", new BsonDocument { {"$gte", dialogueBegTime} } } },
            new BsonDocument { {"BegTime", new BsonDocument { {"$lte", dialogueEndTime} } } },
            new BsonDocument("ApplicationUserId", "178bd1e8-e98a-4ed9-ab2c-ac74734d1903")
          }}};
          Console.WriteLine("0");

          var resultFromMongoBlobVideos = blobVideoColl.Find(mongoQuery).ToList(); 
          var numberBlobs = resultFromMongoBlobVideos.Count;
          Console.WriteLine("1");
          var queryFrames = new BsonDocument { { "$and", new BsonArray {
                new BsonDocument { {"Time", new BsonDocument { {"$gte", dialogueBegTime} } } },
                new BsonDocument { {"Time", new BsonDocument { {"$lte", dialogueEndTime} } } },
                new BsonDocument("ApplicationUserId", "178bd1e8-e98a-4ed9-ab2c-ac74734d1903")
              }}};
    
          var framesInfo = framesInfoColl.Find(queryFrames).ToList();
          Console.WriteLine("2");
          framesInfo.ForEach(p => Convert.ToDateTime(p["Time"]));
          foreach (var frameInfo in framesInfo)
          {
            Console.WriteLine($"{frameInfo["Time"]}");
          }
          Console.WriteLine("Beg ---------------------------------------------- Blob ------------------ End");

          for(int j = 0; j < resultFromMongoBlobVideos.Count; j++)  
          {      
            var begTime  = resultFromMongoBlobVideos[j]["BegTime"].ToString();
            var blobVideoName = resultFromMongoBlobVideos[j]["BlobName"].ToString();
            var endTime  = resultFromMongoBlobVideos[j]["EndTime"].ToString();
  
            Console.WriteLine(begTime + " | " + blobVideoName + " | " + endTime ); 

           // work with time
           //begTime format example  2018-09-11 11:03:43
           //begTimeAfterParse example 9/11/18 11:03:43 AM
            var beg = DateTime.ParseExact(begTime, "yyyy-MM-dd HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture);
            var end = DateTime.ParseExact(endTime, "yyyy-MM-dd HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture);

            //to do: change
            //begSecond = (int)begTimeAfterParse.TimeOfDay.TotalSeconds;
            //endSecond = (int)endTimeAfterParse.TimeOfDay.TotalSeconds;
  
            var blobVideo = containerVideos.GetBlockBlobReference(blobVideoName);
            await blobVideo.DownloadToFileAsync("unmerged_videos/" + blobVideoName, FileMode.Create);

            ffmpegMergeCommandPartVideoList += "-i unmerged_videos/" + blobVideoName + " ";
            ffmpegMergeCommandPartLayerList += $"[{i}:v][{i}:a]";

           // START. Download blobs for merge and generate ffmpeg command. Main actions in that loop:
           // 1. we get beg, name, end of video in time range of dialog from blob.videos_test
           // 2. download videos from containervideoblobstestmerge
           // 3. get informations about frames in frames.info_copy using time range from blob.videos_test
           // 4. download last frames from containerforframes
           // 5. Detect duration of holes. 
           // 6. generated some parts of ffmpeg arguments line for merge videos with frames.

            if(j != resultFromMongoBlobVideos.Count - 1)
            {
              var nextBegTime = resultFromMongoBlobVideos[j+1]["BegTime"].ToString();
              var begNext = DateTime.ParseExact(nextBegTime, "yyyy-MM-dd HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture);


              var holeDuration = begNext.Subtract(end).TotalSeconds;
              Console.WriteLine("holeDuration between current and next video: " + holeDuration);

              if(holeDuration > 1){
                Console.WriteLine($"{begTime}, {endTime}, {framesInfo.First()["Time"]}, {framesInfo.Last()["Time"]}");
                Console.WriteLine($"{begTime > framesInfo.First()["Time"]}, {endTime > framesInfo.Last()["Time"]}");


                var resultMongoFramesInfo = framesInfo.Where(p => p["Time"] >= begTime && p["Time"] <= endTime).ToList();
                
                //Console.WriteLine($"{JsonConvert.SerializeObject(resultMongoFramesInfo)}");
                
                // START. Work with frames. Donwload frames between beg and end of current video in loop
                //var queryMongoFramesInfo = new BsonDocument { { "$and", new BsonArray {
                //  new BsonDocument { {"Time", new BsonDocument { {"$gte", begTime} } } },
                //  new BsonDocument { {"Time", new BsonDocument { {"$lte", endTime} } } },
                //  new BsonDocument("ApplicationUserId", "178bd1e8-e98a-4ed9-ab2c-ac74734d1903")
                //}}};
      

                //var resultMongoFramesInfo = framesInfoColl.Find(queryMongoFramesInfo).ToList();  
                var frameName = resultMongoFramesInfo.Last()["FileName"].ToString();
                Console.WriteLine("Last frame of video: " + frameName);
                  // END. Work with frames.
                var blobFrame = containerFrames.GetBlockBlobReference(frameName);
                await blobFrame.DownloadToFileAsync($"unmerged_frames/{frameName}", FileMode.Create);

                ffmpegMergeCommandPartVideoList += $"-loop 1 -framerate 24 -t {holeDuration} -i unmerged_frames/{frameName}" + " ";
                numberBlobs++;
                ffmpegMergeCommandPartLayerListFrame = $"[{i+1}:v][temporaryMark:a]";
                ffmpegMergeCommandPartLayerList += ffmpegMergeCommandPartLayerListFrame;
                i++;
                framesNumber++;
              }
            }
           
           Console.WriteLine("");
           
           i++;
          } 
          // END WORK WITH MONGO(CosmosDB)

          // Start Assemble ffmpeg argument line
          var ffmpegMergeCommandPartOutputParametrs = $"concat=n={numberBlobs}:v=1:a=1";
          string ffmpegFinalArgumentsLine = $"-hide_banner {ffmpegMergeCommandPartVideoList} {ffmpegMergeCommandPartFfmpegGeneralParametrs} {ffmpegMergeCommandPartSpec}{ffmpegMergeCommandPartLayerList}{ffmpegMergeCommandPartOutputParametrs}{ffmpegMergeCommandPartSpec} {ffmpegMergeCommandPartNameOutputVideo}";
          //Console.WriteLine($"ffmpeg {ffmpegFinalArgumentsLine}");
          ffmpegFinalArgumentsLine = ffmpegFinalArgumentsLine.Replace("temporaryMark", $"{numberBlobs}");
          // End Assemble ffmpeg argument line


          // Start Merge using generated ffmpeg argument line
          Process mergeProc = new Process();
          mergeProc.StartInfo.UseShellExecute = false;
          mergeProc.StartInfo.FileName = "ffmpeg";
          mergeProc.StartInfo.Arguments = ffmpegFinalArgumentsLine;
          mergeProc.StartInfo.RedirectStandardError = true;
          
          Console.WriteLine("\nffmpeg working...");
          mergeProc.Start();
          mergeProc.WaitForExit();
          Console.WriteLine("ffmpeg work complete.");
          // End Merge.
  
          Console.WriteLine($"\nGenerated Line of ffmpeg:");
          Console.WriteLine("'''");
          Console.WriteLine($"ffmpeg {ffmpegFinalArgumentsLine}");
          Console.WriteLine("'''");
          Console.WriteLine($"\nNumber of merged objects: {numberBlobs}. Videos: {numberBlobs - framesNumber}. Frames: {framesNumber}.");
          Console.WriteLine("");
          Console.WriteLine("####################################");
          
        }   

        // START Remove Temprory Content of directories unmerged_videos and unmerged_frames
         /* 
        System.IO.DirectoryInfo dirUnmerVideos = new DirectoryInfo("unmerged_videos");
        foreach (FileInfo file in dirUnmerVideos.GetFiles())
        {
            file.Delete(); 
        }
        foreach (DirectoryInfo dir in dirUnmerVideos.GetDirectories())
        {
            dir.Delete(true); 
        } 

        System.IO.DirectoryInfo dirUnmerFrames = new DirectoryInfo("unmerged_frames");
        foreach (FileInfo file in dirUnmerFrames.GetFiles())
        {
            file.Delete(); 
        }
        foreach (DirectoryInfo dir in dirUnmerFrames.GetDirectories())
        {
            dir.Delete(true); 
        } 
        */
        //DirectoryInfo dirUnmerVideos = Directory.CreateDirectory("unmerged_videos");
        //DirectoryInfo dirUnmerFrames = Directory.CreateDirectory("unmerged_frames");
        //Directory.Delete("unmerged_videos", true);
        //Directory.Delete("unmerged_frames", true);
        dirUnmerVideos.Delete(true);
        dirUnmerFrames.Delete(true);
       // END Remove Temprory Content
      





        await context.Response.WriteAsync("Net core app work is completed.");
      });
    }
  }
}
