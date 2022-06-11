using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Google.Apis.Http;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;

#nullable enable

namespace fiitobot.GoogleSpreadsheet
{
    public class GSheetEditsBuilder
    {
        private readonly List<Request> requests;
        private readonly SheetsService service;
        private readonly int sheetId;
        private readonly string spreadSheetId;

        public GSheetEditsBuilder(SheetsService service, string spreadSheetId, int sheetId)
        {
            this.service = service;
            this.spreadSheetId = spreadSheetId;
            this.sheetId = sheetId;
            requests = new List<Request>();
        }

        public GSheetEditsBuilder SortRange(ValueTuple<int, int> topLeft, ValueTuple<int, int> bottomRight, params SortSpec[] specs)
        {
            var (topIndex, leftIndex) = topLeft;
            var (bottomIndex, rightIndex) = bottomRight;
            requests.Add(
                new Request
                {
                    SortRange = new SortRangeRequest
                    {
                        Range = new GridRange
                        {
                            SheetId = sheetId,
                            StartRowIndex = topIndex,
                            StartColumnIndex = leftIndex,
                            EndRowIndex = bottomIndex + 1,
                            EndColumnIndex = rightIndex + 1
                        },
                        SortSpecs = specs
                    }
                });
            return this;
        }
        
        public GSheetEditsBuilder WriteRange(ValueTuple<int, int> topLeft, List<List<string>> payload)
        {
            var (topIndex, leftIndex) = topLeft;
            var rows = new List<RowData>();
            foreach (var row in payload)
            {
                var cells = new List<CellData>();
                foreach (var value in row)
                {
                    cells.Add(GetCellData(value));
                }

                rows.Add(
                    new RowData
                    {
                        Values = cells
                    }
                );
            }

            requests.Add(
                new Request
                {
                    UpdateCells = new UpdateCellsRequest
                    {
                        Start = new GridCoordinate
                        {
                            SheetId = sheetId,
                            RowIndex = topIndex,
                            ColumnIndex = leftIndex
                        },
                        Rows = rows,
                        Fields = "*"
                    }
                });
            return this;
        }

        private static CellData GetCellData(string? value)
        {
            if (int.TryParse(value, out var x))
                return new CellData
                {
                    UserEnteredFormat = new CellFormat
                    {
                        NumberFormat = new NumberFormat
                        {
                            Type = "NUMBER",
                            Pattern = "0"
                        }
                    },
                    UserEnteredValue = new ExtendedValue
                    {
                        NumberValue = x
                    }
                };
            if (double.TryParse(value, out var d))
                return new CellData
                {
                    UserEnteredFormat = new CellFormat
                    {
                        NumberFormat = new NumberFormat
                        {
                            Type = "NUMBER",
                            Pattern = "0.00"
                        }
                    },
                    UserEnteredValue = new ExtendedValue
                    {
                        NumberValue = d
                    }
                };
            if (DateTime.TryParse(value, out var dt))
            {
                //var stringValue = dt.ToString("dd.MM.yyyy HH:mm:ss");
                return new CellData
                {
                    UserEnteredFormat = new CellFormat
                    {
                        NumberFormat = new NumberFormat
                        {
                            Type = "DATE_TIME",
                            Pattern = "dd.mm.yyyy hh:mm:ss"
                        }
                    },
                    UserEnteredValue = new ExtendedValue
                    {
                        NumberValue = (dt - new DateTime(1899, 12, 30)).TotalDays
                    }
                };
            }

            return new CellData
            {
                UserEnteredFormat = new CellFormat
                {
                    NumberFormat = new NumberFormat
                    {
                        Type = "TEXT",
                    }
                },
                UserEnteredValue = new ExtendedValue
                {
                    StringValue = value
                }
            };
        }

        private static ExtendedValue GetUserEnteredValue(string? value)
        {
            if (int.TryParse(value, out var x))
                return new ExtendedValue
                {
                    NumberValue = x
                };
            if (double.TryParse(value, out var d))
                return new ExtendedValue
                {
                    NumberValue = d
                };
            return new ExtendedValue
            {
                StringValue = value
            };
        }

        public GSheetEditsBuilder MergeCell(ValueTuple<int, int> topLeft, ValueTuple<int, int> bottomRight)
        {
            var (top, leftIndex) = topLeft;
            var (bottom, rightIndex) = bottomRight;
            requests.Add(
                new Request
                {
                    MergeCells = new MergeCellsRequest
                    {
                        Range = new GridRange
                        {
                            SheetId = sheetId,
                            StartRowIndex = top,
                            StartColumnIndex = leftIndex,
                            EndRowIndex = bottom + 1,
                            EndColumnIndex = rightIndex + 1
                        },
                        MergeType = "MERGE_ALL"
                    }
                });

            return this;
        }

        public GSheetEditsBuilder InsertRows(int startRow, int count)
        {
            requests.Add(
                new Request
                {
                    InsertDimension = new InsertDimensionRequest
                    {
                        Range = new DimensionRange
                        {
                            SheetId = sheetId,
                            StartIndex = startRow,
                            EndIndex = startRow + count,
                            Dimension = "ROWS"
                        }
                    }
                });

            return this;
        }

        public GSheetEditsBuilder InsertColumns(int startColumn, int count)
        {
            requests.Add(
                new Request
                {
                    InsertDimension = new InsertDimensionRequest
                    {
                        Range = new DimensionRange
                        {
                            SheetId = sheetId,
                            StartIndex = startColumn,
                            EndIndex = startColumn + count,
                            Dimension = "COLUMNS"
                        }
                    }
                });

            return this;
        }

