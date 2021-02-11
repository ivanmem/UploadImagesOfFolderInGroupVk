using System;
using System.IO;
using Newtonsoft.Json;

namespace UploadImagesOfFolderInGroupVk
{
    [Serializable]
    public class Config
    {
        public string[] AccessTokens = Array.Empty<string>();
        public long GroupId;
        public long AlbumId;
        public long Skip = 0;
        public string? PathFiles;
        private static string _settingsPath = Path.Combine(Directory.GetCurrentDirectory(), "settings.json");

        public static Config Get()
        {
            static void Error()
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Заполните конфигурацию.");
                Console.ReadLine();
                Environment.Exit(0);
            }
            
            if (!File.Exists(_settingsPath))
            {
                var newConfig = new Config
                {
                    Skip = 0,
                    AccessTokens = new [] {"123"},
                    AlbumId = 0,
                    GroupId = 0,
                    PathFiles = Directory.GetCurrentDirectory()
                };
                File.WriteAllText(_settingsPath, JsonConvert.SerializeObject(newConfig));
                Error();
            }

            try
            {
                var config = JsonConvert.DeserializeObject<Config>(File.ReadAllText(_settingsPath));
                if (config.AlbumId == 0 || config.GroupId == 0 || config.AccessTokens.Length == 0)
                {
                    Error();
                }

                config.PathFiles ??= Directory.GetCurrentDirectory();
                return config;
            }
            catch
            {
                Error();
            }
            
            // Сюда не дойдёт, ибо в случае ошибки консолька выключается
            return new ();
        }

        public static void Save(Config config)
        {
            var jsonConfig = JsonConvert.SerializeObject(config, Formatting.Indented);
            File.WriteAllText(_settingsPath, jsonConfig);
        }
    }
}