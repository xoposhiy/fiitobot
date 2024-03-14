using System;
using System.Collections.Generic;
using System.Globalization;
using fiitobot.Services;

namespace fiitobot
{
    public class ContactWithDetails
    {
        public readonly Contact Contact;
        public readonly ContactDetails ContactDetails;

        public static implicit operator Contact(ContactWithDetails c) => c.Contact;

        public ContactWithDetails(Contact contact, ContactDetails details)
        {
            Contact = contact;
            ContactDetails = details;
        }

        public override string ToString()
        {
            return Contact.ToString();
        }

        public IReadOnlyList<ContactDetail> Details => ContactDetails?.Details ?? new List<ContactDetail>();
        public long Id => Contact.Id;
    }

    public class SemesterMarks
    {
        public List<DisciplineMark> Marks = new List<DisciplineMark>();

        /// <summary>
        ///     1..8
        /// </summary>
        public int SemesterNumber;
    }

    public class DisciplineMark
    {
        public string DisciplineName;
        public int? Mark100Grade;
        public string MarkName;

        public DisciplineMark(string disciplineName)
        {
            DisciplineName = disciplineName;
        }

        public override string ToString()
        {
            var s = $"{DisciplineName}: {MarkName}";
            if (Mark100Grade.HasValue)
                s += $" ({Mark100Grade.Value})";
            return s;
        }

        public void ParseAndSetMark(bool isExam, string text)
        {
            var textIsNumber = int.TryParse(
                text.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture,
                out var score);
            Mark100Grade = textIsNumber && score > 5 ? score : (int?)null;
            if (text.StartsWith("отл", StringComparison.OrdinalIgnoreCase)
                || text == "5"
                || (isExam && textIsNumber && score >= 80))
                MarkName = "отл";
            else if (text.StartsWith("хор", StringComparison.OrdinalIgnoreCase)
                     || text == "4"
                     || (isExam && textIsNumber && score < 80 && score >= 60))
                MarkName = "хор";
            else if (text.StartsWith("уд", StringComparison.OrdinalIgnoreCase)
                     || text == "3"
                     || (isExam && textIsNumber && score < 60 && score >= 40))
                MarkName = "уд";
            else if (text.StartsWith("неуд", StringComparison.OrdinalIgnoreCase)
                     || text == "2"
                     || (isExam && textIsNumber && score > 5 && score < 40))
                MarkName = "неуд";
            else if (text.StartsWith("зач", StringComparison.OrdinalIgnoreCase)
                     || (!isExam && textIsNumber && score >= 40))
                MarkName = "зач";
            else if (text.StartsWith("незач", StringComparison.OrdinalIgnoreCase)
                     || (!isExam && textIsNumber && score < 40))
                MarkName = "незач";
            else
                MarkName = text;
        }
    }

    public class ContactDetail
    {
        public string Parameter;

        public string Rubric;
        public DateTime UpdateTime;
        public string Value;

        public ContactDetail(string rubric, string parameter, string value, DateTime updateTime)
        {
            Rubric = rubric;
            Parameter = parameter;
            Value = value;
            UpdateTime = updateTime;
        }
    }
}
