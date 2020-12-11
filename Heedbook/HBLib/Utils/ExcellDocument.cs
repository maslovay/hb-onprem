using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using HBLib.Model;
using Newtonsoft.Json;

namespace HBLib.Utils
{
    public class ExcellDocument : IDisposable
    {
        private readonly string _documentName;
        private SpreadsheetDocument _document;
        private WorkbookPart _workbookPart;
        public ExcellDocument(string documentName)
        {
            _documentName = documentName;
            _document = SpreadsheetDocument.Create(documentName, DocumentFormat.OpenXml.SpreadsheetDocumentType.Workbook);
        }
        public WorkbookPart AddWorkbookPart()
        {
            WorkbookPart _workbookPart = _document.AddWorkbookPart();
            _workbookPart.Workbook = new Workbook();
            return _workbookPart;
        }
        public WorksheetPart AddWorksheetPart(ref WorkbookPart workbookPart)
        {
            WorksheetPart worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
            worksheetPart.Worksheet = new Worksheet(new SheetData());
            return worksheetPart;
        }
        public SheetData AddSheet(ref WorkbookPart workbookPart, WorksheetPart worksheetPart, string sheetName)
        {
            var sheets = workbookPart.Workbook.AppendChild(new Sheets());
            Sheet sheet = new Sheet()
            { 
                Id = workbookPart.GetIdOfPart(worksheetPart), 
                SheetId = 1, 
                Name = sheetName 
            };
            sheets.Append(sheet);
            SheetData sheetData = worksheetPart.Worksheet.AppendChild(new SheetData());
            return sheetData;
        }
        public void AddRow(SheetData sheetData, params string[] fields)
        {
            Row row = new Row();
            foreach(var item in fields)
                row.Append(ConstructCell(item, CellValues.String));
            sheetData.AppendChild(row);
        }
        public void AddCell(ref SheetData sheetData, string str, int rowIndex, int columnIndex)
        {
            var cell = ConstructCell(str, CellValues.String, rowIndex, columnIndex);
            var rowCount = sheetData.Elements<Row>().Count();
                      
            var row = sheetData.Elements<Row>().FirstOrDefault(r => r.RowIndex == rowIndex);
            if (row == null)
            {
                row = new Row() { RowIndex = (UInt32)rowIndex };
                sheetData.Append(row);
            }
            var columnName = GetExcelColumnName(columnIndex);
            var cellReference = columnName + rowIndex;      // e.g. A1

            // Check if the row contains a cell with the specified column name.
            var existCell = row.Elements<Cell>()
                    .FirstOrDefault(c => c.CellReference.Value == cellReference);
            if (existCell == null)
            {
                if (row.ChildElements.Count < columnIndex)
                    row.AppendChild(cell);
                else
                    row.InsertAt(cell, (int)columnIndex);
            }
            else
                row.InsertAt(cell, columnIndex);
        }
        public void AddCell(ref SheetData sheetData, List<ColoredText> listOfColoredText, SharedStringTablePart sharedStringTablePart, int rowIndex, int columnIndex)
        {
            var cell = ConstructCell(listOfColoredText, sharedStringTablePart, rowIndex, columnIndex);
            var rowCount = sheetData.Elements<Row>().Count();
                      
            var row = sheetData.Elements<Row>().FirstOrDefault(r => r.RowIndex == rowIndex);
            if (row == null)
            {
                row = new Row() { RowIndex = (UInt32)rowIndex };
                sheetData.Append(row);
            }
            var columnName = GetExcelColumnName(columnIndex);
            var cellReference = columnName + rowIndex;      // e.g. A1

            // Check if the row contains a cell with the specified column name.
            var existCell = row.Elements<Cell>()
                    .FirstOrDefault(c => c.CellReference.Value == cellReference);
            if (existCell == null)
            {
                if (row.ChildElements.Count < columnIndex)
                    row.AppendChild(cell);
                else
                    row.InsertAt(cell, (int)columnIndex);
            }
            else
                row.InsertAt(cell, columnIndex);
        }
        public string GetCellValue(SpreadsheetDocument document, Cell cell)
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
        private Cell ConstructCell(string value, CellValues dataType, int? rowIndex = null, int? columnIndex = null)
        {
            string cellReference = null;
            if(columnIndex != null || rowIndex != null)
            {
                cellReference = GetCellReference(rowIndex, columnIndex);
            }

            if(cellReference is null)
                return new Cell()
                {
                    CellValue = new CellValue(value),
                    DataType = new EnumValue<CellValues>(dataType)
                };
            else
                return new Cell()
                {
                    CellValue = new CellValue(value),
                    DataType = new EnumValue<CellValues>(dataType),
                    CellReference = cellReference
                };
        }
        private Cell ConstructCell<T>(ref T model, CellValues dataType, int? rowIndex = null, int? columnIndex = null)
            where T : class
        {     
            string value = "";
            if(model != null)
                value = model.ToString();

            string cellReference = null;
            if(columnIndex != null || rowIndex != null)
            {
                cellReference = GetCellReference(rowIndex, columnIndex);
            }

            if(cellReference is null)
                return new Cell()
                {
                    CellValue = new CellValue(value),
                    DataType = new EnumValue<CellValues>(dataType)
                };
            else
                return new Cell()
                {
                    CellValue = new CellValue(value),
                    DataType = new EnumValue<CellValues>(dataType),
                    CellReference = cellReference
                };
        }
        
