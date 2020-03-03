#!/bin/bash
cd /hbml_service/
source /opt/intel/computer_vision_sdk/bin/setupvars.sh
python3 manage.py collectstatic --no-input
gunicorn hbml_service.wsgi -b 0.0.0.0:8000