using System;
using System.Collections.Generic;
using fiitobot.Services;

namespace fiitobot
{
    public class ContactWithDetails
    {
        public ContactWithDetails(Contact contact, ContactDetails details = null)
        {
            Contact = contact;
            ContactDetails = details;
        }

        public readonly Contact Contact;
        public readonly ContactDetails ContactDetails;
        public IReadOnlyList<ContactDetail> Details => ContactDetails?.Details ?? new List<ContactDetail>();
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
