using System;
using System.Collections.Generic;

namespace UserOperations.Models.AnalyticModels
{
    public class UserWeeklyInfo
    {
        public UserWeeklyInfo(int corp, int comp)
        {
            AmountUsersInCompany = comp;
            AmountUsersInCorporation = corp;
        }
        public double? TotalAvg;
        public double? Dynamic;
        public Dictionary<DateTime, double> AvgPerDay;
        public int? OfficeRating;
        public int? OfficeRatingChanges;
        public int? CorporationRating;
        public int? CorporationRatingChanges;
        public int AmountUsersInCorporation { get; set; }
        public int AmountUsersInCompany { get; set; }

    }
}