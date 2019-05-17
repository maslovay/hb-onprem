# Документация по добавлению SSL сертификата

### 1. Установка certbot  
На slave до того, как что либо будет поставлено выполнить
  
  ```
  sudo apt-get update
  sudo apt-get install software-properties-common
  sudo add-apt-repository universe
  sudo add-apt-repository ppa:certbot/certbot
  sudo apt-get update
  sudo apt-get install certbot python-certbot-nginx 
  sudo certbot --nginx
  ```
В результате будет создано 2 файла с ключами сертификата и самим сертификатом. Их необходимо перекопировать в master 
в tls.crt и в tls.key

### 2. Установка helm и cert-manager
В master выполнить

  ```
  sudo snap install helm --classic
  helm init
  ```
При инициализации возможна ошибка, в этом случае надо поставить tiller
  ```
  kubectl create serviceaccount --namespace kube-system tiller
  kubectl create clusterrolebinding tiller-cluster-rule --clusterrole=cluster-admin --serviceaccount=kube-system:tiller
  kubectl patch deploy --namespace kube-system tiller-deploy -p '{"spec":{"template":{"spec":{"serviceAccount":"tiller"}}}}'
  ```
### 3. Установка cert manager
  ```
  kubectl apply -f https://raw.githubusercontent.com/jetstack/cert-manager/release-0.7/deploy/manifests/00-crds.yaml  
  kubectl create namespace cert-manager  
  kubectl label namespace cert-manager certmanager.k8s.io/disable-validation=true  
  helm repo add jetstack https://charts.jetstack.io 
  helm install --name cert-manager --namespace default --version v0.7.0 jetstack/cert-manager
  ```
  
### 4. Создание сертификата и cluster issuer 
Конфиги лежат в директории /source/hb-infrastructure/ClusterIssuer
  ```
  kubectl apply -f clusterissuer.yml
  kubectl apply -f certificates.yaml
  ```

