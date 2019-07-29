#!/usr/bin/env bash
sudo apt install systemd

launcher=$(pwd)/run.sh
workdir=$(pwd)/../HbApiTester

sudo chmod +x $launcher

sed -i "s|LAUNCHERPATH|$launcher|g" apitester.service
sudo cp apitester.service /etc/systemd/system/

sed -i "s|WORKDIR|$workdir|g" run.sh

sudo systemctl enable apitester.service
sudo systemctl start apitester.service