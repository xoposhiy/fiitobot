using fiitobot.Services;

namespace fiitobot
{
    public static class ContactExtensions
    {
        public static ContactDetailsLevel GetDetailsLevelFor(this Contact contact, Contact contactViewer)
        {
            return contactViewer.Type switch
            {
                _ when contactViewer.TgId == 33598070 => ContactDetailsLevel.Iddqd, // @xoposhiy id
                ContactType.Administration => ContactDetailsLevel.Minimal | ContactDetailsLevel.Contacts | ContactDetailsLevel.LinksToFiitTeamFiles | ContactDetailsLevel.Marks | ContactDetailsLevel.SecretNote | ContactDetailsLevel.TechnicalInfo,
                ContactType.Staff => ContactDetailsLevel.Minimal | ContactDetailsLevel.Contacts | ContactDetailsLevel.Marks,
                ContactType.Student when contactViewer.AdmissionYear == contact.AdmissionYear => ContactDetailsLevel.Minimal | ContactDetailsLevel.Contacts,
                ContactType.Student => ContactDetailsLevel.Minimal,
                _ => ContactDetailsLevel.No
            };
        }
    }
}
