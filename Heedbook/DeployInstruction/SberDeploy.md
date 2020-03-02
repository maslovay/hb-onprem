## Документация по развертыванию on-prem в кластере Сбербанка.
###### 1. Конфигурация кластера.
Необходимо иметь 4 виртуальных машины.
- master 
- front
- slave1
- slave2

Необходимо перенести следующие на виртаульные машины следующие либы, файлы и докер контейнеры
  - master
    - Packages: docker, kubernetes (kubectl, kubeadm, kubelet, версия 1.14.2-00)
    - Files: папку hb-infrastructure, hb-live
  - slave1
    - Packages: docker, kubernetes (kubectl, kubeadm, kubelet, версия 1.14.2-00) 
    - Containers: образы всех микросервисов, за исключением speech to text, face analyze, образы hpa, flannel
  - slave2
    - Packages: docker, kubernetes (kubectl, kubeadm, kubelet, версия 1.14.2-00) 
    - Containers: образы всех микросервисов speech to text, face analyze
  - front
    - Packages: docker, postgresql
    - Docker: образы фронта и веб-сокета
    
#### 2. Деплой кластера.
###### 2.1 Деплой мастера.
Перейти в папку, в которой находятся пакет docker. Выполнить команду
```sudo yum install *.rpm
```
Перейти в папку, в которой находится пакет kubernetes. Выполнить команду
```sudo yum install *.rpm
```
После этого на виртуальной машине будет установлен docker и kubernetes
Проверяем, что docker поставился и включаем его на машине
``` sudo docker --version
sudo systemctl enable docker
```
Отключаем свап, именуем машину master, инициализируем ее, даем возможность работы с кластером и
получаем команду для присоединения к кластеру
```sudo swapoff -a
sudo hostnamectl set-hostname master
sudo kubeadm init --pod-network-cidr=10.244.0.0/16
mkdir -p $HOME/.kube &&
sudo cp -i /etc/kubernetes/admin.conf $HOME/.kube/config &&
sudo chown $(id -u):$(id -g) $HOME/.kube/config
sudo kubeadm token create --print-join-command
```
В результате этих операций будет выведена команда по типу
```
sudo kubeadm join <ip> --token ############## \
    --discovery-token-ca-cert-hash sha256:###############################################
```
Это и будет команда подключения к мастеру.
###### 2.2 Деплой slave1.
Перейти в папку, в которой находятся пакет docker. Выполнить команду
```sudo yum install *.rpm
```
Перейти в папку, в которой находится пакет kubernetes. Выполнить команду
```sudo yum install *.rpm
```
После этого на виртуальной машине будет установлен docker и kubernetes
Проверяем, что docker поставился и включаем его на машине
``` sudo docker --version
sudo systemctl enable docker
```
Дать название виртуальной машине
```
sudo hostnamectl set-hostname slave
```

Выполнить команду присоединения к кластеру
```
sudo kubeadm join <ip> --token ############## \
    --discovery-token-ca-cert-hash sha256:###############################################
```
Подгрузить все докер контейнеры микросервисов и необходимых сервисов. Для этого необходимо зайти в папку с образами докера 
и выполнить
```sudo docker load -i *.tar
```
###### 2.3 Деплой slave2.
Перейти в папку, в которой находятся пакет docker. Выполнить команду
```sudo yum install *.rpm
```
Перейти в папку, в которой находится пакет kubernetes. Выполнить команду
```sudo yum install *.rpm
```
После этого на виртуальной машине будет установлен docker и kubernetes
Проверяем, что docker поставился и включаем его на машине
``` sudo docker --version
sudo systemctl enable docker
```
Дать название виртуальной машине
```
sudo hostnamectl set-hostname slave
```
Выполнить команду присоединения к кластеру
```
sudo kubeadm join <ip> --token ############## \
    --discovery-token-ca-cert-hash sha256:###############################################
```
Подгрузить все докер контейнеры микросервисов и необходимых сервисов. Для этого необходимо зайти в папку с образами докера 
и выполнить
```sudo docker load -i *.tar
````

###### 2.4 Деплой фронта.
Перейти в папку, в которой находятся пакет docker. Выполнить команду
```sudo yum install *.rpm
```
Проверяем, что docker поставился и включаем его на машине
``` sudo docker --version
sudo systemctl enable docker
```
Подгрузить все докер контейнеры микросервисов и необходимых сервисов. Для этого необходимо зайти в папку с образами докера 
и выполнить
```sudo docker load -i *.tar
````
Запустить образы фронта и вебсокета
```
sudo docker run -d -p 8000:8000 front
sudo docker run -d -p 10000:10000 websocket
```

#### Настройка мастера, развертывание ftp и postgresql
Зайти на виртуальной машине master в папку hb-infrastructure и выполнить
```sh deploy.sh
```
Установка ctontab
``` sudo yum install crontab
```
Настройка жизненного цикла для всех микросервисов (user_name - имя пользователя, microservice_name - имя сервиса, 
надо добавить для всех)
```
sudo crontab -e
* */2 * * * /home/user_name/Downloads/microservice_name.sh
```