        public GSheetEditsBuilder ChangeName(string name)
        {
            requests.Add(
                new Request
                {
                    UpdateSheetProperties = new UpdateSheetPropertiesRequest
                    {
                        Fields = "title",

                        Properties = new SheetProperties
                        {
                            SheetId = sheetId,
                            Title = name,
                        }
                    }
                });
            return this;
        }

        public GSheetEditsBuilder ColorizeRange(ValueTuple<int, int> rangeStart, ValueTuple<int, int> rangeEnd, Color backgroundColor)
        {
            var (top, left) = rangeStart;
            var (bottom, right) = rangeEnd;
            // new
            bottom++;
            right++;
            var rows = new List<RowData>();
            for (var r = top; r < bottom; r++)
            {
                var cells = new List<CellData>();
                for (var c = left; c < right; c++)
                {
                    cells.Add(
                        new CellData
                        {
                            UserEnteredFormat = new CellFormat
                            {
                                BackgroundColor = backgroundColor
                            }
                        });
                }

                rows.Add(
                    new RowData
                    {
                        Values = cells
                    });
            }

            requests.Add(
                new Request
                {
                    UpdateCells = new UpdateCellsRequest
                    {
                        Range = new GridRange
                        {
                            SheetId = sheetId,
                            StartRowIndex = top,
                            StartColumnIndex = left,
                            EndRowIndex = bottom,
                            EndColumnIndex = right
                        },
                        Rows = rows,
                        Fields = "userEnteredFormat(backgroundColor)"
                    }
                });

            return this;
        }

        public GSheetEditsBuilder AddNote(ValueTuple<int, int> rangeStart, string noteText)
        {
            var (top, leftIndex) = rangeStart;
            requests.Add(
                new Request
                {
                    UpdateCells = new UpdateCellsRequest
                    {
                        Range = new GridRange
                        {
                            SheetId = sheetId,
                            StartRowIndex = top,
                            StartColumnIndex = leftIndex,
                            EndRowIndex = top + 1,
                            EndColumnIndex = leftIndex + 1
                        },
                        Rows = new List<RowData>
                        {
                            new RowData
                            {
                                Values = new List<CellData>
                                {
                                    new CellData
                                    {
                                        Note = noteText
                                    }
                                }
                            }
                        },
                        Fields = "note"
                    }
                });

            return this;
        }

        public GSheetEditsBuilder AddBorders(ValueTuple<int, int> rangeStart, ValueTuple<int, int> rangeEnd, Color borderColor)
        {
            var (top, leftIndex) = rangeStart;
            var (bottom, rightIndex) = rangeEnd;
            requests.Add(
                new Request
                {
                    UpdateBorders = new UpdateBordersRequest
                    {
                        Range = new GridRange
                        {
                            SheetId = sheetId,
                            StartRowIndex = top,
                            StartColumnIndex = leftIndex,
                            EndRowIndex = bottom + 1,
                            EndColumnIndex = rightIndex + 1
                        },
                        Top = new Border
                        {
                            Color = borderColor,
                            Style = "SOLID"
                        },
                        Bottom = new Border
                        {
                            Color = borderColor,
                            Style = "SOLID"
                        },
                        Left = new Border
                        {
                            Color = borderColor,
                            Style = "SOLID"
                        },
                        Right = new Border
                        {
                            Color = borderColor,
                            Style = "SOLID"
                        }
                    }
                });
            return this;
        }

        public GSheetEditsBuilder DeleteRows(int startRow, int count)
        {
            requests.Add(
                new Request
                {
                    DeleteDimension = new DeleteDimensionRequest
                    {
                        Range = new DimensionRange
                        {
                            SheetId = sheetId,
                            Dimension = "ROWS",
                            StartIndex = startRow,
                            EndIndex = startRow + count
                        }
                    }
                });
            return this;
        }

        public GSheetEditsBuilder DeleteColumns(int startRow, int count)
        {
            requests.Add(
                new Request
                {
                    DeleteDimension = new DeleteDimensionRequest
                    {
                        Range = new DimensionRange
                        {
                            SheetId = sheetId,
                            Dimension = "COLUMNS",
                            StartIndex = startRow,
                            EndIndex = startRow + count
                        }
                    }
                });
            return this;
        }

        public GSheetEditsBuilder UnMergeAll()
        {
            requests.Add(
                new Request
                {
                    UnmergeCells = new UnmergeCellsRequest
                    {
                        Range = new GridRange
                        {
                            SheetId = sheetId
                        }
                    }
                });
            return this;
        }

        public GSheetEditsBuilder ClearAll()
        {
            requests.Add(
                new Request
                {
                    UpdateCells = new UpdateCellsRequest
                    {
                        Range = new GridRange
                        {
                            SheetId = sheetId
                        },
                        Fields = "*"
                    }
                });
            return this;
        }

        public void Execute()
        {
            var requestBody = new BatchUpdateSpreadsheetRequest {Requests = requests};
            var request = service.Spreadsheets.BatchUpdate(requestBody, spreadSheetId);
            request.AddUnsuccessfulResponseHandler(new ErrorsHandler());
            request.Execute();
        }
    }

    public class ErrorsHandler : IHttpUnsuccessfulResponseHandler
    {
        public Task<bool> HandleResponseAsync(HandleUnsuccessfulResponseArgs args)
        {
            Console.WriteLine("Can't write to Google Sheet. ");
            Console.WriteLine(args.Response.StatusCode + " " + args.Response.ReasonPhrase);
            Console.WriteLine(args.Response.Content.ReadAsStringAsync().Result);
            return Task.FromResult(false);
        }
    }
}
