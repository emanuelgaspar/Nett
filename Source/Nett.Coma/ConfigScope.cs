namespace Nett.Coma
{
    public sealed class ConfigScope
    {
        public string ConfigFilePath;

        public ConfigScope(string configFilePath)
        {
            this.ConfigFilePath = configFilePath;
        }
    }
}
