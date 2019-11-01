using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace UserOperations.Providers.Interfaces
{
    public interface IHelpProvider
    {
        void AddComanyPhrases();
        ///Method for get Cell Value
     //   string GetCellValue(SpreadsheetDocument document, Cell cell);
    }
}
