# Deploy FTP server

Установка ftp
  ```sh
    sudo apt-get update
    sudo apt-get install vsftpd
  ```
Создание пользователя *"nkrokhmal"*, после чего заполняем предложенную информацию о пользователе, включая пароль
  ```sh
    sudo adduser nkrokhmal
  ```
Создаем директорию для хранения данных и создаем все папки, в которых будет храниться все медиафайлы 
  ```sh
    sudo mkdir /home/nkrokhmal/storage
    sudo mkdir /home/nkrokhmal/storage/audiomobileapp
    sudo mkdir /home/nkrokhmal/storage/audios
    sudo mkdir /home/nkrokhmal/storage/dialogueaudios
    sudo mkdir /home/nkrokhmal/storage/dialoguevideos
    sudo mkdir /home/nkrokhmal/storage/clientavatars
    sudo mkdir /home/nkrokhmal/storage/frames
    sudo mkdir /home/nkrokhmal/storage/imagemobileapp
    sudo mkdir /home/nkrokhmal/storage/media
    sudo mkdir /home/nkrokhmal/storage/mediacontent
    sudo mkdir /home/nkrokhmal/storage/useravatars
    sudo mkdir /home/nkrokhmal/storage/videos
    sudo mkdir /home/nkrokhmal/storage/videosmobileapp    
  ```
Настройка прав для пользователя
 ```sh
   sudo chown -R nkrokhmal /home/nkrokhmal/storage/
 ```
В файле */etc/vsftpd.conf* добавить *write_enable=YES*.   
В файле */etc/ssh/sshd_config* исправить *MaxSessions 1000*.
