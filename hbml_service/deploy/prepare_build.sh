#!/bin/bash
DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" >/dev/null 2>&1 && pwd )"
cd $DIR/../
wget http://registrationcenter-download.intel.com/akdlm/irc_nas/15078/l_openvino_toolkit_p_2018.5.455.tgz
tar -xf l_openvino_toolkit_p_2018.5.455.tgz
rm l_openvino_toolkit_p_2018.5.455.tgz
mv l_openvino_toolkit_p_2018.5.455 l_openvino_toolkit
