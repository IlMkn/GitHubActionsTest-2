# Постановка задачи<br />
 Введем следующую задачу: <br />
 Допустим у нас на локальном диске имеется код, который нужно протестировать, и есть удаленный репозиторий, в котором находятся тесты. Нам нужно протестировать имеющийся у нас код, загрузив его в репозиторий с тестами, получить локально результат тестирования кода и работы workflow и отменить любые изменения в репозитории, которые могли произойти во время тестирования.<br />
# Описание программы <br />
 Решение задачи будет основано на .NET Core, поддерживается тестирование программ на C# и C++. Полное описание можно найти в отчете, расположенном в репозитории. 
# Инструкция<br />
 1) Настройка:<br />
  1.1) Добавление PAT(при тестировании использовался PAT, дающий полный доступ ко всем функциям GH, но PAT с доступом к репозиторию и workflow достаточно) в качестве секрета в репозиторий (по умолчанию в workflow будет использоваться секрет с названием "TOKEN"). Добавить секрет можно в соответсвтующей вкладке в настройках репозитория;<br />
   1.2) При использовании собственных тестов (при тестировании кода на C#), добавить в файл проекта этих тестов код из текстового файла ForTestCsproj.txt, содержащегося в репозитории. Необходимо для корректной сборки/тестирования решения;<br />
   1.3) Внести изменения в workflow:<br />
   CopyFolder: значение dst_owner - имя владельца репозитория, dst_repo_name - название репозитоиря, dst_branch - название временной ветки, используемый для тестирования, src_branch - основная ветка, из которой берутся тесты и в которой находятся файлы workflow, src_path - название директории с тестами, dst_path - путь для копирования содержимого директория (в случае тестирования C++ копирование происходит в отдельную директорию, в случае с C# - в корневую папку репозитория)<br />
   DeleteBranches: значение branches, расположенное в шаге Delete branch, должно иметь название временной ветки, в которой происходит тестирование.<br />
   BuildAndTest: (Тестирование C#)строки 58-60 указать имена файлов .csproj тестируемого проекта и проекта тестов(также строка 74),(C++) строки 125 и 155 - указать название директории с тестами.<br />
   1.4) При создании локальной копии папки с примером кода для тестирования нужно поменять в ней название папки "_github" на ".github" <br />
 2) Использование программы:<br />
   2.1) Заполнить все необходимые для работы программы данные в файле конфигурации:<br />
   PersonalAccessToken - PAT с доступом к репозиторию и workflow (параметры доступа PAT определяются во время его создания);<br />
   User и Repo означают имя пользователя GitHub и название репозитория GitHub соответственно.<br />
   2.2) Поместить в директорию, содержащую файл решения, директорию кода для тестирования , для работы тестирования C++ подразумевается использование CMake;<br />
   2.3) После успешного пуша и создания ветки "temp" программа ожидает завершения workflow и загрузки артефактов в репозиторий(в текущей реализации просто используется Thread.Sleep()). В некоторых ситуациях, когда сервера GitHub могут быть сильно нагружены, времени установленного для простоя по умолчанию может не хватить. В этом случае стоит изменить параметр в Thread.Sleep();<br />
   2.4) После завершения работы программы в консоль будет осуществлен базовый вывод информации о ходе тестирования. Ту же информацию можно найти в локальной папке дебага программы. <br />
