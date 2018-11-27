# Это тестовое приложение для демонстрации работы .Net Core 2.0 приложения в контейнере docker с использованием rabbitmq и nfs. 
## Код
**В классе HBLib.Utils.HeedbookMessengerStatic.MQMessenger есть документация в формате XML комментариев.**

### Функция main()

*Строки 22-31:*

Сначала необходимо инициализировать статический класс для установления связи с сервером rabbitmq.

*Строки 32-35:*

Затем необходимо подписаться на нужный топик. Для подписки необходимо передать функцию, принимающую на вход строку с сообщением, и возвращающую void. Эта функия выполняет работу, которую необходимо выполнить при получении сообщения.

*Строки 36-41:*

Здесь запускается бесконечный цикл для того, чтобы приложение оставалось работать и ждать команд, получаемых через rabbitmq.

### Функция Worker()

Всё, что делает функция - получает команду на создание или удаление текстового файла через rabbitmq. Функция логирует в консоль выполненные действия и ошибки.

## Сборка и запуск приложения .Net Core под Linux.

**Полезные ссылки:**
* *Установка .Net Core в Linux* https://docs.microsoft.com/ru-ru/dotnet/core/linux-prerequisites?tabs=netcore2x
* *Сборка приложения платформы .Net Core 2.0 в Linux через командную строку* https://docs.microsoft.com/ru-ru/dotnet/core/tutorials/using-with-xplat-cli

Здесь ничего сложного. Нужно зайти в папку с основным приложением (Local) и выполнить следующие команды:

`dotnet add reference ../hblibonprem/hb-onprem/HBLibOnprem/HBLibOnprem/HBLibOnprem.csproj` - добавить ссылку на связанный проект
`dotnet add package Newtonsoft.Json` - добавить nuget-пакет Newtonsoft.Json в проект
`dotnet build -c Release` - собрать проект под конфигурацией Release

Для запуска необходимо ввести команду: 
`dotnet run --no-build -c Release` - запустить собранный проект без пересборки в конфигурации Release.

## Сборка под docker-контейнер

Для создания образа необходим конфигурационный файл. По умолчанию docker ищет файл "Dockerfile". Он представляет собой текстовый файл с инструкциями.

`docker build -t hbcontainerimagesstorage.azurecr.io/hbonpremhelloworld .` - команда, чтобы собрать docker-образ с тегом "hbcontainerimagesstorage.azurecr.io/hbonpremhelloworld", используя Dockerfile в данном каталоге

### Содержимое Dockerfile

* `FROM microsoft/dotnet:2.1-sdk` - использовать в качестве исходного образа образ "microsoft/dotnet" с тегом "2.1-sdk"
* `RUN mkdir -p /service/lib /service/app /filedump` - создать в образе каталоги /service/lib, /service/app, /filedump
* `ADD hblibonprem/hb-onprem/HBLibOnprem/HBLibOnprem /service/lib` - скопировать в образ содержимое hblibonprem/hb-onprem/HBLibOnprem/HBLibOnprem в каталог /service/lib
* `ADD Local /service/app` - скопировать в образ содержимое Local в каталог /service/app
* `RUN dotnet restore -f /service/lib/HBLibOnprem.csproj` - запустить в образе команду для восстановления пакетов и ссылок в библиотеке HBLibOnprem
* `RUN dotnet add /service/app/Local.csproj reference /service/lib/HBLibOnprem.csproj` - добавить в основной проект ссылку на библиотеку HBLibOnprem
* `RUN dotnet restore -f /service/app/Local.csproj` - восстановить пакеты и ссылки в основном проекте
* `RUN dotnet build --force -f netcoreapp2 -c Release /service/app/Local.csproj` - пересобрать основной проект под фреймворк .NEt Core 2.0 с пересборкой связанных проектов под конфигурацию Release с указанием пути к основному проекту
* `WORKDIR /filedump` - установить в образе рабочей директорией "/filedump"
* `CMD ["dotnet", "run", "--no-build", "-c", "Release", "-p", "/service/app/Local.csproj"]` - команда, выполняемая при запуске контейнера. В данном случае запускается основной проект без пересборки под конфигурацией Release с указанием пути проекта.

### Строка для запуска контейнера

**Данная строка использовалась для запуска контейнера в виртуальной машине "cr-tests". Некоторые параметры будут отличаться при запуске на другой машине.**

`docker run --rm --name hbonpremtest1 -d -v /home/test-machine/services_info/hbonpremhelloworld/filedump/:/filedump/ hbcontainerimagesstorage.azurecr.io/hbonpremhelloworld:1.0` - ниже описание, что происходит

* `docker run` - создать и запустить docker-контейнер
* `--rm` - удалить контейнер по завершении его работы
* `--name hbonpremtest1` - задать контейнеру имя "hbonpremtest1"
* `-d` - объявить запущенный контейнер "демоном"
* `-v /home/test-machine/services_info/hbonpremhelloworld/filedump/:/filedump/` - смонтировать каталог и всё содержимое в "/home/test-machine/services_info/hbonpremhelloworld/filedump/" в каталог контейнера "/filedump/"
* `hbcontainerimagesstorage.azurecr.io/hbonpremhelloworld:1.0` - имя запускаемого образа с тегом "1.0"

## Где тут используется NFS?

NFS предлагается использовать следующим образом: NFS монтируется на локальной машине в тот каталог, который позже будет смонтирован в контейнер для сброса файлов. Альтернативный вариант - написать небольшой bash-скрипт, который монтирует NFS непосредственно в контейнер, а затем запускает основное приложение. Этот скрипт, очевидно, необходимо будет скопировать в образ на стадии сборки образа и изменить стартовую команду со старта основного приложения на старт этого скрипта.
