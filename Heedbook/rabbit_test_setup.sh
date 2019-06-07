export DEBIAN_FRONTEND=noninteractive
#wget https://packages.erlang-solutions.com/erlang-solutions_1.0_all.deb
#dpkg -i erlang-solutions_1.0_all.deb


## Install RabbitMQ signing key
apt-key adv --keyserver "hkps.pool.sks-keyservers.net" --recv-keys "0x6B73A36E6026DFCA"

## Install apt HTTPS transport
apt-get install apt-transport-https

## Add repositories that provision latest RabbitMQ and Erlang 21.x releases
#wget https://packages.erlang-solutions.com/erlang-solutions_1.0_all.deb
#dpkg -i erlang-solutions_1.0_all.deb
#tee /etc/apt/sources.list.d/bintray.rabbitmq.list <<EOF


wget -O- https://packages.erlang-solutions.com/ubuntu/erlang_solutions.asc | apt-key add -
echo "deb https://packages.erlang-solutions.com/ubuntu bionic contrib" | tee /etc/apt/sources.list.d/rabbitmq.list <<EOF
deb https://dl.bintray.com/rabbitmq-erlang/debian bionic erlang-21.x
deb https://dl.bintray.com/rabbitmq/debian bionic main
EOF

## Update package indices
apt-get update -y

## Erlang installation
apt-get install esl-erlang -y

## Install rabbitmq-server and its dependencies
apt-get install rabbitmq-server -y --fix-missing --allow-unauthenticated
apt-get install systemd -y

