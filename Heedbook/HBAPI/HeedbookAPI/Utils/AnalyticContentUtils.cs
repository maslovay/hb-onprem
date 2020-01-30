using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using HBData;
using UserOperations.Models.Get.HomeController;
using HBData.Models;
using UserOperations.Controllers;
using System.Reflection;
using UserOperations.Models.Get;
using HBData.Repository;
using Microsoft.EntityFrameworkCore;
using System.IO;
using UserOperations.Models.Get.AnalyticContentController;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Spreadsheet;

namespace UserOperations.Utils.AnalyticContentUtils
{
    public class AnalyticContentUtils
    {
        private readonly RecordsContext _context;
        private readonly IConfiguration _config;
        private readonly IGenericRepository _repository;

        public AnalyticContentUtils(RecordsContext context, IConfiguration config, IGenericRepository repository)
        {
            _context = context;
            _config = config;
        }
        public MemoryStream CreatePoolAnswersSheet(List<AnswerInfo> answers, string sheetName)
        {
            var answersModified = answers.SelectMany(x => x.Answers.Select(p => new { p.Answer, p.ContentId, p.DialogueId, p.Time, x.ContentName })).ToList();
            List<List<string>> answersList = new List<List<string>>();
            foreach (var answ in answersModified)
            {
                answersList.Add(new List<string> { answ.Time.ToString(), answ.ContentId.ToString(), answ.ContentName, answ.DialogueId.ToString(), answ.Answer });
            }
            return CreateSpreadsheetDocument(sheetName, answersList, new List<string> { "Time", "FullName", "ContentName", "DialogueId", "Answer" });
        }
        private MemoryStream CreateSpreadsheetDocument(string sheetName, List<List<string>> answersList, List<string> headers)
        {
            MemoryStream memoryStream = new MemoryStream();
            using (SpreadsheetDocument spreadsheetDocument1 = SpreadsheetDocument.Create(memoryStream, SpreadsheetDocumentType.Workbook))
            {
                // Add a WorkbookPart to the document.
                WorkbookPart workbookpart1 = spreadsheetDocument1.AddWorkbookPart();
                workbookpart1.Workbook = new Workbook();

                // Add a WorksheetPart to the WorkbookPart.
                WorksheetPart worksheetPart1 = workbookpart1.AddNewPart<WorksheetPart>();
                SheetData sheetData1 = new SheetData();
                worksheetPart1.Worksheet = new Worksheet(sheetData1);

                // Add Sheets to the Workbook.
                Sheets sheets1 = spreadsheetDocument1.WorkbookPart.Workbook.
                    AppendChild<Sheets>(new Sheets());

                // Append a new worksheet and associate it with the workbook.
                Sheet sheet1 = new Sheet()
                {
                    Id = spreadsheetDocument1.WorkbookPart.
                    GetIdOfPart(worksheetPart1),
                    SheetId = 1,
                    Name = sheetName
                };
                Row headRow = CreateHeaderRow(headers);
                sheetData1.Append(headRow);
                List<Row> rows = CreateAllRowsFromData(answersList);
                foreach (var row in rows)
                {
                    sheetData1.Append(row);
                }
                sheets1.Append(sheet1);

                workbookpart1.Workbook.Save();
                // Close the document.
                spreadsheetDocument1.Close();
            }
            return memoryStream;
        }
        private Row CreateRow(List<Cell> cells, uint index)
        {
            Row row =  new Row() { RowIndex = index };
            foreach (var cell in cells)
            {
                row.Append(cell);
            }
            return row;
        }
        private Cell CreateCell(string text, string index)
        {
            return new Cell()
            {
                CellReference = index,// "A2",
                DataType = CellValues.String,
                CellValue = new CellValue(text)
            };
        }
        private Row CreateHeaderRow(List<string> headers)
        {
            List<Cell> headerCells = new List<Cell>();
            char startCol = 'A';
            foreach (var item in headers)
            {
                Cell newCell = CreateCell(item, startCol.ToString() + "1");
                headerCells.Add(newCell);
                startCol++;
            }

            return CreateRow(headerCells, 1);
        }
        private List<Row> CreateAllRowsFromData(List<List<string>> data)
        {
            int startRow = 2;
            List<Row> rows = new List<Row>();

            foreach (var row in data)
            {
                char startCol = 'A';
                List<Cell> cells = new List<Cell>();
                foreach (var text in row)
                {
                    Cell cell = CreateCell(text, startCol.ToString() + startRow.ToString());
                    cells.Add(cell);
                    startCol++;
                }
                Row newRow = CreateRow(cells, (uint)startRow);
                rows.Add(newRow);
                startRow++;
            }
            return rows;
        }
    }
}