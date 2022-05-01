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
using System.Configuration;

namespace tempApp
{
    public class Config
    {
        public string PersonalAccessToken { get; set; }
        public string User { get; set; }
        public string Repo { get; set; }
        public string SourceDir { get; set; }
        public string targetDir { get; set; }
        public string deleteDir { get; set; }
    }

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
            string @tempjs = File.ReadAllText("jsonConfig.json");
            var config = JsonConvert.DeserializeObject<Config>(tempjs);

            string sourceDirPath = Path.GetFullPath(@config.SourceDir);
            sourceDirPath = Path.GetFullPath(Path.Combine(@sourceDirPath, @"..\..\..\..\..\"));
            sourceDirPath = Path.GetFullPath(Path.Combine(@sourceDirPath, @config.SourceDir));
            string targetDirPath = Path.GetFullPath(@config.targetDir);
            targetDirPath = Path.GetFullPath(Path.Combine(@targetDirPath, @"..\..\..\..\..\..\"));
            targetDirPath = Path.GetFullPath(Path.Combine(@targetDirPath, @config.targetDir));
            string deleteDirPath = Path.GetFullPath(@config.deleteDir);
            deleteDirPath = Path.GetFullPath(Path.Combine(@deleteDirPath, @"..\..\..\..\..\"));
            deleteDirPath = Path.GetFullPath(Path.Combine(@deleteDirPath, @config.deleteDir));

            try
            {
                Program.Copy(sourceDirPath, targetDirPath);
            }
            catch (Exception)
            {
                Console.WriteLine("Не удалось создать директорию и скопировать в неё файлы(директория уже существует)");
            }

            try
            {
                Repository.Init(targetDirPath);//git init
                Repository repo = new Repository(targetDirPath);
                Commands.Stage(repo, "*");//git add *
                repo.Branches.Add("temp", repo.Commit("sampletext", new Signature("Sampletext", "Sampletext", DateTimeOffset.Now), new Signature("Sampletext", "Sampletext", DateTimeOffset.Now), new CommitOptions()));
                Commands.Checkout(repo, "temp");//git checkout "branchname"
                repo.Network.Remotes.Add("origin2", "https://github.com/" + config.Repo);//add remote
                Remote remote = repo.Network.Remotes["origin2"];
                repo.Branches.Update(repo.Branches["temp"], b => b.Remote = remote.Name,
                                                            b => b.UpstreamBranch = "temp");
                var creds = new UsernamePasswordCredentials()
                {
                    Username = config.User,
                    Password = config.PersonalAccessToken
                };
                CredentialsHandler credHandler = (_url, _user, _cred) => creds;
                var options = new PushOptions() { CredentialsProvider = credHandler };

                repo.Network.Push(remote, @"refs/heads/temp", options);//создание и пуш временной ветки
                repo.Dispose();//удаляет зависимости
            }
            catch (Exception)
            {
                Console.WriteLine("Ошибка в создании git репозитория или пуша ветки");
            }

            var di = new DirectoryInfo(deleteDirPath);

            Delete(di);
            Directory.Delete(deleteDirPath, true);//удаление временной директории

            Console.WriteLine("Ожидание завершения WorkFlow(120с)");
            System.Threading.Thread.Sleep(120000);

            HttpClient clienthttp;

            clienthttp = new HttpClient();
            clienthttp.BaseAddress = new Uri("https://api.github.com");

            clienthttp.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Token", config.PersonalAccessToken);

            clienthttp.DefaultRequestHeaders.Add(
                "User-Agent",
                "Mozilla/5.0 (Windows NT 6.1) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/41.0.2228.0 Safari/537.36");

            clienthttp.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github.v3+json"));

            string concurl;//url для получения артефакта со статусом выполнения workflow
            string durl;//url для получения артефакта с тестом
            string query;//url для получения артефакта
            //query = "https://api.github.com/repos/" + ConfigurationManager.AppSettings["Repo"] + "/actions/artifacts";
            query = "https://api.github.com/repos/" + config.Repo + "/actions/artifacts";

            //получение артефакта workflow, который говорит о статусе его выполнения, и получение файла теста в том случае, если workflow был выполнен
            Console.WriteLine();
            try
            {
                HttpResponseMessage response = await clienthttp.GetAsync(query);
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

                if (!Directory.Exists("conclusion"))
                {
                    Directory.CreateDirectory("conclusion");
                }

                string extractPath = Path.GetFullPath("conclusion");

                if (!extractPath.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal))
                    extractPath += Path.DirectorySeparatorChar;

                using (ZipArchive archive = ZipFile.OpenRead("conc.zip"))
                {
                    foreach (ZipArchiveEntry entry in archive.Entries)
                    {
                        if (entry.FullName=="conclusion.txt")
                        {
                            string destinationPath = Path.GetFullPath(Path.Combine(extractPath, entry.FullName));
                            if (destinationPath.StartsWith(extractPath, StringComparison.Ordinal))
                                entry.ExtractToFile(destinationPath, true);
                        }
                    }
                }

                File.Delete("conc.zip");
                string text = System.IO.File.ReadAllText(@"conclusion\conclusion.txt");

                if (text.Contains("All checks have passed"))
                {
                    Console.WriteLine("Все тесты пройдены");
                }
                if (text.Contains("A problem in building a solution has occured"))
                {
                    Console.WriteLine("Произошли ошибки в сборке проекта");
                }
                if (text.Contains("A problem in configure/make/build has occured"))
                {
                    Console.WriteLine("Произошли ошибки в сборке проекта");
                }
                if (text.Contains("All/Some tests have failed"))
                {
                    Console.WriteLine("Все тесты или некоторые из них не были пройдены");
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
