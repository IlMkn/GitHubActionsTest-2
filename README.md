# Постановка задачи

 Введем следующую задачу:
 
 Допустим у нас на локальном диске имеется код, который нужно протестировать, и есть удаленный репозиторий, в котором находятся тесты. Нам нужно протестировать имеющийся у нас код, загрузив его в репозиторий с тестами, получить локально результат тестирования кода и работы workflow и отменить любые изменения в репозитории, которые могли произойти во время тестирования.
 
# Описание программы

 Решение задачи будет основано на .NET Core, поддерживается тестирование программ на C# и C++. Полное описание можно найти в отчете, расположенном в репозитории.
 
# Инструкция

### Настройка
 
   - Добавление PAT(при тестировании использовался PAT, дающий полный доступ ко всем функциям GH, но PAT с доступом к репозиторию и workflow достаточно) в качестве секрета в репозиторий (по умолчанию в workflow будет использоваться секрет с названием "TOKEN"). Добавить секрет можно в соответствующей вкладке в настройках репозитория;

   - Для поддержки работы тестов C# необходимо добавить в их .csproj код, приведенный далее, или код из файла ForTestCsproj.txt:
   ```
  <PropertyGroup>
    <GenerateAssemblyInfo>false</GenerateAssemblyInfo>
    <GenerateTargetFrameworkAttribute>false</GenerateTargetFrameworkAttribute>
    <GenerateProgramFile>false</GenerateProgramFile>
  </PropertyGroup>
  ```
   - Внести изменения в workflow:

   CopyFolder:
   ```
   uses: andstor/copycat-action@v3
      with:
        personal_token: ${{ secrets.TOKEN }}
        src_path: /CSTests/. //путь к тестам
        dst_path: /. //путь для копирования
        dst_owner: IlMkn //автор репозитория
        dst_repo_name: GitHubActionsTest-2 //имя репозитоиря
        dst_branch: temp //ветвь, в которую будет произведено копирование
        src_branch: main //ветвь, из которой будет происходить копирование
   ```
   DeleteBranches:
   ```
    uses: dawidd6/action-delete-branch@v3
      with:
        github_token: ${{github.token}}
        branches: temp //название ветви, которая будет удалена
   ```
   BuildAndTest: 
   ```
   - name: Add csproj
        continue-on-error: true
        id: step2
        if: steps.check_files1.outputs.files_exists == 'true'
        run: |
          dotnet sln ./myNewFolder/t.sln add **/TestConsoleApp.csproj //указать .csproj проекта, который нужно протестировать
          dotnet add XUnitTestProject1.csproj reference **/TestConsoleApp.csproj //указать .csproj проекта с тестами
          dotnet sln ./myNewFolder/t.sln add XUnitTestProject1.csproj //указать .csproj проекта с тестами
          nuget restore ./myNewFolder/t.sln
                  
      - name: Build
        continue-on-error: true
        id: step3
        if: steps.check_files1.outputs.files_exists == 'true'
        run: |
          dotnet build ./myNewFolder/t.sln
      - name: Test
        continue-on-error: true
        id: step4
        if: steps.check_files1.outputs.files_exists == 'true'
        run: |
          dotnet test XUnitTestProject1.csproj --logger "trx;LogFileName=test-results.trx" //указать .csproj проекта с тестами   
   ```
   ```
   - name: AppendToCMake 
        if: steps.check_files2.outputs.files_exists == 'true' 
        uses: DamianReeves/write-file-action@v1.0
        with:
          path: CMakeLists.txt
          contents: |
            add_subdirectory(CPPTests) //указать имя папки с тестами
          write-mode: append      
   ```
   ```
   - name: Run Test
        continue-on-error: true
        id: cpp_s4
        if: steps.check_files2.outputs.files_exists == 'true'
        run: /home/runner/work/GitHubActionsTest-2/GitHubActionsTest-2/build/*СPPTests*/GitHubActionsTest-2.test //указать имя папки с тестами в адресе
   ```
   - При создании локальной копии папки с примером кода для тестирования нужно поменять в ней название папки "_github" на ".github". <br />
    
 ### Использование программы
 
   - Заполнить все необходимые для работы программы данные в файле конфигурации(jsonConfig.json):
    
   ```
   "PersonalAccessToken": "PAT", //Personal Access Token с доступом к репозиторию и workflow(параметры доступа задаются при создании PAT)
   "User": "IlMkn", //имя пользователя, в репозитории которого будет происходить тестирование
   "Repo": "IlMkn/GitHubActionsTest-2", //идентификатор репозитория, в котором будет происходить тестирование
   "SourceDir": "sourceDirCS", //название папки с кодом для тестирования(должна быть расположена в общей папке проекта вместе с его файлом .sln)
   "targetDir": "testDirectory2/targetDir", //название папки, в которую юудет происходить копирование кода для тестирования(можно оставить значение по умолчанию)
   "deleteDir": "testDirectory2" //удаляет папку с копией кода для тестирования после пуша содержимого в удаленый репозиторий
   ```
   - После успешного пуша и создания ветки "temp" программа ожидает завершения workflow и загрузки артефактов в репозиторий(в текущей реализации просто используется Thread.Sleep()). В некоторых ситуациях, когда сервера GitHub могут быть сильно нагружены, времени установленного для простоя по умолчанию может не хватить. В этом случае стоит изменить параметр в Thread.Sleep();<br />
   - После завершения работы программы в консоль будет осуществлен базовый вывод информации о ходе тестирования. Ту же информацию можно найти в локальной папке дебага программы (conc.txt\conclusion.txt). <br />

## Использованные API
Checkout - https://github.com/actions/checkout;<br />
File Existence - https://github.com/andstor/file-existence-action;<br />
Copy File - https://github.com/andstor/copycat-action;<br />
Delete Branch - https://github.com/dawidd6/action-delete-branch;<br />
Setup DotNet - https://github.com/actions/setup-dotnet;<br />
Setup NuGet - https://github.com/NuGet/setup-nuget;<br />
Upload Artifact - https://github.com/actions/upload-artifact;<br />
Write File - https://github.com/DamianReeves/write-file-action;<br />
Git functions - https://github.com/libgit2/libgit2sharp;<br />
JSON library - https://github.com/JamesNK/Newtonsoft.Json.
