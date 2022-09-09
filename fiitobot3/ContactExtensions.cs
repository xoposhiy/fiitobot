using fiitobot.Services;

namespace fiitobot
{
    public static class ContactExtensions
    {
        public static ContactDetailsLevel GetDetailsLevelFor(this Contact contact, Contact contactViewer)
        {
            return contactViewer.Type switch
            {
                ContactType.Administration => ContactDetailsLevel.Iddqd,
                ContactType.Staff => ContactDetailsLevel.Minimal | ContactDetailsLevel.Contacts | ContactDetailsLevel.Marks,
                ContactType.Student when contact.TgId == contactViewer.TgId => ContactDetailsLevel.Minimal | ContactDetailsLevel.Contacts | ContactDetailsLevel.Marks, // что видит про себя
                ContactType.Student when contactViewer.AdmissionYear == contact.AdmissionYear => ContactDetailsLevel.Minimal | ContactDetailsLevel.Contacts, // что видит про однопоточников
                ContactType.Student => ContactDetailsLevel.Minimal, // что видит про остальных
                _ => ContactDetailsLevel.No
            };
        }
    }
}
