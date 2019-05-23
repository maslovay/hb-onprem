wget https://packages.erlang-solutions.com/erlang-solutions_1.0_all.deb
dpkg -i erlang-solutions_1.0_all.deb


## Install RabbitMQ signing key
apt-key adv --keyserver "hkps.pool.sks-keyservers.net" --recv-keys "0x6B73A36E6026DFCA"

## Install apt HTTPS transport
apt-get install apt-transport-https

## Add Bintray repositories that provision latest RabbitMQ and Erlang 21.x releases
tee /etc/apt/sources.list.d/bintray.rabbitmq.list <<EOF
deb https://dl.bintray.com/rabbitmq-erlang/debian bionic erlang-21.x
deb https://dl.bintray.com/rabbitmq/debian bionic main
EOF

## Update package indices
apt-get update -y

## Erlang installation
apt-get install esl-erlang -y

## Install rabbitmq-server and its dependencies
apt-get install rabbitmq-server -y --fix-missing




apt-get install systemd -y
service rabbitmq-server start

rabbitmqctl add_user admin kloppolk_2018
rabbitmqctl set_user_tags admin administrator
rabbitmqctl add_vhost test
rabbitmqctl set_permissions -p / admin ".*" ".*" ".*"
rabbitmqctl set_permissions -p / guest ".*" ".*" ".*"
rabbitmqctl set_permissions -p test guest ".*" ".*" ".*"

rabbitmq-plugins enable rabbitmq_management
