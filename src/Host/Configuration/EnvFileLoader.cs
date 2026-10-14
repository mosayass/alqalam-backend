namespace AlQalam.Host.Configuration;

public static class EnvFileLoader
{
    public static void Load(string fileName = ".env")
    {
        var currentDir = new DirectoryInfo(Directory.GetCurrentDirectory());
        FileInfo? envFile = null;

        for (var i = 0; i < 5 && currentDir is not null; i++)
        {
            var potentialPath = Path.Combine(currentDir.FullName, fileName);
            if (File.Exists(potentialPath))
            {
                envFile = new FileInfo(potentialPath);
                break;
            }

            currentDir = currentDir.Parent;
        }

        if (envFile is null || !envFile.Exists)
        {
            return;
        }

        foreach (var line in File.ReadAllLines(envFile.FullName))
        {
            var trimmed = line.Trim();
            if (string.IsNullOrWhiteSpace(trimmed) || trimmed.StartsWith('#'))
            {
                continue;
            }

            var separatorIndex = trimmed.IndexOf('=');
            if (separatorIndex <= 0)
            {
                continue;
            }

            var key = trimmed[..separatorIndex].Trim();
            var value = trimmed[(separatorIndex + 1)..].Trim();

            if ((value.StartsWith('"') && value.EndsWith('"')) ||
                (value.StartsWith('\'') && value.EndsWith('\'')))
            {
                value = value[1..^1];
            }

            if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable(key)))
            {
                Environment.SetEnvironmentVariable(key, value);
            }
        }
    }
}
