using System;
using HBLib.Utils;

namespace HBLib.Model
{
    public class GoogleSttLongrunningResult
    {
        public String Name { get; set; }

        public GoogleSttResponse Response { get; set; }

        public GoogleError Error { get; set; }
    }
}