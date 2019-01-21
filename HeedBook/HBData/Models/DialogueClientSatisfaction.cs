using System;
using HBLib.Models;

namespace HBData.Models
{
    public class DialogueClientSatisfaction
    {
        public Guid DialogueClientSatisfactionId { get; set; }

        //dialogue
        public Guid? DialogueId { get; set; }
        public  Dialogue Dialogue { get; set; }

        //0. TOTAL ESTIMATION

        public double? MeetingExpectationsTotal { get; set; }

        public double? BegMoodTotal { get; set; }

        public double? EndMoodTotal { get; set; }


        //1. CLIENT ESTIMATION 

        public double? MeetingExpectationsByClient { get; set; }

        //2. EMPOYEE ESTIMATION 

        public double? MeetingExpectationsByEmpoyee { get; set; }

        public double? BegMoodByEmpoyee { get; set; }

        public double? EndMoodByEmpoyee { get; set; }


        //3. TEACHER ESTIMATION 

        public double? MeetingExpectationsByTeacher { get; set; }

        public double? BegMoodByTeacher { get; set; }

        public double? EndMoodByTeacher { get; set; }


        //4. NN ESTIMATION

        public double? MeetingExpectationsByNN { get; set; }

        public double? BegMoodByNN { get; set; }

        public double? EndMoodByNN { get; set; }


    }
}
