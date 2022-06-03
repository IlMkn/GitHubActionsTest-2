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

            WebClient wc = new WebClient();
            wc.Headers.Add("user-agent",
                "Mozilla / 5.0(Windows NT 6.1) AppleWebKit / 537.36(KHTML, like Gecko) Chrome / 41.0.2228.0 Safari / 537.36");
            try
            {
                wc.DownloadFile("https://raw.githubusercontent.com/IlMkn/GitHubActionsTest-2/master/.github/workflows/BuildAndTestCS.yml", "BuildAndTestCS.txt");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            List<string> namesList = new List<string>();
            string text0 = "";
            int count = 0;
            int tempIndex = 0;
            foreach (string line in File.ReadLines("BuildAndTestCS.txt"))
            {
                text0 += line + '\n';
                if (line.Contains(" - name: "))
                {
                    tempIndex = line.IndexOf('-');
                    string stepName = line.Substring(line.IndexOf(':') + 2);
                    namesList.Add(stepName.Replace(' ', '_'));
                    count++;
                    for (int i = 0; i < line.IndexOf('n'); i++)
                    {
                        text0 += " ";
                    }
                    text0 += "continue-on-error: true" + '\n';
                    for (int i = 0; i < line.IndexOf('n'); i++)
                    {
                        text0 += " ";
                    }
                    text0 += "id: step_" + stepName.Replace(' ', '_') + '\n';
                    count++;
                }
            }

            foreach (var element in namesList)
            {

                for (int i = 0; i < tempIndex; i++)
                {
                    text0 += " ";
                }
                text0 += "- name: Check step(true) - " + element + '\n';

                for (int i = 0; i < tempIndex + 2; i++)
                {
                    text0 += " ";
                }
                text0 += "if: steps.step_" + element + ".outcome == 'success'" + '\n';

                for (int i = 0; i < tempIndex + 2; i++)
                {
                    text0 += " ";
                }
                text0 += "shell: bash" + '\n';

                for (int i = 0; i < tempIndex + 2; i++)
                {
                    text0 += " ";
                }
                text0 += "run: |" + '\n';

                for (int i = 0; i < tempIndex + 4; i++)
                {
                    text0 += " ";
                }
                text0 += "expr 'Step " + element + " is succesful' > " + element + "-art.txt" + '\n';


                for (int i = 0; i < tempIndex; i++)
                {
                    text0 += " ";
                }
                text0 += "- name: Check step(false) - " + element + '\n';

                for (int i = 0; i < tempIndex + 2; i++)
                {
                    text0 += " ";
                }
                text0 += "if: steps.step_" + element + ".outcome != 'success'" + '\n';

                for (int i = 0; i < tempIndex + 2; i++)
                {
                    text0 += " ";
                }
                text0 += "shell: bash" + '\n';

                for (int i = 0; i < tempIndex + 2; i++)
                {
                    text0 += " ";
                }
                text0 += "run: |" + '\n';

                for (int i = 0; i < tempIndex + 4; i++)
                {
                    text0 += " ";
                }
                text0 += "expr 'Step " + element + " has failed' > " + element + "-art.txt" + '\n';
                text0 += '\n';
            }


            foreach (var element in namesList)
            {

                for (int i = 0; i < tempIndex; i++)
                {
                    text0 += " ";
                }
                text0 += "- name: Add artifact for step - " + element + '\n';

                for (int i = 0; i < tempIndex + 2; i++)
                {
                    text0 += " ";
                }
                text0 += "uses: actions/upload-artifact@v2" + '\n';

                for (int i = 0; i < tempIndex + 2; i++)
                {
                    text0 += " ";
                }
                text0 += "with:" + '\n';

                for (int i = 0; i < tempIndex + 4; i++)
                {
                    text0 += " ";
                }
                text0 += "name: " + element + "-art" + '\n';

                for (int i = 0; i < tempIndex + 4; i++)
                {
                    text0 += " ";
                }
                text0 += "path: ./**/" + element + "-art.txt" + '\n';
                text0 += '\n';
            }

            for (int i = 0; i < tempIndex; i++)
            {
                text0 += " ";
            }
            text0 += "- name: Invoke workflow without inputs"+'\n';

            for (int i = 0; i < tempIndex + 2; i++)
            {
                text0 += " ";
            }
            text0 += "uses: benc-uk/workflow-dispatch@v1" + '\n';

            for (int i = 0; i < tempIndex + 2; i++)
            {
                text0 += " ";
            }
            text0 += "with:" + '\n';

            for (int i = 0; i < tempIndex + 4; i++)
            {
                text0 += " ";
            }
            text0 += "workflow: Delete" + '\n';

            for (int i = 0; i < tempIndex + 4; i++)
            {
                text0 += " ";
            }
            text0 += "token: ${{ secrets.TOKEN }}" + '\n';

            File.WriteAllText("BuildAndTestCS.txt", String.Empty);
            File.WriteAllText("BuildAndTestCS.txt", text0);


            File.Move("BuildAndTestCS.txt", Path.ChangeExtension("BuildAndTestCS.txt", ".yml"),true);
            File.Move("BuildAndTestCS.yml", sourceDirPath.ToString() + @"\.github\workflows\BuildAndTestCS.yml", true);

            
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

            Console.WriteLine("Ожидание завершения WorkFlow(150с)");
            System.Threading.Thread.Sleep(150000);

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
            query = "https://api.github.com/repos/" + config.Repo + "/actions/artifacts";

            //получение артефакта workflow, который говорит о статусе его выполнения
            bool CopyFolderStatus = false;
            Console.WriteLine();
            try
            {
                HttpResponseMessage response = await clienthttp.GetAsync(query);
                response.EnsureSuccessStatusCode();
                string responseBody = await response.Content.ReadAsStringAsync();
                var RCR = JsonConvert.DeserializeObject<Root>(responseBody);
                int i = 0;
                while ((RCR.artifacts[i].name != "concCopy")&& (i < RCR.artifacts.Count))
                {
                    i++;
                }
                concurl = RCR.artifacts[i].archive_download_url;

                try
                {
                    var response2 = await clienthttp.GetAsync(@concurl + "?filename=concCopy.zip");
                    using (var stream = await response2.Content.ReadAsStreamAsync())
                    {
                        var fileInfo = new FileInfo("concCopy.zip");
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

                using (ZipArchive archive = ZipFile.OpenRead("concCopy.zip"))
                {
                    foreach (ZipArchiveEntry entry in archive.Entries)
                    {
                        if (entry.FullName == "conclusionCopyFolder.txt")
                        {
                            string destinationPath = Path.GetFullPath(Path.Combine(extractPath, entry.FullName));

                            if (destinationPath.StartsWith(extractPath, StringComparison.Ordinal))
                                entry.ExtractToFile(destinationPath, true);
                        }
                    }
                }

                File.Delete("concCopy.zip");
                string text = System.IO.File.ReadAllText(@"conclusion\conclusionCopyFolder.txt");

                if (text.Contains("Copy workflow completed succesfully"))
                {
                    CopyFolderStatus = true;
                    Console.WriteLine("CopyFolder запустился и успешно завершил работу");
                }
                if (text.Contains("Copy workflow completed with an error"))
                {
                    Console.WriteLine("CopyFolder не запустился или завершил работу с ошибкой");
                }

                if (CopyFolderStatus)
                {

                    Console.WriteLine("Работа BuildAndTestCS:");
                    int successCount = 0;
                    int stepCount = 0;
                    foreach (var element in namesList)
                    {
                        Console.WriteLine();
                        response = await clienthttp.GetAsync(query);
                        response.EnsureSuccessStatusCode();
                        responseBody = await response.Content.ReadAsStringAsync();
                        RCR = JsonConvert.DeserializeObject<Root>(responseBody);
                        i = 0;

                        string tempStr = element + "-art";
                        while ((RCR.artifacts[i].name != tempStr) && (i < RCR.artifacts.Count))
                        {
                            i++;
                        }
                        concurl = RCR.artifacts[i].archive_download_url;


                        try
                        {
                            var response2 = await clienthttp.GetAsync(@concurl + "?filename="+element+"-art.zip");
                            using (var stream = await response2.Content.ReadAsStreamAsync())
                            {
                                var fileInfo = new FileInfo(element + "-art.zip");
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

                        extractPath = Path.GetFullPath("conclusion");

                        if (!extractPath.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal))
                            extractPath += Path.DirectorySeparatorChar;

                        using (ZipArchive archive = ZipFile.OpenRead(element + "-art.zip"))
                        {
                            foreach (ZipArchiveEntry entry in archive.Entries)
                            {
                                if (entry.FullName == element + "-art.txt")
                                {
                                    string destinationPath = Path.GetFullPath(Path.Combine(extractPath, entry.FullName));

                                    if (destinationPath.StartsWith(extractPath, StringComparison.Ordinal))
                                        entry.ExtractToFile(destinationPath, true);
                                }
                            }
                        }

                        File.Delete(element + "-art.zip");
                        text = System.IO.File.ReadAllText(@"conclusion\"+ element + "-art.txt");

                        if (text.Contains("is succesful"))
                        {
                            Console.WriteLine("  {0}  Шаг {1} успешно завершил работу", stepCount + 1, element);
                            successCount++;
                        }
                        if (text.Contains("has failed"))
                        {
                            Console.WriteLine("  {0}  Шаг {1} завершил работу с ошибкой", stepCount + 1, element);
                        }
                        stepCount++;
                    }
                    Console.WriteLine();
                    Console.WriteLine("Процент выполнения workflow - {0}%", successCount/(double)namesList.Count*100);

                    Console.WriteLine();
                    response = await clienthttp.GetAsync(query);
                    response.EnsureSuccessStatusCode();
                    responseBody = await response.Content.ReadAsStringAsync();
                    RCR = JsonConvert.DeserializeObject<Root>(responseBody);
                    i = 0;
                    while ((RCR.artifacts[i].name != "concDeleteBranch") && (i < RCR.artifacts.Count-1))
                    {
                        i++;
                    }
                    concurl = RCR.artifacts[i].archive_download_url;

                    if (RCR.artifacts[i].name == "concDeleteBranch")
                    {
                        try
                        {
                            var response2 = await clienthttp.GetAsync(@concurl + "?filename=concDeleteBranch.zip");
                            using (var stream = await response2.Content.ReadAsStreamAsync())
                            {
                                var fileInfo = new FileInfo("concDeleteBranch.zip");
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

                        extractPath = Path.GetFullPath("conclusion");

                        if (!extractPath.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal))
                            extractPath += Path.DirectorySeparatorChar;

                        using (ZipArchive archive = ZipFile.OpenRead("concDeleteBranch.zip"))
                        {
                            foreach (ZipArchiveEntry entry in archive.Entries)
                            {
                                if (entry.FullName == "conclusionDeleteBranch.txt")
                                {
                                    string destinationPath = Path.GetFullPath(Path.Combine(extractPath, entry.FullName));

                                    if (destinationPath.StartsWith(extractPath, StringComparison.Ordinal))
                                        entry.ExtractToFile(destinationPath, true);
                                }
                            }
                        }


                        File.Delete("concDeleteBranch.zip");
                        text = System.IO.File.ReadAllText(@"conclusion\conclusionDeleteBranch.txt");

                        if (text.Contains("DeleteBranch workflow completed succesfully"))
                        {
                            Console.WriteLine("DeleteBranch успешно завершил работу");
                        }
                        if (text.Contains("DeleteBranch workflow completed with an error"))
                        {
                            Console.WriteLine("DeleteBranch завершил работу с ошибкой");
                        }

                    }
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
