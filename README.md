# hb-onprem

Ссылка на Yandex Disk, где лежат все схемы, необходимые exe файлы
https://disk.yandex.ru/client/disk/Project%2010%20DialogAnalytics/HeedbookOnPrem

# Файловое хранилище (FTP сервер)
### Подключение.
  - Host - 13.79.133.194
  - User - nkrokhmal
  - Password - kloppolk_2018

Если хочется посмотреть на все файлы "вживую", можно скачать FileZilla, установить, после чего выполнить последовательность операций
1. File ->  Site Manager
2. Protocol - выбрать SFTP, Logon Type - выбрать Normal, скопировать Host, Password и подключиться. 

### Правила наименования файлов и описание структуры файлового хранилища.
1. **videos** - папка, куда попадают видеофайлы длительностью 15 секунд. Правило наименования файлов в этой папке в дальнейшем будет - ApplicationUserId_DateTime.mkv, где
  - ApplicationUserId - Id пользователя
  - DateTime - время начала видео в формате YYYYMMDDhhmmss
2. **frames** - папка, куда попадают кадры, полученные в результате раскадровки видео из папки videos. Правило наименования файлов в этой папке в дальнейшем будет - ApplicationUserId_DateTime.jpg, где
  - ApplicationUserId - Id пользователя
  - DateTime - время начала видео в формате YYYYMMDDhhmmss
3. **dialoguevideos** - папка, где будет храниться уже собранное видео диалога. Правило наименование файлов в этой папке будет - DialogueId.mkv, где
  - DialogueId - id диалога в PostgreSQL.
4. **dialogueaudios** - папка, где будет храниться извлеченное аудио из видео диалога. Правило наименование файлов в этой папке будет - DialogueId.mkv, где
  - DialogueId - id диалога в PostgreSQL.
5. **clientavatars** - папка, где будут храниться аватары всех клиентов. В качестве аватара выбирается кадр, на котором лицо клиента имеет самый большой размер. Правило наименование файлов в этой папке будет - DialogueId.mkv, где
  - DialogueId - id диалога в PostgreSQL.
6. **test** - папка, где можно тестировать абсолютно все, но, чтобы не мешать коллегам, стоит называть все файлы следующим образом - initials_filename, где
  - initials - ваши инициалы
  - filename - ваше название файла


