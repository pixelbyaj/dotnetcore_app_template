namespace StartupServices.Helper
{
    internal static class ConfigHelper
    {
        private static readonly string[] _commandArgs = { "config-path", "console" };
        public static IDictionary<string, string>? GetDictionary(IConfigurationSection configurationSection)
        {
            var configs = configurationSection.GetChildren();
            if (configs != null && configs.Any())
            {
                return configs.ToDictionary(x => x.Key, x => x.Value ?? string.Empty);
            }

            return configurationSection.GetChildren()?.ToDictionary(x => x.Key, x => x.Value ?? string.Empty);
        }

        public static IConfigurationRoot SetConfiguration(IDictionary<string, string> args)
        {
            return new ConfigurationBuilder()
                    .SetBasePath(args["config-path"])
                    .AddJsonFile("appsettings.json", optional: false)
                    .AddEnvironmentVariables()
                    .Build();
        }
        public static void SetNLogConfiguration(string configPath)
        {
            var nlogPath = $"{configPath}/nlog.config";
            NLog.LogManager.Configuration = new NLog.Config.XmlLoggingConfiguration(nlogPath);
        }
        public static IDictionary<string, string> ParseCommandline(string[] args)
        {
            if (args.Length == 0)
            {
                var arguments = new Dictionary<string, string>
                {
                    { "config-path", System.IO.Directory.GetCurrentDirectory() }
                };
                return arguments;
            }
            var cmdlineArguments = new Dictionary<string, string>();

            // Loop through each command-line argument
            for (int i = 0; i < args.Length; i++)
            {
                // Check if the argument starts with "--"
                if (args[i].StartsWith("--"))
                {
                    // Get the argument name without the "--" prefix
                    string argName = args[i].Substring(2);

                    // Check if there's another argument after the current one
                    if (i + 1 < args.Length && !args[i + 1].StartsWith("--"))
                    {
                        // If yes, use it as the value
                        cmdlineArguments.Add(argName,args[i + 1]);
                        i++; // Skip the next argument
                    }
                    else
                    {
                        // If no value provided, store an empty string
                        cmdlineArguments.Add(argName, "");
                    }
                }
            }

            return cmdlineArguments;

        }
    }
}
