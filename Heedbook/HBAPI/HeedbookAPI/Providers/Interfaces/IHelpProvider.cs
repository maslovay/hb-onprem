using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UserOperations.Models.AnalyticModels;

namespace UserOperations.Providers.Interfaces
{
    public interface IHelpProvider
    {
        void AddComanyPhrases();
        void CreatePoolAnswersSheet(List<AnswerInfo> answers);
    }
}
