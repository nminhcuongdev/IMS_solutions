namespace Ims.Api.Hosting;

internal static class SecretsConfigurationExtensions
{
    public const string FileName = "appsettings.Secrets.json";

    public static ConfigurationManager AddImsSecrets(this ConfigurationManager configuration)
    {
        configuration.AddJsonFile(FileName, optional: true, reloadOnChange: false);
        return configuration;
    }
}
