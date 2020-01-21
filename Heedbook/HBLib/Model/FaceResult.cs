using System;

namespace HBLib.Model
{
    public class FaceResult
    {
        public Double[] Descriptor { get; set; }

        public FaceRectangle Rectangle { get; set; }

        public FaceAttributes Attributes { get; set; }

        public FaceEmotions Emotions { get; set; }

        public Headpose Headpose { get; set; }
    }
}