# Deploy FTP server

Установка ftp
  ```sh
    sudo apt-get update
    sudo apt-get install vsftpd
  ```
Создание пользователя *"open_user"*, после чего заполняем предложенную информацию о пользователе, включая пароль
  ```sh
    sudo adduser open_user
  ```
Создаем директорию для хранения данных и создаем все папки, в которых будет храниться все медиафайлы 
  ```sh
    sudo mkdir /home/open_user/storage
    sudo mkdir /home/open_user/storage/audiomobileapp
    sudo mkdir /home/open_user/storage/audios
    sudo mkdir /home/open_user/storage/dialogueaudios
    sudo mkdir /home/open_user/storage/dialoguevideos
    sudo mkdir /home/open_user/storage/clientavatars
    sudo mkdir /home/open_user/storage/frames
    sudo mkdir /home/open_user/storage/imagemobileapp
    sudo mkdir /home/open_user/storage/media
    sudo mkdir /home/open_user/storage/mediacontent
    sudo mkdir /home/open_user/storage/useravatars
    sudo mkdir /home/open_user/storage/videos
    sudo mkdir /home/open_user/storage/videosmobileapp    
  ```
Настройка прав для пользователя
 ```sh
   sudo chown -R open_user /home/open_user/storage/
 ```
В файле */etc/vsftpd.conf* добавить *write_enable=YES*.   
В файле */etc/ssh/sshd_config* исправить *MaxSessions 1000*.
