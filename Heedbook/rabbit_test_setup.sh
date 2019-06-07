export DEBIAN_FRONTEND=noninteractive

echo 'deb http://www.rabbitmq.com/debian/ testing main' | sudo tee /etc/apt/sources.list.d/rabbitmq.list
wget -O- https://www.rabbitmq.com/rabbitmq-release-signing-key.asc | sudo apt-key add -

## Update package indices
apt-get update -y

## Install rabbitmq-server and its dependencies
apt-get install rabbitmq-server -y --fix-missing --allow-unauthenticated
apt-get install systemd -y
