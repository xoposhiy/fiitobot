namespace tgnames
{
    public interface IYdbSettings
    {
        string YdbEndpoint { get; }
        string YdbDatabase { get; }
        string YandexCloudKeyFile { get; }
        string AccessToken { get; }
    }
}
