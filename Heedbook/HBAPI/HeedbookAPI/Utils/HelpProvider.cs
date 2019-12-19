using System;
using DocumentFormat.OpenXml.Packaging;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using DocumentFormat.OpenXml.Spreadsheet;
using HBData;
using Microsoft.EntityFrameworkCore;
using HBData.Models;
using DocumentFormat.OpenXml;
using UserOperations.Models.AnalyticModels;

namespace UserOperations.Utils
{
    public class HelpProvider
    {
        private readonly RecordsContext _context;
        public HelpProvider( RecordsContext context )
        {
            _context = context;
            //xlsx
        }

        public void AddComanyPhrases(string filePath)
        {
            using (FileStream FS = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                using (SpreadsheetDocument doc = SpreadsheetDocument.Open(filePath, false))
                {
                    System.Console.WriteLine();
                    WorkbookPart workbook = doc.WorkbookPart;
                    SharedStringTablePart sstpart = workbook.GetPartsOfType<SharedStringTablePart>().First();
                    SharedStringTable sst = sstpart.SharedStringTable;

                    WorksheetPart worksheet = workbook.WorksheetParts.First();
                    Worksheet sheet = worksheet.Worksheet;

                    var cells = sheet.Descendants<Cell>();
                    var rows = sheet.Descendants<Row>();

                    var phrases = _context.Phrases
                        .Include(p => p.PhraseType)
                        .ToList();
                    var phraseTypes = _context.PhraseTypes.ToList();

                    var user = _context.ApplicationUsers
                        .Include(p => p.Company)
                        .FirstOrDefault(p => p.FullName == "Сотрудник с бейджем №1");


                    foreach (var row in rows)
                    {
                        try
                        {
                            //var rowCells = row.Elements<Cell>();
                            var phraseTextString = GetCellValue(doc, row.Descendants<Cell>().ElementAt(0));
                            var phraseTypeString = GetCellValue(doc, row.Descendants<Cell>().ElementAt(1));
                            var existPhrase = phrases.FirstOrDefault(p => p.PhraseText == phraseTextString
                                    && p.PhraseType.PhraseTypeText == phraseTypeString);

                            var phraseType = phraseTypes.FirstOrDefault(p => p.PhraseTypeText == GetCellValue(doc, row.Descendants<Cell>().ElementAt(1)));
                            if (phraseType is null)
                                continue;

                            if (existPhrase == null)
                            {
                                System.Console.WriteLine($"phrase not exist in base");
                                var newPhrase = new Phrase
                                {
                                    PhraseId = Guid.NewGuid(),
                                    PhraseText = GetCellValue(doc, row.Descendants<Cell>().ElementAt(0)),
                                    PhraseTypeId = phraseType.PhraseTypeId,
                                    LanguageId = 2,
                                    IsClient = false,
                                    WordsSpace = 1,
                                    Accurancy = 1,
                                    IsTemplate = false
                                };
                                var phraseCompany = new PhraseCompany
                                {
                                    PhraseCompanyId = Guid.NewGuid(),
                                    PhraseId = newPhrase.PhraseId,
                                    CompanyId = user.CompanyId
                                };
                                System.Console.WriteLine($"Phrase: {newPhrase.PhraseText} - {newPhrase.PhraseTypeId}");
                                _context.Phrases.Add(newPhrase);
                                _context.PhraseCompanys.Add(phraseCompany);
                            }
                            else
                            {
                                var phraseCompany = new PhraseCompany
                                {
                                    PhraseCompanyId = Guid.NewGuid(),
                                    PhraseId = existPhrase.PhraseId,
                                    CompanyId = user.CompanyId
                                };
                                System.Console.WriteLine($"Phrase: {existPhrase.PhraseText} - {existPhrase.PhraseTypeId}");
                                _context.PhraseCompanys.Add(phraseCompany);
                                System.Console.WriteLine($"phrase exist in base");
                            }
                        }
                        catch (NullReferenceException ex)
                        {
                            System.Console.WriteLine($"exception!!");
                            break;
                        }
                    }
                    _context.SaveChanges();
                }
            }
        }
        public MemoryStream CreatePoolAnswersSheet(List<AnswerInfo> answers, string sheetName)
        {
            var answersModified = answers.SelectMany(x => x.Answers.Select(p => new { p.Answer, p.ContentId, p.DialogueId, p.Time, x.ContentName })).ToList();
            List<List<string>> answersList = new List<List<string>>();
            foreach (var answ in answersModified)
            {
                answersList.Add(new List<string> { answ.Time.ToString(), answ.ContentId.ToString(), answ.ContentName, answ.DialogueId.ToString(), answ.Answer });
            }
            return CreateSpreadsheetDocument(sheetName, answersList, new List<string> { "Time", "ContentId", "ContentName", "DialogueId", "Answer" });
        }

        //-----READ------
        ///Method for read xlsx table
        private static string GetCellValue(SpreadsheetDocument document, Cell cell)
        {
            SharedStringTablePart stringTablePart = document.WorkbookPart.SharedStringTablePart;
            string value = cell.CellValue.InnerXml;

            if (cell.DataType != null && cell.DataType.Value == CellValues.SharedString)
            {
                return stringTablePart.SharedStringTable.ChildElements[Int32.Parse(value)].InnerText;
            }
            else
            {
                return value;
            }
        }

        //-----CREATE----------
        ///Methods for create xlsx table
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
