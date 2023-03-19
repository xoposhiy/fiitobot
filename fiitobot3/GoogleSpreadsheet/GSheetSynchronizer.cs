using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Reflection;

namespace fiitobot.GoogleSpreadsheet
{
    [SuppressMessage("ReSharper", "StaticMemberInGenericType")]
    public class GSheetSynchronizer<TRecord, TId>
        where TRecord: class
        where TId : IComparable<TId>
    {
        private readonly GSheet sheet;
        private readonly Func<TRecord, TId> getId;
        private static readonly FieldInfo[] Fields;
        private List<List<string>> data;

        static GSheetSynchronizer()
        {
            Fields = typeof(TRecord).GetFields(BindingFlags.Public | BindingFlags.Instance);
        }
        
        public GSheetSynchronizer(GSheet sheet, Func<TRecord, TId> getId)
        {
            this.sheet = sheet;
            this.getId = getId;
        }

        public List<TRecord> LoadSheet(Func<TRecord> recordFactory)
        {
            data = sheet.ReadRange("A1:ZZ");
            var (_, records) = ParseRecords(recordFactory, data);
            return records;
        }

        private (Dictionary<string, int> headers, List<TRecord> records) ParseRecords(Func<TRecord> recordFactory,
            List<List<string>> rowsOfCells)
        {
            var headers = rowsOfCells[0]
                .TakeWhile(s => !string.IsNullOrWhiteSpace(s)).Select((h, i) => (h, i))
                .ToDictionary(th => th.h.ToLowerInvariant(), h => h.i);
            var records = rowsOfCells.Skip(1).Select(row => FillRecordFieldsFromRow(recordFactory(), row, headers)).ToList();
            return (headers, records);
        }

        public void UpdateSheet(Func<TRecord> recordFactory, List<TRecord> recordsToUpdate)
        {
            if (data == null)
                throw new Exception("LoadSheet must be called just before UpdateSheet");
            var (headers, oldRecords) = ParseRecords(recordFactory, data);
            var newRecords = recordsToUpdate.ToDictionary(getId);
            var editBuilder = sheet.Edit();
            int index = 1;
            foreach (var oldRecord in oldRecords)
            {
                var id = getId(oldRecord);
                if (newRecords.TryGetValue(id, out var newRecord))
                {
                    UpdateRecord(index, oldRecord, newRecord, headers, editBuilder);
                    newRecords.Remove(id);
                }
                index++;
            }
            foreach (var newRecord in newRecords.Values)
            {
                UpdateRecord(index, null, newRecord, headers, editBuilder);
                index++;
            }
            editBuilder.Execute();
        }

        private TRecord FillRecordFieldsFromRow(TRecord record, List<string> row, Dictionary<string, int> headerPos)
        {
            foreach (var (field, fieldPos) in GetFieldsWithColPosition(headerPos))
            {
                try
                {
                    var value = GetRowValue(field.FieldType, row, fieldPos);
                    field.SetValue(record, value);
                }
                catch (Exception e)
                {
                    throw new FormatException($"Field {field.Name} in {typeof(TRecord)}. {e.Message}", e);
                }
            }
            return record;
        }

        private object GetRowValue(Type type, List<string> row, int fieldPos)
        {
            if (fieldPos >= row.Count)
                return type.GetDefaultValue();
            var value = row[fieldPos];
            try
            {
                return Convert(type, value);
            }
            catch (Exception e)
            {
                throw new FormatException($"Incorrect value {value}. {e.Message}", e);
            }
        }

        private static object Convert(Type type, string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return type.GetDefaultValue();
            if (type == typeof(string))
                return value;
            if (type == typeof(int))
                return int.Parse(value);
            if (type == typeof(long))
                return long.Parse(value);
            if (type == typeof(double))
                return double.Parse(value.Replace(",", "."), CultureInfo.InvariantCulture);
            if (type == typeof(DateTime))
                return DateTime.Parse(value);
            var notNullableType = Nullable.GetUnderlyingType(type);
            if (notNullableType != null)
            {
                if (string.IsNullOrWhiteSpace(value))
                    return null;
                return Convert(notNullableType, value);
            }
            throw new Exception($"Unsupported type {type}");
        }

        private void UpdateRecord(int rowIndex, TRecord oldRecord, TRecord record, Dictionary<string, int> headers, GSheetEditsBuilder editBuilder)
        {
            foreach (var (field, colPos) in GetFieldsWithColPosition(headers))
            {
                var value = field.GetValue(record);
                if (oldRecord != null)
                {
                    var oldValue = field.GetValue(oldRecord);
                    if (Equals(oldValue, value)) continue;
                }
                editBuilder.WriteRangeNoCasts(
                    (rowIndex, colPos),
                    new List<List<object>> { new List<object> { value } });
            }
        }

        private IEnumerable<(FieldInfo field, int colPos)> GetFieldsWithColPosition(Dictionary<string, int> headers)
        {
            foreach (var (fieldName, fieldPos) in headers)
            {
                var field = Fields.FirstOrDefault(f =>
                    f.Name.Equals(fieldName, StringComparison.InvariantCultureIgnoreCase));
                if (field == null)
                    throw new FormatException($"{fieldName} not found in {typeof(TRecord)}");
                yield return (field, fieldPos);
            }
        }
    }
}
