# kinda magic )
echo Some kind of magic to turn RabbitMQ on...
mkdir ./rabbit_tmp
cp /var/lib/rabbitmq/mnesia/* ./rabbit_tmp/ 

rm -rf /var/lib/rabbitmq/mnesia/* 

service rabbitmq-server restart
service rabbitmq-server stop
cp ./rabbit_tmp/* /var/lib/rabbitmq/mnesia/ 
service rabbitmq-server start

echo Lets set users and vhosts...

rabbitmqctl add_user admin kloppolk_2018
rabbitmqctl set_user_tags admin administrator
rabbitmqctl add_vhost test
rabbitmqctl set_permissions -p / admin ".*" ".*" ".*"
rabbitmqctl set_permissions -p / guest ".*" ".*" ".*"
rabbitmqctl set_permissions -p test guest ".*" ".*" ".*"


echo Lets start testing...

rabbitmq-plugins enable rabbitmq_management
service rabbitmq-server restart
