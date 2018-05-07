namespace Mattersight.mock.ba.ae
{
    /// <summary>
    /// This is in no way close to proper.  I'm just putting it here for this POC since it is a common library useable by everything.
    /// </summary>
    public class Configuration
    {
        // It isn't proper to include a type (SMSProvider) in the name, but I'm doing it for ease of troubleshooting.  
        // SMSProvider is showing up the logs so I want to see it in the constant's name to easily find it.
        // ReSharper disable once InconsistentNaming
        public const string OrleansStreamProviderName_SMSProvider = "SMSProvider";
    }
}
