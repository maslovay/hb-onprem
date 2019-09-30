# Deploy PostgreSQL

Установка postgresql
  ```sh
    sudo apt-get update
    sudo apt-get install postgresql postgresql-contrib 
  ```
Создание пользователя *"test_user"* и базы данный *"test_db"*
  ```sh
    sudo -u postgres createuser test_user
    sudo -u postgres createdb test_db
  ```
Переключение на использование Postgres аккаунта и переход к командной строке Postgres 
  ```sh
    sudo -i -u postgres
    psql
  ```
Создание пароля для пользователя *"test_user"*
 ```sh
    ALTER user test_user with encrypted password 'test_password';
 ```
Выдача полных прав пользователю *"test_user"* для базы данных *"test_db"*
  ```sh
    GRANT all privileges on database test_db to test_user;
    ALTER database test_db owner to test_user;
  ```
Зайти в папку __/etc/postgresql/10/main/__ (если версия postgres отлична от 10, то путь будет другим в соответствии с версией)  
В файле  *postgresql.conf* 
  - заменить *max_connections = 1000*; 
  - раскоментировать  *listen_addresses = 'localhost'* и заменить на __'*'__.  

В файле *pg_hba.conf* вставить строку  
  - *"host  all  all 0.0.0.0/0 md5"*

Также необходимо после открыть на виртуальной машине порт __5432__ 
