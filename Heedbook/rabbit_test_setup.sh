#wget -O- https://packages.erlang-solutions.com/ubuntu/erlang_solutions.asc | apt-key add -
#echo "deb https://packages.erlang-solutions.com/ubuntu bionic contrib" | tee /etc/apt/sources.list.d/rabbitmq.list <<EOF
#deb https://dl.bintray.com/rabbitmq-erlang/debian bionic erlang-21.x
#deb https://dl.bintray.com/rabbitmq/debian bionic main
#EOF

## Update package indices
apt-get update -y

## Erlang installation
apt-get install esl-erlang -y

## Install rabbitmq-server and its dependencies
apt-get install rabbitmq-server -y --fix-missing --allow-unauthenticated
apt-get install systemd -y

