using System;
using System.Collections.Generic;
using System.Linq;
using DetectFaceIdScheduler.Settings;
using DetectFaceIdScheduler.Utils;
using HBData.Models;
using Newtonsoft.Json;

namespace DetectFaceIdScheduler.Services
{
    public class DetectFaceIdService
    {
        private readonly DetectFaceIdSettings _settings;
        private readonly VectorCalculation _vectorCalc;
        public DetectFaceIdService(DetectFaceIdSettings settings)
        {
            _settings = settings;
            _vectorCalc = new VectorCalculation();
        }

        public void DetectFaceIds( ref List<FrameAttribute> frameAttribute)
        {
            for (int i = 0; i< frameAttribute.Count(); i ++)
            {  
                frameAttribute[i].FileFrame.FaceId = DetectLocalFaceId(
                    frameAttribute
                        .Skip(Math.Max(0, i + 1 - _settings.PeriodFrames))
                        .Take(Math.Min(i + 1, _settings.PeriodFrames))
                        .ToList()
                );
            }
        }

        private Guid? DetectLocalFaceId(List<FrameAttribute> frameAttribute)
        {
            var frameForCompare = frameAttribute.Last();
            if (frameForCompare.FileFrame.FaceId != null) 
            {
                return frameForCompare.FileFrame.FaceId;
            }

            frameAttribute = frameAttribute
                .Where(p => p.FileFrame.Time >= frameForCompare.FileFrame.Time.AddSeconds(-_settings.PeriodTime))
                .ToList();

            var index = frameAttribute.Count() - 1;
            var lastFrame = frameAttribute[index];                
            var faceIds = new List<Guid?>();
            var i = index - 1;

            while (i >= 0)
            {
                var cos = _vectorCalc.Cos(
                    JsonConvert.DeserializeObject<List<double>>(lastFrame.Descriptor),
                    JsonConvert.DeserializeObject<List<double>>(frameAttribute[i].Descriptor));
                if (cos > _settings.Threshold) 
                    faceIds.Add(frameAttribute[i].FileFrame.FaceId);
                --i;
            }
            return (faceIds.Any()) ?  faceIds.GroupBy(x => x).OrderByDescending(x => x.Count()).First().Key : Guid.NewGuid();
        }



    }
}