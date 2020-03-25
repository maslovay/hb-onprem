# kinda magic )
echo Some kind of magic to turn RabbitMQ on...
mkdir /app/HBOperations/DialogueStatusCheckerSchedulerTests/rabbit_tmp/
cp /var/lib/rabbitmq/mnesia/* /app/HBOperations/DialogueStatusCheckerSchedulerTests/rabbit_tmp/ -R

rm -rf /var/lib/rabbitmq/mnesia/* 

service rabbitmq-server restart
service rabbitmq-server stop
cp /app/HBOperations/DialogueStatusCheckerSchedulerTests/rabbit_tmp/* /var/lib/rabbitmq/mnesia/ -R
service rabbitmq-server start

#echo Lets set users and vhosts...

rabbitmq-plugins enable rabbitmq_management 

rabbitmqctl add_user admin kloppolk_2018
rabbitmqctl set_user_tags admin administrator
rabbitmqctl add_vhost test
rabbitmqctl set_permissions -p / admin ".*" ".*" ".*"
rabbitmqctl set_permissions -p / guest ".*" ".*" ".*"
rabbitmqctl set_permissions -p test admin ".*" ".*" ".*"
rabbitmqctl set_permissions -p test guest ".*" ".*" ".*"

#rabbitmqctl list_vhosts
#rabbitmqctl list_users
#rabbitmqctl list_user_permissions guest

#echo Lets start testing...

#rabbitmq-plugins enable rabbitmq_management

service rabbitmq-server restart

rabbitmqctl list_vhosts
rabbitmqctl list_users
service --status-all
#ls -l
mkdir /app/HBOperations/DialogueStatusCheckerSchedulerTests/TestResults/
cd /app/HBOperations/DialogueStatusCheckerSchedulerTests/
dotnet test --logger:"trx;LogFileName=results.trx" ; base64 /app/HBOperations/DialogueStatusCheckerSchedulerTests/TestResults/results*.trx > /app/HBOperations/DialogueStatusCheckerSchedulerTests/TestResults/results_base64 ;
curl -X POST "https://heedbookapi.northeurope.cloudapp.azure.com/user/ExpressTester/PublishUnitTestResults" -H  "accept: application/json" -H  "Content-Type: application/json-patch+json" -d "{ \"TrxTextBase64\" : \"$(cat /app/HBOperations/DialogueStatusCheckerSchedulerTests/TestResults/results_base64)\" }";
echo test ended
#rm /app/HBOperations/DialogueStatusCheckerSchedulerTests/TestResults/*.trx -R; exit 0;
#dotnet test --logger:"trx;LogFileName=results.trx" ; base64 /app/HBOperations/DialogueStatusCheckerSchedulerTests/TestResults/results*.trx > /app/HBOperations/DialogueStatusCheckerSchedulerTests/TestResults/results_base64 ; curl -X POST "https://heedbookapi.northeurope.cloudapp.azure.com/user/ExpressTester/PublishUnitTestResults" -H  "accept: application/json" -H  "Content-Type: application/json-patch+json" -d "{ \"TrxTextBase64\" : \"$(cat /app/HBOperations/DialogueStatusCheckerSchedulerTests/TestResults/results_base64)\" }";
#rm /app/HBOperations/DialogueStatusCheckerSchedulerTests/TestResults/*.trx; exit 0;
#find /var/logs/rabbitmq/ * | xargs cat
#service --status-all