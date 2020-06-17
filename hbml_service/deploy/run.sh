#!/bin/bash
cd /hbml_service/
source /opt/intel/computer_vision_sdk/bin/setupvars.sh
python3 manage.py runserver
gunicorn hbml_service.wsgi -b 0.0.0.0:8000