        private Cell ConstructCell(List<ColoredText> listOfColoredText, SharedStringTablePart sharedStringTablePart, int? rowIndex = null, int? columnIndex = null)
        {
            string cellReference = null;
            if(columnIndex != null || rowIndex != null)
            {
                cellReference = GetCellReference(rowIndex, columnIndex);
            }
            var cell = new Cell()
            {
                DataType = new EnumValue<CellValues>(CellValues.SharedString),
                CellReference = cellReference
            };

            var sharedStringItem = new SharedStringItem();
            
            foreach(var textItem in listOfColoredText)
            {
                var run = new Run();
                // System.Console.WriteLine($"textItem.Text: {textItem.Text}");
                run.Append(new Text(textItem.Text));
                run.RunProperties = new RunProperties();
                run.RunProperties.Append(
                    new Color()
                    {
                        Rgb = textItem.Colour.Replace("#", string.Empty)
                    },
                    new FontSize()
                    {
                        Val = 7
                    });
                // sharedStringItem.Append(run);
                sharedStringItem.Append(run);                
            }
            sharedStringItem.Append(
                new Alignment
                {
                    Vertical = new EnumValue<VerticalAlignmentValues>(VerticalAlignmentValues.Top),
                    Horizontal = new EnumValue<HorizontalAlignmentValues>(HorizontalAlignmentValues.Justify),
                    WrapText = true
                });

            sharedStringTablePart.SharedStringTable.Append(sharedStringItem);
            var sharedStringItems = sharedStringTablePart.SharedStringTable.Elements<SharedStringItem>();
            int index = 0;
            foreach (var item in sharedStringItems)
            {
                if (item == sharedStringItem)
                {
                    break;
                }
                index++;
            }
            
                cell.AppendChild<CellValue>(new CellValue(index.ToString()));
            return cell;
            // if(cellReference is null)
            //     return new Cell()
            //     {
            //         CellValue = new CellValue(value),
            //         DataType = new EnumValue<CellValues>(dataType)
            //     };
            // else
            //     return new Cell()
            //     {
            //         CellValue = new CellValue(value),
            //         DataType = new EnumValue<CellValues>(dataType),
            //         CellReference = cellReference
            //     };
        }
        private string GetCellReference(int? rowIndex, int? columnIndex)
        {
            var columnName = GetExcelColumnName(columnIndex);
            var cellReference = columnName + rowIndex;
            return cellReference;
        }
        private string GetExcelColumnName(int? columnNumber)
        {
            int dividend = (int)columnNumber;
            string columnName = String.Empty;
            int modulo;

            while (dividend > 0)
            {
                modulo = (dividend - 1) % 26;
                columnName = Convert.ToChar(65 + modulo).ToString() + columnName;
                dividend = (int)((dividend - modulo) / 26);
            } 

            return columnName;
        }
        public void SaveDocument(ref WorkbookPart workbookPart)
        {
            workbookPart.Workbook.Save();
        }
        public void CloseDocument()
        {
            _document.Close();
            Dispose();
        }

        public void Dispose()
        {
            _document.Dispose();
        }
    }
}