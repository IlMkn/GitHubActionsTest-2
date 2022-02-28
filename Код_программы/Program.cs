using System;
using System.IO;
using LibGit2Sharp;
using LibGit2Sharp.Handlers;
using System.Net.Http;
using System.Net.Http.Headers;
using Newtonsoft.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Net;
using System.IO.Compression;

namespace tempApp
{
    class Program
    {
        static void Copy(string sourceDir, string targetDir)//копирует содержимое директории в временную директорию, используемую для remote push
        {

            Directory.CreateDirectory(targetDir);
            foreach (var file in Directory.GetFiles(sourceDir))
                File.Copy(file, Path.Combine(targetDir, Path.GetFileName(file)));
            foreach (var directory in Directory.GetDirectories(sourceDir))
                Copy(directory, Path.Combine(targetDir, Path.GetFileName(directory)));
        }


        static void Delete(DirectoryInfo deleteDir)//лишает директорию и все файлы в ней атрибута "только для чтения"
        {
            deleteDir.Attributes &= ~FileAttributes.ReadOnly;

            foreach (var dir in deleteDir.GetDirectories())
            {
                Delete(dir);
                dir.Attributes &= ~FileAttributes.ReadOnly;
            }
            foreach (var fi in deleteDir.GetFiles())
            {
                fi.Attributes = FileAttributes.Normal;
            }

        }


        static async Task Main(string[] args)
        {
            //К заполнению
            string sourseDir = @"C:\testDirectory\sourceDir";//директория, содержащая код для тестирования и папку .github с .yml файлом для запуска workflow
            string targetDir = @"C:\testDirectory2\targetDir";//директория, создаваемая для инициализации git репозитория и помещения в неё кода для тестирования
            string deleteDir = @"C:\testDirectory2";//удаление созданной временной директории
            string username = "Sampletext";//данные для коммита(является комментарием к коммиту)
            string email = "Sampletext";//данные для коммита(является комментарием к коммиту)
            string USERNAME = "USERNAME";//имя пользователя для аутентификации
            string PASSWORD = "Personal Access Token";//используется вместо пароля(PAT с доступом к репозиторию и workflow)
            string url = "https://github.com/IlMkn/GitHubActionsTest-2";//url github репозитория, с которым будет проходить работа
            string concurl;//url для получения артефакта со статусом выполнения workflow
            string durl;//url для получения артефакта с тестом
            string query = "https://api.github.com/repos/IlMkn/GitHubActionsTest-2/actions/artifacts";//url для получения артефакта

            try
            {
                Program.Copy(sourseDir, targetDir);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Не удалось создать директорию и скопировать в неё файлы(директория уже существует)");
            }
            Repository.Init(targetDir);//git init
            Repository repo = new Repository(targetDir);

            Commands.Stage(repo, "*");//git add *
            repo.Branches.Add("temp", repo.Commit("sampletext", new Signature(username, email, DateTimeOffset.Now), new Signature(username, email, DateTimeOffset.Now), new CommitOptions()));//добавляет ветку в репозитории
            Commands.Checkout(repo, "temp");//git checkout "branchname"

            repo.Network.Remotes.Add("origin2", url);//add remote
            Remote remote = repo.Network.Remotes["origin2"];

            repo.Branches.Update(repo.Branches["temp"], b => b.Remote = remote.Name,
                                                        b => b.UpstreamBranch = "temp");

            var creds = new UsernamePasswordCredentials()
            {
                Username = USERNAME,
                Password = PASSWORD
            };
            CredentialsHandler credHandler = (_url, _user, _cred) => creds;
            var options = new PushOptions() { CredentialsProvider = credHandler };

            repo.Network.Push(remote, @"refs/heads/temp", options);//создание и пуш временной ветки


            repo.Dispose();//удаляет зависимости 

            var di = new DirectoryInfo(deleteDir);

            Delete(di);
            Directory.Delete(deleteDir, true);//удаление временной директории

            Console.WriteLine("Ожидание завершения WorkFlow(120с)");
            System.Threading.Thread.Sleep(120000);

            HttpClient clienthttp;

            clienthttp = new HttpClient();
            clienthttp.BaseAddress = new Uri("https://api.github.com");

            clienthttp.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Token", PASSWORD);

            clienthttp.DefaultRequestHeaders.Add(
                "User-Agent",
                "Mozilla/5.0 (Windows NT 6.1) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/41.0.2228.0 Safari/537.36");



            clienthttp.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github.v3+json"));


            //получение артефакта workflow, который говорит о статусе его выполнения, и получение файла теста в том случае, если workflow был выполнен
            Console.WriteLine();
            try
            {
                HttpResponseMessage response = await clienthttp.GetAsync(query);//получается список артефактов
                response.EnsureSuccessStatusCode();
                string responseBody = await response.Content.ReadAsStringAsync();
                var RCR = JsonConvert.DeserializeObject<Root>(responseBody);
                int i = 0;
                while (RCR.artifacts[i].name != "conc")
                {
                    i++;
                }
                concurl = RCR.artifacts[i].archive_download_url;

                try
                {
                    var response2 = await clienthttp.GetAsync(@concurl + "?filename=conc.zip");
                    using (var stream = await response2.Content.ReadAsStreamAsync())
                    {
                        var fileInfo = new FileInfo("conc.zip");
                        using (var fileStream = fileInfo.OpenWrite())
                        {
                            await stream.CopyToAsync(fileStream);
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("Возникла ошибка при скачивании {0}", e.Message);
                }
                ZipFile.ExtractToDirectory("conc.zip", "conc.txt");
                string text = System.IO.File.ReadAllText(@"conc.txt\conclusion.txt");
                if (text.Contains("0+0") || text.Contains("0+2"))
                {
                    int j = 0;

                    while (RCR.artifacts[j].name != "test-results")
                    {
                        j++;
                    }

                    durl = RCR.artifacts[j].archive_download_url;
                    try
                    {
                        var response1 = await clienthttp.GetAsync(@durl + "?filename=test-results.zip");
                        using (var stream = await response1.Content.ReadAsStreamAsync())
                        {
                            var fileInfo = new FileInfo("test-results.zip");
                            using (var fileStream = fileInfo.OpenWrite())
                            {
                                await stream.CopyToAsync(fileStream);
                            }
                        }
                        Console.WriteLine("Результат тестирования получен");
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Возникла ошибка при скачивании {0}", e.Message);
                    }
                }
                else
                {
                    Console.WriteLine("Тесты не были пройдены");
                }
            }
            catch (HttpRequestException e)
            {
                Console.WriteLine("\nОшибка в поиске конкретного репозитория");
                Console.WriteLine("Message :{0} ", e.Message);
            }


        }
    }
}
