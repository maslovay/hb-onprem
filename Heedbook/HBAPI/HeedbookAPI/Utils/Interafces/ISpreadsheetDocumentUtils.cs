using System.Collections.Generic;
using System.IO;
using UserOperations.Models.AnalyticModels;

namespace UserOperations.Utils.Interfaces
{
    public interface ISpreadsheetDocumentUtils
    {
        void AddComanyPhrases(string filePath);
        MemoryStream CreatePoolAnswersSheet(List<AnswerInfo> answers, string sheetName);
    }
}