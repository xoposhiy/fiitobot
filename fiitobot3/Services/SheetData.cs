using System.Collections.Generic;

namespace fiitobot.Services
{
    public class SheetData
    {
        public SheetData(string sheetName, int sourceId, string url, List<List<string>> values)
        {
            SheetName = sheetName;
            SourceId = sourceId;
            Url = url;
            Values = values;
        }

        public readonly string SheetName;
        public readonly int SourceId;
        public readonly string Url;
        public readonly List<List<string>> Values;

        public void Deconstruct(out string name, out int sourceId, out string url, out List<List<string>> values)
        {
            name = SheetName;
            sourceId = SourceId;
            values = Values;
            url = Url;
        }
    }
}
