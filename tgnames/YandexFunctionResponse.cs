namespace tgnames
{
    public class YandexFunctionResponse
    {
        public YandexFunctionResponse(int statusCode, string body)
        {
            StatusCode = statusCode;
            Body = body;
        }

        public int StatusCode { get; set; }
        public string Body { get; set; }
    }
}
