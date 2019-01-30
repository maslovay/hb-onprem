using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using HBData.Models;
using HBData.Repository;
using HBLib.Utils;
using Microsoft.Extensions.DependencyInjection;

namespace ExtractFramesFromVideo
{
    public class FramesFromVideo
    {
        private readonly SftpClient _client;

        private readonly IGenericRepository _repository;
        
        public FramesFromVideo(SftpClient client,
            IServiceScopeFactory factory)
        {
            _client = client;
            var scope = factory.CreateScope();
            _repository = scope.ServiceProvider.GetService<IGenericRepository>();
        }

        public async Task Run(string videoBlobName)
        {
                        //string storageConnectionString = MyJsonConfig["connect_string"];
            var datePartForFrameName = videoBlobName.Split('_', '_')[1];


            var yearFrame = int.Parse(datePartForFrameName.Substring(0, 4));
            var monthFrame = int.Parse(datePartForFrameName.Substring(4, 2));
            var dayFrame = int.Parse(datePartForFrameName.Substring(6, 2));
            var hourFrame = int.Parse(datePartForFrameName.Substring(8, 2));
            var minuteFrame = int.Parse(datePartForFrameName.Substring(10, 2));
            var secondFrame = int.Parse(datePartForFrameName.Substring(12, 2));

            var timeGreFrame = new DateTime(yearFrame, monthFrame, dayFrame, hourFrame, minuteFrame, secondFrame);
            timeGreFrame = timeGreFrame.AddSeconds(2);

            var start = videoBlobName.IndexOf('/') + 1;
            var end = videoBlobName.IndexOf('_', start);
            //var body = "178bd1e8-e98a-4ed9-ab2c-ac74734d1903_20190116082942_2.mkv";
            var applicUserId = videoBlobName.Substring( start, end - start);

            // Write blob to memory stream 
            using (var memoryStream = await _client.DownloadFromFtpAsMemoryStreamAsync(videoBlobName))
            {
                //await cloudBlockBlob.DownloadToStreamAsync(memStream);

                // START DOWNLOAD SPECIFIED VIDEO FROM STORAGE

                // END DOWNLOAD SPECIFIED VIDEO FROM STORAGE


                // Configuration of ffmpeg process that will be created
                var psi = new ProcessStartInfo("ffmpeg")
                {
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    Arguments = "-hide_banner -i pipe:0 -f image2 -vf fps=1/3 -update 1 pipe:1"
                };

                // Use configuration of process and run it
                var process = new Process {StartInfo = psi};
                process.Start();

                // Write to StdIn(Async)
                var inputTask = Task.Run(() =>
                {
                    var rawToProc = process.StandardInput;
                    rawToProc.BaseStream.Write(memoryStream.ToArray(), 0, memoryStream.ToArray().Length);
                    process.StandardInput.Close();
                });

                // START BLOCK Write Stdout of ffmpeg to byte array(ffmpegOut)

                var baseStream = process.StandardOutput.BaseStream as FileStream;
                byte[] ffmpegOut;

                var lastRead = 0;
                using (var ms = new MemoryStream())
                {
                    // to do: 4096 ? Answer: Count of bytes in iteration. We can use any number
                    var buffer = new byte[4096];
                    do
                    {
                        lastRead = baseStream.Read(buffer, 0, buffer.Length);
                        ms.Write(buffer, 0, lastRead);

                        //Console.WriteLine("LastRead ----- " + lastRead +  "Buffer Lenght ----- " + buffer.Length);
                    } while (lastRead > 0);

                    ffmpegOut = ms.ToArray();
                    Console.WriteLine("ffmpegOut.Length" + ffmpegOut.Length);
                }
                // END BLOCK    

                Stream streamForUpload = new MemoryStream();
                long ffmpegOutLen = ffmpegOut.Length;
                var isUpload = false;
                var blobNamePrefix = 0;

                // START BLOCK Algorithm for read ffmpeg output, detect jpeg and write jpeg to blobstorage
                for (var i = 0; i < ffmpegOutLen - 3; i++)
                    try
                    {
                        // Detect jpeg bytes start file signature
                        if (ffmpegOut[i] == 255 && ffmpegOut[i + 1] == 216)
                        {
                            isUpload = true;
                            blobNamePrefix++;
                            //cloudBlockBlob = destinationContainer.GetBlockBlobReference($"{BlobNamePrefix}_test_frame.jpg");
                            Console.WriteLine(blobNamePrefix + "==========");
                        }

                        // limit of frames from 15 second video(5 frames)
                        if (i < ffmpegOutLen - 1)
                            //if (BlobNamePrefix < 6)
                            if (ffmpegOut[i + 2] == 255 && ffmpegOut[i + 3] == 217)
                            {
                                var timeGreFrameComplete = timeGreFrame.Year + timeGreFrame.Month.ToString("D2") +
                                                           timeGreFrame.Day.ToString("D2") +
                                                           timeGreFrame.Hour.ToString("D2") +
                                                           timeGreFrame.Minute.ToString("D2") +
                                                           timeGreFrame.Second.ToString("D2");

                                isUpload = false;

                                Console.WriteLine("!!!Stream upload length ---- " + streamForUpload.Length);
                                streamForUpload.Seek(0, SeekOrigin.Begin);


                                // START TEST WORK WITH STORAGE
                                await _client.UploadAsMemoryStreamAsync(streamForUpload, "test/", $"{applicUserId}_{timeGreFrameComplete}_2_test20.jpg");
                                    
                                // END TEST WORK WITH STORAGE

                                streamForUpload.SetLength(0);
                                
                                
                                // START CODE POSTGRESQL
                                var fileFrame = new FileFrame
                                {
                                    ApplicationUserId = Guid.Parse(applicUserId),
                                    FaceLength = 0,
                                    FileContainer = "frames",
                                    FileExist = true,
                                    FileName = $"{applicUserId}_{timeGreFrameComplete}_2_test20.jpg",
                                    IsFacePresent = false,
                                    StatusId = 1,
                                    StatusNNId = 1,
                                    Time = new DateTime(timeGreFrame.Year, timeGreFrame.Month, timeGreFrame.Day,
                                        timeGreFrame.Hour, timeGreFrame.Minute, timeGreFrame.Second)
                                };
                                timeGreFrame = timeGreFrame.AddSeconds(3);
                                
                                await _repository.CreateAsync(fileFrame);
                                _repository.Save();
                                // END CODE POSTGRESQL
                            }

                        // Write to stream jpeg content between start and end file signature
                        if (isUpload) streamForUpload.WriteByte(ffmpegOut[i]);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"Exception occured {e}");
                    }
                // END BLOCK

                Task.WaitAll(inputTask);
                process.WaitForExit();
            }
        }
    }
}