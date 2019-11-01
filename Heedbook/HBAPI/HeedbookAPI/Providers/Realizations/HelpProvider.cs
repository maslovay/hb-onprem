using System;
using DocumentFormat.OpenXml.Packaging;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using DocumentFormat.OpenXml.Spreadsheet;
using HBData;
using Microsoft.EntityFrameworkCore;
using HBData.Models;
using DocumentFormat.OpenXml;
using UserOperations.Providers.Interfaces;

namespace UserOperations.Providers.Realizations
{
    public class HelpProvider : IHelpProvider
    {
        private readonly RecordsContext _context;
        public HelpProvider(
            RecordsContext context
        )
        {
            _context = context;
        }

        //-----READ------
        public void AddComanyPhrases()
        {
            var filePath = "/home/oleg/Downloads/Phrases.xlsx";
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
        ///Method for get Cell Value
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
        public void CreateSpreadsheetDocument()
        {
            var fileName = "D:/test.xlsx";
            SpreadsheetDocument spreadsheetDocument = SpreadsheetDocument.
                Create(fileName, SpreadsheetDocumentType.Workbook);

            // Add a WorkbookPart to the document.  
            WorkbookPart workbookpart = spreadsheetDocument.AddWorkbookPart();
            workbookpart.Workbook = new Workbook();

            // Add a WorksheetPart to the WorkbookPart.  
            WorksheetPart worksheetPart = workbookpart.AddNewPart<WorksheetPart>();
            worksheetPart.Worksheet = new Worksheet(new SheetData());

            // Add Sheets to the Workbook.  
            Sheets sheets =
                spreadsheetDocument.WorkbookPart.Workbook.AppendChild<Sheets>(new Sheets());

            // Append a new worksheet and associate it with the workbook.  
            Sheet sheet = new Sheet()
            {
                Id = spreadsheetDocument.WorkbookPart.
                GetIdOfPart(worksheetPart),
                SheetId = 1,
                Name = "mySheet"
            };
            sheets.Append(sheet);

            Worksheet worksheet = new Worksheet();
            SheetData sheetData = new SheetData();

            Row row =
                new Row()
                {
                    RowIndex = 2U,
                    Spans = new ListValue<StringValue>() { InnerText = "2:2" }
                };
            Cell cell =
                new Cell()
                {
                    CellReference = "A2",
                    DataType = CellValues.String,
                    CellValue = new CellValue("Microsoft")
                };
            row.Append(cell);
            sheetData.Append(row);
            worksheet.Append(sheetData);
            worksheetPart.Worksheet = worksheet;

            workbookpart.Workbook.Save();

            // Close the document.  
            spreadsheetDocument.Close();

            Console.WriteLine("The hyperlink has been inserted.\nPress a key.");
            Console.ReadKey();
        }
        private void GenerateSharedStringTablePart1Content(SharedStringTablePart sharedStringTablePart1)
        {
            SharedStringTable sharedStringTable1 = new SharedStringTable() { Count = (UInt32Value)5U, UniqueCount = (UInt32Value)5U };

            SharedStringItem sharedStringItem1 = new SharedStringItem();
            Text text1 = new Text();
            text1.Text = "HeaderVal";

            sharedStringItem1.Append(text1);

            SharedStringItem sharedStringItem2 = new SharedStringItem();
            Text text2 = new Text();
            text2.Text = "HeaderNumber";

            sharedStringItem2.Append(text2);

            SharedStringItem sharedStringItem3 = new SharedStringItem();
            Text text3 = new Text();
            text3.Text = "HeaderLink";

            sharedStringItem3.Append(text3);

            SharedStringItem sharedStringItem4 = new SharedStringItem();
            Text text4 = new Text();
            text4.Text = "test";

            sharedStringItem4.Append(text4);

            SharedStringItem sharedStringItem5 = new SharedStringItem();
            Text text5 = new Text();
            text5.Text = "http://google.com";

            sharedStringItem5.Append(text5);

            sharedStringTable1.Append(sharedStringItem1);
            sharedStringTable1.Append(sharedStringItem2);
            sharedStringTable1.Append(sharedStringItem3);
            sharedStringTable1.Append(sharedStringItem4);
            sharedStringTable1.Append(sharedStringItem5);

            sharedStringTablePart1.SharedStringTable = sharedStringTable1;
        }
        private static void SetCellValue(SharedStringTablePart shareStringTablePart, Cell cell, string value)
        {
            if (cell == null || cell.CellValue.Text.Equals(string.Empty))
            {
                return;
            }
            else
            {
                if (cell.DataType.Value == CellValues.SharedString)
                {
                    int shareStringId = int.Parse(cell.CellValue.Text);
                    SharedStringItem item = shareStringTablePart.SharedStringTable.Elements<SharedStringItem>().ElementAt(shareStringId);

                    item.Elements<Text>().First().Text = value;

                }
                else
                {
                    cell.CellValue.Text = value;
                }
            }
        }

    }
}
