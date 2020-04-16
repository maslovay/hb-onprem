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

rabbitmqctl list_vhosts
rabbitmqctl list_users
service --status-all

mkdir /app/HBOperations/DialogueCreatorSchedulerTests/TestResults/
cd /app/HBOperations/DialogueCreatorSchedulerTests/
dotnet test --logger:"trx;LogFileName=results.trx" ; base64 /app/HBOperations/DialogueCreatorSchedulerTests/TestResults/results*.trx > /app/HBOperations/DialogueCreatorSchedulerTests/TestResults/results_base64 ;
curl -X POST "https://heedbookapi.northeurope.cloudapp.azure.com/user/ExpressTester/PublishUnitTestResults" -H  "accept: application/json" -H  "Content-Type: application/json-patch+json" -d "{ \"TrxTextBase64\" : \"$(cat /app/HBOperations/DialogueCreatorSchedulerTests/TestResults/results_base64)\" }";
echo test ended
