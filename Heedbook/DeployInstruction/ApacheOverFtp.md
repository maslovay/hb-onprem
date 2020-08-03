# Установка Apache поверх ftp сервера  

Установка Apache2
  ```sh
    sudo apt-get update
    sudo apt install apache2
  ```
Создаем в папке */etc/apache2/sites-available* файл *ftpserver.conf*
  ```sh
    <VirtualHost *:80>
    # ServerName filereference.northeurope.cloudapp.azure.com
    DocumentRoot /home/open_user/storage
    ErrorLog ${APACHE_LOG_DIR}/error.log
    CustomLog ${APACHE_LOG_DIR}/access.log combined
    <Directory /home/open_user/storage>
      AllowOverride None
      Order allow,deny
      allow from  46.148.203
      Require all granted
    </Directory>
    <Directory /home/open_user/storage/clientavatars>
      AllowOverride None
      Order allow,deny
      <Files ~ "\.(jpg|png)$">
        order deny,allow
        Allow from All
      </Files>
    </Directory>
    <Directory /home/open_user/storage/dialoguevideos>
       AllowOverride None
       Order allow,deny
      <Files ~ "\.(mkv|webm|mp4)$">
        order deny,allow
        Allow from All
      </Files>
    </Directory>
    <Directory /home/open_user/storage/media>
      allow from all
      AllowOverride None
      Order allow,deny
    </Directory>
    </VirtualHost>
  ```
В файле */etc/apache2/apach2.conf* добавляем строки  в самое начало 
```sh
<Directory /home/open_user/storage>
 Options Indexes FollowSymLinks
 AllowOverride None
 Require all granted
</Directory>
```
Выполняем команды
 ```sh
    sudo a2ensite ftpserver.conf (активируем наш сайт)
    sudo a2dissite 000-default.conf (дизейблим старый дефолтный сайт)
    sudo apache2ctl configtest (проверяем конфиг на правильность)
    sudo systemctl reload apache2 (релоадим сервис)
 ```
После этого заходим по адресу виртуалки, на которой все развернуто - там должно быть все.
