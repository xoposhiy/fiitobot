﻿using System;

namespace fiitobot
{
    // Create Settings.Production.cs file with concrete values set in default constructor.
    public partial class Settings
    {
        public string GoogleApiKey;
        public string TgToken;
        public long DevopsChatId;
        public string SpreadSheetId;
        public string GoogleAuthJson;
        public string YandexCloudStaticKeyId;
        public string YandexCloudStaticKey;
        public string TgClientApiId;
        public string TgClientApiHash;
        public string TgClientPhoneNumber;
        public string PhotoListUrl;
        public long ModeratorsChatId;
        public string SpreadsheetUrl => $"https://docs.google.com/spreadsheets/d/{SpreadSheetId}";
        public string FaqGoogleDocUrl => $@"https://www.googleapis.com/drive/v3/files/1u0o3GvZKJhSQNkdKKSIrUllu2d5-XnWY6SDNviv7W7E/export?mimeType=text/plain&key={GoogleApiKey}";
        public string ItsLogin;
        public string ItsPassword;

        public string TgClientConfig(string what)
        {
            switch (what)
            {
                case "api_id": return TgClientApiId;
                case "api_hash": return TgClientApiHash;
                case "phone_number": return TgClientPhoneNumber;
                case "verification_code":
                    Console.Write("Code: ");
                    return Console.ReadLine();
                default: return null;
            }
        }
    }
}
