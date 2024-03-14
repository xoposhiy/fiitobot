using fiitobot.Services;

namespace fiitobot
{
    public static class ContactExtensions
    {
        public static ContactDetailsLevel GetDetailsLevelFor(this Contact contact, Contact contactViewer)
        {
            // TODO refactor common parts (SecretKeys)
            return contactViewer.Type switch
            {
                ContactType.Administration when contact.TgId == contactViewer.TgId => ContactDetailsLevel.Iddqd | ContactDetailsLevel.SecretKeys,
                ContactType.Administration => ContactDetailsLevel.Iddqd,
                ContactType.Staff when contact.TgId == contactViewer.TgId => ContactDetailsLevel.Minimal | ContactDetailsLevel.Contacts | ContactDetailsLevel.Marks | ContactDetailsLevel.SecretKeys,
                ContactType.Staff => ContactDetailsLevel.Minimal | ContactDetailsLevel.Contacts | ContactDetailsLevel.Marks,
                ContactType.Student when contact.TgId == contactViewer.TgId => ContactDetailsLevel.Minimal | ContactDetailsLevel.Contacts | ContactDetailsLevel.Marks | ContactDetailsLevel.SecretKeys, // что видит про себя
                ContactType.Student when contactViewer.GraduationYear == contact.GraduationYear || contactViewer.AdmissionYear == contact.AdmissionYear => ContactDetailsLevel.Minimal | ContactDetailsLevel.Contacts, // что видит про однопоточников
                ContactType.Student when contact.Type == ContactType.Administration || contact.Type == ContactType.Staff => ContactDetailsLevel.Minimal | ContactDetailsLevel.Contacts, // что видит про преподов и команду ФИИТ
                ContactType.Student => ContactDetailsLevel.Minimal, // что видит про остальных
                _ => ContactDetailsLevel.No
            };
        }
    }
}
