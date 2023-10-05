namespace MessageServices.Helper
{
    internal static class ConfigHelper
    {
        private static readonly string[] _commandArgs = { "configPath","console" };
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
                    .SetBasePath(args["configPath"])
                    .AddJsonFile("appsettings.json", optional: false)
                    .AddEnvironmentVariables()
                    .Build();
        }
        public static void SetNLogConfiguration(string configPath)
        {
            var nlogPath = $"{configPath}\\nlog.config";
            NLog.LogManager.Configuration = new NLog.Config.XmlLoggingConfiguration(nlogPath);
        }
        public static IDictionary<string, string> ParseCommandline(string[] args)
        {
            if (args.Length == 0)
            {
                var arguments = new Dictionary<string, string>
                {
                    { "configPath", System.IO.Directory.GetCurrentDirectory() }
                };
                return arguments;
            }
            var cmdlineArguments = Enumerable.Range(0, args.Length / 2).ToDictionary(i => args[2 * i].Replace("-", ""), i => args[2 * i + 1]);

            foreach (string arg in cmdlineArguments.Keys)
            {
                if (!_commandArgs.Contains(arg))
                    throw new ArgumentException("Invalid argument", arg);
            }
            if (cmdlineArguments.Count == 0 || !cmdlineArguments.ContainsKey("configPath"))
            {
                cmdlineArguments.Add("configPath", System.IO.Directory.GetCurrentDirectory());
            }
            return cmdlineArguments;

        }
    }
}
