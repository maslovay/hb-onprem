# ============ OPENVINO ============
# Taken from https://github.com/mateoguzman/openvino-docker
FROM ubuntu:16.04
COPY l_openvino_toolkit* /openvino/l_openvino_toolkit
RUN cd /openvino/l_openvino_toolkit*
ARG INSTALL_DIR=/opt/intel/computer_vision_sdk
RUN apt-get update && apt-get -y upgrade && apt-get autoremove
#Install needed dependences
RUN apt-get install -y --no-install-recommends \
        build-essential \
        cpio \
        curl \
        git \
        lsb-release \
        pciutils \
        python3.5 \
        python3.5-dev \
        python3-pip \
        python3-setuptools \
        sudo
# installing OpenVINO dependencies
RUN cd /openvino/l_openvino_toolkit* && \
    ./install_cv_sdk_dependencies.sh
RUN pip3 install --upgrade pip
RUN pip3 install numpy
# installing OpenVINO itself
RUN cd /openvino/l_openvino_toolkit* && \
    sed -i 's/decline/accept/g' silent.cfg && \
    ./install.sh --silent silent.cfg
# Model Optimizer
RUN cd $INSTALL_DIR/deployment_tools/model_optimizer/install_prerequisites && \
    ./install_prerequisites.sh
# clean up
RUN apt autoremove -y && \
    rm -rf /openvino /var/lib/apt/lists/*
RUN echo "source $INSTALL_DIR/bin/setupvars.sh" >> /root/.bashrc
# ============ DJANGO ============
RUN mkdir /hbml_service
RUN mkdir /static
WORKDIR /hbml_service
ADD . /hbml_service
RUN pip3 install --upgrade pip
RUN pip3 install -r deploy/requirements.txt
CMD ["/bin/bash", "-c", "/hbml_service/deploy/run.sh"]