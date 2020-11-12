using HBData;
using HBData.Models;
using HBData.Repository;
using HBLib;
using HBLib.Utils;
using Newtonsoft.Json;
using Notifications.Base;
using RabbitMqEventBus.Events;
using Renci.SshNet.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ExtractFramesFromVideo
{
    public class FramesFromVideo
    {
        private readonly INotificationHandler _handler;
        private readonly ElasticClient _log;
        private readonly RecordsContext _context;
        private const string FrameContainerName = "frames";
        private readonly FFMpegSettings _settings;
        private readonly FFMpegWrapper _wrapper;
        private readonly SftpClient _sftpClient;
        private readonly SftpSettings _sftpSettings;
        
        public FramesFromVideo(
            INotificationHandler handler,
            FFMpegSettings settings,
            FFMpegWrapper wrapper,
            SftpClient sftpClient,
            SftpSettings sftpSettings,
            RecordsContext context,
            ElasticClient log
            )
        {
            _handler = handler;
            _settings = settings;
            _wrapper = wrapper;
            _sftpClient = sftpClient;
            _sftpSettings = sftpSettings;
            _context = context;
            _log = log;
        }

        public async Task Run(string videoBlobRelativePath)
        {
            _log.SetFormat("{Path}");
            _log.SetArgs(videoBlobRelativePath);
            try
            {
                _log.Info("Function Extract Frames From Video Started");
                var fileName = Path.GetFileNameWithoutExtension(videoBlobRelativePath);
                var applicationUserId = fileName.Split(("_"))[0];
                var deviceId = fileName.Split(("_"))[1];
                var videoTimeStamp =
                    DateTime.ParseExact(fileName.Split(("_"))[2], "yyyyMMddHHmmss", CultureInfo.InvariantCulture);
                
                    var pathClient = new PathClient();
                var sessionDir = Path.GetFullPath(pathClient.GenLocalDir(pathClient.GenSessionId()));

                var ffmpeg = new FFMpegWrapper(
                    new FFMpegSettings
                    {
                        FFMpegPath = Path.Combine(pathClient.BinPath(), "ffmpeg.exe")
                    });
                
                await _sftpClient.DownloadFromFtpToLocalDiskAsync(
                        $"{_sftpSettings.DestinationPath}{videoBlobRelativePath}", sessionDir);
                var localFilePath = Path.Combine(sessionDir, Path.GetFileName(videoBlobRelativePath));
                var startTime = DateTime.Now;
                var frames = await SplitVideoToFramesInMemoryStream(localFilePath, applicationUserId, deviceId, videoTimeStamp);
                var firstFrame = frames.FirstOrDefault();
                frames = frames.Where(p => p.FrameName != firstFrame.FrameName)
                    .ToList();
                // System.Console.WriteLine($"Frames info - {JsonConvert.SerializeObject(frames)}");
                // var tasks = frames.Select(p => {
                //     return Task.Run(async() => 
                //     {
                //         await _sftpClient.UploadAsync(p.FramePath, "frames", p.FrameName);
                //     });
                // });
                // await Task.WhenAll(tasks);
                
                // _log.Info($"Processing frames {JsonConvert.SerializeObject(frames)}");
                var existedFrames = _context.FileFrames.Where(p => p.DeviceId == Guid.Parse(deviceId))
                    .ToList();
                var fileFrames = new List<FileFrame>();
                foreach (var frame in frames)
                {
                    var existedFrame = existedFrames?.FirstOrDefault(p => p.FileName == frame.FrameName);
                    if(existedFrame == null)
                    {
                        var fileFrame = await CreateFileFrameAsync(applicationUserId, frame.FrameTime, frame.FrameName, deviceId);
                        fileFrames.Add(fileFrame);
                        _log.Info($"Creating frame - {frame.FrameName}");
                        RaiseNewFrameEvent(frame.FrameName);
                    }
                }
                _log.Info($"Frames for adding - {JsonConvert.SerializeObject(fileFrames)}");
                if (fileFrames.Any())
                {
                    lock (_context)
                    {
                        _context.FileFrames.AddRange(fileFrames);
                        _context.SaveChanges();
                    }
                }
                System.Console.WriteLine($"Total seconds: {DateTime.Now.Subtract(startTime).TotalSeconds}");
                foreach (var fileFrame in fileFrames)
                {
                    RaiseNewFrameEvent(fileFrame.FileName);
                }
                _log.Info("Deleting local files");
                Directory.Delete(sessionDir, true);

                System.Console.WriteLine("Function finished");
                _log.Info("Function finished");

            }
            catch (Exception e)
            {
                _log.Fatal($"Exception occured {e}");
                System.Console.WriteLine($"{e}");
            }   
        }
        private async Task<List<FrameInfo>> SplitVideoToFramesInMemoryStream(
            string inputFileName, 
            string applicationUserId, 
            string deviceId, 
            DateTime videoTimeStamp, 
            int period = 3)
        {
            string arguments = $"-skip_frame nokey -i {inputFileName} -r 1/{period} -f image2pipe -";  //Работает
            Process proc = new Process();
            proc.StartInfo.FileName = Environment.OSVersion.Platform == PlatformID.Win32NT ? _settings.FFMpegPath : "ffmpeg";
            proc.StartInfo.UseShellExecute = false;
            proc.StartInfo.RedirectStandardInput = true;
            proc.StartInfo.RedirectStandardOutput = true;
            proc.StartInfo.UseShellExecute = false;
            proc.StartInfo.Arguments = arguments;
            proc.Start();
            var MS = (Stream)new MemoryStream();            
            // var inputTask = Task.Run(() =>
            // {
            //     convertMemoryStreamToStream(videoStream, proc.StandardInput.BaseStream);
            //     // videoStream.Position = 0;
            //     // videoStream.CopyTo(proc.StandardInput.BaseStream);
            //     proc.StandardInput.Close();
            // });    
             
            var outputTask = Task.Run(() =>
            {
                var OutputStream = proc.StandardOutput.BaseStream;
                OutputStream.CopyTo(MS);
                proc.StandardOutput.Close();
            });
            // Task.WaitAll(inputTask, outputTask);
            Task.WaitAll(outputTask);
            proc.WaitForExit();
            
            int index = 1;
            List<FrameInfo> framesInfo = new List<FrameInfo>();
            var uploadTasks = new List<Task>();
            MS.Position = 0;
            foreach (Image Im in GetThumbnails(MS))             
            {
                var FrameStream = new MemoryStream();
                using (var im = new Bitmap(Im))
                {
                    var frameTime =  videoTimeStamp.AddSeconds(index * 3).ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture);
                    var frameName = $"{applicationUserId}_{deviceId}_{frameTime}.jpg";
                    framesInfo.Add(new FrameInfo()
                        {
                            FrameTime = frameTime,
                            FrameName = frameName
                        });
                    im.Save(FrameStream, ImageFormat.Jpeg);                    
                    FrameStream.Position = 0;
                    uploadTasks.Add(Task.Run(async() =>
                        {
                            await _sftpClient.UploadAsMemoryStreamAsync(FrameStream, "frames", frameName);
                        }));                    
                    index++;
                }
            }
            await Task.WhenAll(uploadTasks);
            return framesInfo;
        }
        private IEnumerable<Image> GetThumbnails(Stream stream)
        {
            byte[] allImages;
            using (var ms = new MemoryStream())
            {
                stream.CopyTo(ms);
                allImages = ms.ToArray();                                               //В allImages поместили поток всех полученных изображений в виде одного массива байтов
            }
            var bof = allImages.Take(8).ToArray();                                      //В bof поместили 8 байтов из массива allImages (8 ячеек массива allImages)
            var prevOffset = -1;                                                
            foreach (var offset in GetBytePatternPositions(allImages, bof))             //Перебираем индексы начала подмассивов
            {
                if (prevOffset > -1)
                    yield return GetImageAt(allImages, prevOffset, offset);             //Возвращаем объект Image на основе под массива байтов в место вызова метода
                prevOffset = offset;
            }
            if (prevOffset > -1)
                yield return GetImageAt(allImages, prevOffset, allImages.Length);       //Вернуть оставшиеся данные массива в виде изображения
        }
        private Image GetImageAt(byte[] data, int start, int end)                        //Получили массив байтов, начальный индекс в массиве и конечный индекс в массиве
        {
            using (var ms = new MemoryStream(end - start))                              //Инициализировали поток
            {
                ms.Write(data, start, end - start);                                     //Записали в поток под последовательность байтов
                return Image.FromStream(ms);                                            //Вернули изображение в виде объекта Image
            }
        }

        private IEnumerable<int> GetBytePatternPositions(byte[] data, byte[] pattern)    //data содержит весь массив байтов с изображениями, pattern - содержит первые 8 ячеек массива data
        {
            var dataLen = data.Length;                                                  //Получили общее количество байтов в массиве
            var patternLen = pattern.Length - 1;                                        //Получили число байтов в шаблоне (8-1) = 7
            int scanData = 0;                                                           //Индекс проверенных байтов массива data
            int scanPattern = 0;                                                        //Индекс проверки байтов шаблона
            while (scanData < dataLen)                                                  //В цикле переберем все ячеки массива байтов
            {
                if (pattern[0] == data[scanData])                                       //Найдем совпадения [0] го байта шаблона в основном массиве. Если нашли первое совпадение, то
                {
                    scanPattern = 1;                                                    //индекс шаблона присваиваем 1
                    scanData++;                                                         //инкрементируем индекс основного массива
                    while (pattern[scanPattern] == data[scanData])                      //Пока последующие байты совпадают
                    {
                        if (scanPattern == patternLen)                                  //Если индекс проверки байтов шаблона равен 7, то 
                        {
                            yield return scanData - patternLen;                         //Возвращаем индекс - (как разность  scanData - 7) начала изображения в общем массиве
                            break;
                        }
                        scanPattern++;
                        scanData++;
                    }
                }
                scanData++;                                                             //Инкрементируем индекс основного массива с данными, до тех пор пока не найдем совпадение
            }
        }
        private void convertMemoryStreamToStream(MemoryStream MS, Stream S)
        {
            byte[] buffer = new byte[32 * 1024]; // 32K buffer for example
            int bytesRead;
            MS.Position = 0;
            while ((bytesRead = MS.Read(buffer, 0, buffer.Length)) > 0)
            {
                S.Write(buffer, 0, bytesRead);
            }
        }
        private void convertStreamToStream(Stream sourceStream, Stream targetStream)
        {
            byte[] buffer = new byte[32 * 1024]; // 32K buffer for example
            int bytesRead;
            while ((bytesRead = sourceStream.Read(buffer, 0, buffer.Length)) > 0)
            {
                targetStream.Write(buffer, 0, bytesRead);
            }
        }
        private async Task<FileFrame> CreateFileFrameAsync(string applicationUserId, string frameTime, string fileName, string deviceId)
        {
            Guid? userId = Guid.Parse(applicationUserId);
            if (userId == Guid.Empty) userId = null;
           
            var fileFrame = new FileFrame {
                FileFrameId = Guid.NewGuid(),
                ApplicationUserId = userId,
                DeviceId = Guid.Parse(deviceId),
                FaceLength = 0,
                FileContainer = "frames",
                FileExist = true,
                FileName = fileName,
                IsFacePresent = false,
                StatusId = 6,
                StatusNNId = 6,
                Time = DateTime.ParseExact(frameTime, "yyyyMMddHHmmss", CultureInfo.InvariantCulture),
                CreationTime = DateTime.UtcNow
            };
            return fileFrame;
        }

        private List<FrameInfo> GetLocalFilesInformation(string applicationUserId, string deviceId, string sessionDir, DateTime videoTimeStamp)
        {
            var frames = Directory.GetFiles(sessionDir, "*.jpg")
                .OrderBy(p => Convert.ToInt32((Path.GetFileNameWithoutExtension(p))))
                .Select(p => new FrameInfo 
                {
                    FramePath = p,
                })
                .ToList();
            for (int i = 0; i< frames.Count(); i++)
            {
                frames[i].FrameTime =  videoTimeStamp.AddSeconds(i * 3).ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture);
                frames[i].FrameName = $"{applicationUserId}_{deviceId}_{frames[i].FrameTime}.jpg";
            }
            return frames;
        }

        private void RaiseNewFrameEvent(string filename)
        {
            var message = new FaceAnalyzeRun
            {
                Path = $"{FrameContainerName}/{filename}"
            };

            _handler.EventRaised(message);
        }
    }

    public class FrameInfo
    {
        public string FramePath;
        public string FrameTime;
        public string FrameName;
    }
}