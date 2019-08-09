command="HbApiTester.dll"
running=`ps ax | grep -v grep | grep $command | wc -l`
if [ $running -gt 0 ]; then
    echo "Dotnet is running"
else
    echo "Dotnet is not running! Trying to restart alarm server!"
    cd ~/sources/hb-onprem/Heedbook/ExpressHbApiTester/HbApiTester/
    dotnet run
fi
