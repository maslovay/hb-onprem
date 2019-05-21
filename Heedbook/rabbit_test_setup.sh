apt-get install erlang erlang-nox -y
apt-get install rabbitmq-server -y
apt-get install systemd -y
#systemctl enable rabbitmq-server
service rabbitmq-server start
cp ./HBOperations/AudioAnalyzeService/OpenVoka /app/

rabbitmqctl add_user admin kloppolk_2018
rabbitmqctl set_user_tags admin administrator
rabbitmqctl add_vhost test
rabbitmqctl set_permissions -p / admin ".*" ".*" ".*"
rabbitmqctl set_permissions -p / guest ".*" ".*" ".*"
rabbitmqctl set_permissions -p test guest ".*" ".*" ".*"

rabbitmq-plugins enable rabbitmq_management
