# Копирование данных с одного FTP на другой с фильтрацией по дате

Папка storagee на новом удаленном сервере должна иметь права 
sudo chown nkrokhmal -R storage

все папки внутри storage должны иметь права на запись
sudo chmod 777 -R .

Владельцем всех папок clientavatars, dialogueaudios, dialoguevideos, media, mediacontent, useravatars 
должен быть nkrokhmal.

- Установим sshpass на старый ftp если его там нет.

- Заходим на старый ftp сервер в папку /home/nkrokhmal/storage и
выполняем ниже следующую команду, в команде на 3 й строке нужно указать дату, для копирования файлов созданных позднее этой даты:

```
  sudo apt-get install sshpass
```
```
for f in dialoguevideos clientavatars useravatars media mediacontent dialogueaudios
do
  find $f/* -type f -newermt "2020-02-12 00:00:00" >> fileList.txt
done

for f in $(cat ./fileList.txt)
do
  sshpass -p 'kloppolk_2018' sudo scp /home/nkrokhmal/storage/$f nkrokhmal@heedbookftp.northeurope.cloudapp.azure.com:/home/nkrokhmal/storage/$f
done
rm -rf fileList.txt
```
- После выполнения команды нужно проверить появились ли новые файлы на новом ftp сервере
