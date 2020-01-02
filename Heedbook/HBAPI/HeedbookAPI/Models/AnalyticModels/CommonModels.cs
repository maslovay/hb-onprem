using System;
using System.Collections.Generic;
using HBData.Models;

namespace UserOperations.Models.AnalyticModels
{
 
    public class EfficiencyOptimizationHourInfo
    {
        public double Load;
        public int UsersCount;
    }


    

    public class EfficiencyLoadEmployeeTimeInfo
    {
        public string Time;
        public double? SatisfactionIndex;
        public double? LoadIndex;
    }

    public class EfficiencyLoadDialogueTimeSatisfactionInfo
    {
        public double? SatisfactionScore;
        public double? Weight;
    }

 

   

}