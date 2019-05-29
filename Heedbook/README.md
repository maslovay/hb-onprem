## Документация по развертыванию on-prem
###### в будущем будут изменения упрощающие данный процесс
#### 1. Cоздать registry (репозиторий для образов докер-контейнеров) на Azure Portal
Create resource ->
В поиске выбираем container registry ->
Следуем инструкции, интуитивно понятно все.

#### 2. Подготавливаем виртуальные машины для kubernetes.
  - Создаем 2 машины: 1 - мастер, 2 - слейв.
  - Мастер машина необходима для управления кластером. Слейв для размещения на ней подов, сервисов, так что основная нагрузка ляжет на слейв машину, отсюда рассчитываем мощности.
  - ОС которая должна стоять на машинах Ubuntu 18.04 LTS.
  - Также стоит открыть все порты (http, https, ssh)
#### 3. Теперь нам необходимо зайти на мастер и слейв машину и установить там docker и kubernetes.
 ##### Устанавливаем docker
---
  ```sh
  sudo apt-get update &&
  sudo apt-get install \
    apt-transport-https \
    ca-certificates \
    curl \
    gnupg-agent \
    software-properties-common &&
  curl -fsSL https://download.docker.com/linux/ubuntu/gpg | sudo apt-key add - &&
  sudo apt-get update &&
  sudo add-apt-repository \
   "deb [arch=amd64] https://download.docker.com/linux/ubuntu \
   $(lsb_release -cs) \
   stable" &&
  sudo apt-get install docker-ce docker-ce-cli containerd.io
  ```
  Проверяем все ли ок?
  ```sh
  docker --version
  ```
  Включаем докер на машинах
  ```sh
  sudo systemctl enable docker
  ```
##### Устанавливаем kubernetes
---
Добавляем ключ подписи кубернетеса:
```sh
curl -s https://packages.cloud.google.com/apt/doc/apt-key.gpg | sudo apt-key add
```
Добавляем репозиторий с kubernetes: 
```sh
sudo apt-add-repository "deb http://apt.kubernetes.io/ kubernetes-xenial main"
```
Устанавливаем kubeadm:
```sh
sudo apt install kubeadm
```
#### 4. Развертывание kubernetes. 
- Отключаем свап:
```sh
sudo swapoff -a
```
- Именуем каждую ноду
Мастер
```sh
sudo hostnamectl set-hostname master
```
Слейв
```sh
sudo hostnamectl set-hostname slave
```
- Инициализируем kubernetes (выполняем только на мастере)
```sh
sudo kubeadm init --pod-network-cidr=10.244.0.0/16
```
после инициализации в конце вывода (stdout) появится информация по настройке использования команд кубернетеса (kubectl) и присоденинения к кластеру.

Первое что нужно сделать это включить возможность работы с кластером
```sh
mkdir -p $HOME/.kube &&
sudo cp -i /etc/kubernetes/admin.conf $HOME/.kube/config &&
sudo chown $(id -u):$(id -g) $HOME/.kube/config
```
Проверяем
```sh
kubectl get pods
```
Ответ должен быть  __"No resources found."__ 
- Теперь на слейв машине выполняем код для присоединения к кластеру ( он был в выводе )
он выглядит примерно следующим образом:
```sh
sudo kubeadm join <ip> --token ############## \
    --discovery-token-ca-cert-hash sha256:###############################################
```

Если необходимо получить код повторно:
```sh
sudo kubeadm token create --print-join-command
```
Далее проверяем на мастер машине:
```sh
kubectl get nodes
```
Список должен состоять из 2 нод - master-node, slave-node
#### 5. Деплоим приложения в кубернетес 
##### Для начала надо запушить все имаджи в созданный нами registry.
___
 - Логинимся в registry
 ```sh
 docker login <login server> --username <username> --password <пароль>
 ```
###### (всю необходимую информацию можно получить на портале перейдя к ресурсу registry, в раздел access keys/ключи доступа)
- Далее редактируем если необходимо файл build.sh внутри папки Heedbook. 
Меняем везде container registry адрес указанный в теге имаджа
Затем запускаем скрипт билда.
```sh
sh build.sh
```
##### Переходим в папку другого нашего репозитория hb-infrastructure 
___
Поправляем в hb-infrastructure во всех deployment файлах кроме rabbitmq, nginx, default-backend меняем на нужный registry в поле image. 
Далее коммитим
```sh
git add .
git commit -m "<сообщение>"
git push
```
##### На master машине создаем папку в директории ~ source, переходим в нее и клонируем репозиторий
____
```sh
git clone https://github.com/maslovay/hb-infrastructure.git
```
##### Далее нам нужно чтобы наш кубернетес мог пуллить имеджи себе, для этого нужно логинить докер внутри кубернетес.
___
```sh
kubectl create secret docker-registry regcred --docker-server=<your-registry-server> --docker-username=<your-name> --docker-password=<your-pword>
```
##### Далее надо опубликовать tls сертификат
___
```sh
cd ~/source/hb-infrastructure &&
kubectl create secret tls tls-secret --key tls.key --cert tls.crt
```
И добавить сервис аккаунт 
```sh
kubectl apply -f service-account.yaml
```
##### К деплою мы готовы. Теперь выполняем команду
```sh
sh deploy.sh
```
Затем проверяем как развернулись поды. Все ли ок.
```sh
kubectl get pods
```
P.S. для удобства также можно добавить автокомплит для kubectl
```sh
sudo apt-get install bash-completion
type _init_completion
source /usr/share/bash-completion/bash_completion
type _init_completion
echo "source $(kubectl completion bash)" >>~/.bashrc
sudo chmod 777 /etc/bash_completion.d/
kubectl completion bash >/etc/bash_completion.d/kubectl
type _init_completion
sudo reboot
```
##### Также необходимо настроить hbml который необходим для работы faceanalyzeservice.
___
Сначала нужно собрать имедж
Переходим в папку hbml_service
```sh
docker build -t <registry>/hbml:latest .
docker push <registry>/hbml:latest
Заходим на мастер машину 
docker pull <registry>/hbml:latest
docker run -d --name hbml -p 8000:8000 <registry>/hbml:latest
```
Теперь нужно настроить reverse proxy
Из за специфики настройки фильтрации хостов необходимо открыть файл /etc/hosts
добавить строчку вида 
>0.0.0.0 localhost 127.0.0.1
Затем необходимо установить nginx
```sh
sudo apt update
sudo apt install nginx
```
далее настраиваем файерволл
```sh
sudo ufw enable
sudo ufw allow 'Nginx HTTP'
```
затем создаем файл в директории __/etc/nginx/conf.d/__
с расширением .conf (имя не имеет значение, нжинкс автоматом инклудит все файлы с данным расширением)
пишем туда 
```
server {
        listen 80;
        server_name <ip address мастера>;
        location / {
                proxy_pass http://0.0.0.0:8000/;
        }
}
```
Готово. 
____
#### troubleshooting:
у меня в процессе развертывания появилась ошибка, у нод был статус not ready даже после деплоя, и поды были все со статусом pending. 
Ошибки были следующие:
после команды 
```sh
kubectl describe nodes
```
в поле __condition__ master ноды была ошибка вида 
__" runtime network not ready: NetworkReady=false reason:NetworkPluginNotReady message:docker: network plugin is not ready: cni config uninitial"__
исправил ее следующим образом:
```sh
kubectl apply -f https://raw.githubusercontent.com/coreos/flannel/bc79dd1505b0c8681ece4de4c0d86c5cd2643275/Documentation/kube-flannel.yml
```
немного подождал и все исправилось. 
