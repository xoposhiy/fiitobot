using System;
using System.Collections.Generic;
using System.Linq;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;

namespace fiitobot.GoogleSpreadsheet
{
    public class GSpreadsheet
    {
        public GSpreadsheet(string spreadsheetId, SheetsService sheetsService)
        {
            SpreadsheetId = spreadsheetId;
            SheetsService = sheetsService;
        }

        public readonly string SpreadsheetId;
        public readonly SheetsService SheetsService;

        public List<GSheet> GetSheets()
        {
            try
            {
                var metadata = SheetsService.Spreadsheets.Get(SpreadsheetId).Execute();
                var sheets = metadata.Sheets.Select(x =>
                    new GSheet(SpreadsheetId, x.Properties.Title, x.Properties.SheetId ?? 0, SheetsService));
                return sheets.ToList();
            }
            catch (Exception e)
            {
                throw new Exception($"Can't get sheets of {SpreadsheetId}", e);
            }
        }

        public GSheet GetSheetById(int sheetId)
        {
            return GetSheets().First(s => s.SheetId == sheetId);
        }

        public GSheet GetSheetByName(string sheetName)
        {
            return GetSheets().First(s => s.SheetName == sheetName);
        }

        public void CreateNewSheet(string title)
        {
            var requests = new List<Request>
            {
                new Request()
                {
                    AddSheet = new AddSheetRequest
                    {
                        Properties = new SheetProperties
                        {
                            Title = title,
                            TabColor = new Color {Red = 1}
                        }
                    }
                }
            };
            var requestBody = new BatchUpdateSpreadsheetRequest {Requests = requests};
            var request = SheetsService.Spreadsheets.BatchUpdate(requestBody, SpreadsheetId);
            var response = request.Execute();
        }
    }
}
