using System;
using System.Collections.Generic;

namespace fiitobot
{
    public class ContactWithDetails
    {
        public ContactWithDetails(Contact contact)
            : this(contact, new List<ContactDetail>())
        {
        }

        public ContactWithDetails(Contact contact, List<ContactDetail> details)
        {
            Contact = contact;
            Details = details;
        }

        public readonly Contact Contact;
        public readonly List<ContactDetail> Details;
    }
    
    public class ContactDetail
    {
        public ContactDetail(string rubric, string parameter, string value, DateTime updateTime)
        {
            Rubric = rubric;
            Parameter = parameter;
            Value = value;
            UpdateTime = updateTime;
        }

        public string Rubric;
        public string Parameter;
        public string Value;
        public DateTime UpdateTime;
    };
}
