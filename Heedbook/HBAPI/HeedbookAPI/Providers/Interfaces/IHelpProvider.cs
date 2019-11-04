using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UserOperations.Models.AnalyticModels;

namespace UserOperations.Providers.Interfaces
{
    public interface IHelpProvider
    {
        void AddComanyPhrases();
        MemoryStream CreatePoolAnswersSheet(List<AnswerInfo> answers, string sheetName);
    }
}
