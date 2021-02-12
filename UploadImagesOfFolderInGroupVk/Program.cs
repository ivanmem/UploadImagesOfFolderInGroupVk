using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using VkNet;
using VkNet.Abstractions;
using VkNet.Exception;

namespace UploadImagesOfFolderInGroupVk
{
    class Program
    {
        public static IVkApi Api
        {
            get
            {
                _prevApiIndex++;
                if (_prevApiIndex >= Apis.Length) _prevApiIndex = 0;
                return Apis[_prevApiIndex];
            }
        }

        private static int _prevApiIndex = -1;
        public static IVkApi[] Apis = Array.Empty<IVkApi>();

        static void Main()
        {
            var conf = Config.Get();
            Console.WriteLine($@"
AccessTokens Length: {conf.AccessTokens.Length}
PathFiles: {conf.PathFiles}
GroupId: {conf.GroupId}
AlbumId: {conf.AlbumId}
Skip: {conf.Skip}"
            );
            Apis = conf.AccessTokens.Select(x =>
            {
                var api = new VkApi();
                api.Authorize(new ()
                {
                    AccessToken = x.Split(' ').First().Trim()
                });
                return api;
            }).ToArray();
            var files = Directory.GetFiles(conf.PathFiles!);
            Console.WriteLine($"Найдено {files.Length} файлов.");
            var wc = new WebClient();
            var newLine = true;
            var i = conf.Skip;
            for (; i < files.Length; i++)
            {
                if (Apis.Length == 0)
                {
                    Console.WriteLine($"Токены без лимита закончились. Всего выполнилось: {i-1}.");
                    i--;
                    break;
                }
                
                var file = files[i];
                var api = Api;
                try
                {
                    var uploadServer = api.Photo.GetUploadServer(conf.AlbumId, conf.GroupId);
                    var responseImg = Encoding.ASCII.GetString(wc.UploadFile(uploadServer.UploadUrl, file));
                    api.Photo.Save(new()
                    {
                        GroupId = conf.GroupId,
                        AlbumId = conf.AlbumId,
                        SaveFileResponse = responseImg
                    });
                }
                catch (Exception ex)
                {
                    newLine = true;
                    Console.WriteLine();
                    if (ex is ParameterMissingOrInvalidException)
                    {
                        Console.WriteLine($"Ошибка загрузки {file}. Текущий индекc: {i}. Пропущено.");
                        continue;
                    }
                    
                    i--;
                    if (ex is TooManyRequestsException)
                    {
                        Apis = Apis.Where(x => x.Token != api.Token).ToArray();
                        Console.WriteLine($"Токен {new string(api.Token.Take(5).ToArray())} удалён из-за TooManyRequestsException. Текущий индекc: {i}.");
                        continue;
                    }

                    if (ex is TooMuchOfTheSameTypeOfActionException)
                    {
                        Apis = Apis.Where(x => x.Token != api.Token).ToArray();
                        Console.WriteLine($"Токен {new string(api.Token.Take(5).ToArray())} удалён из-за TooMuchOfTheSameTypeOfActionException");
                        continue;
                    }
                    Console.WriteLine($"Ошибка загрузки {file}. Текущий индекc: {i}.");
                    continue;
                }
                
                var str = $"На текущий момент загружено {i} фотографий.";
                if (newLine) Console.WriteLine();
                Console.SetCursorPosition(0, Console.CursorTop);
                Console.Write(str);
                newLine = false;
            }

            Console.WriteLine();
            conf.Skip = i;
            Config.Save(conf);
            Console.WriteLine(@$"Загрузка завершена. Skip сохранён: {conf.Skip}.
Последнее загруженное фото: {files[i]}
Следующее будет: {files[i+1]}
");
            Console.ReadLine();
        }
    }
}