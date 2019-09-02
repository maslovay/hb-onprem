#!/usr/bin/env bash
docker save  containerregistryhb.azurecr.io/faceanalyzeservice:latest > faceanalyze &&
docker save  containerregistryhb.azurecr.io/userservice:latest > userservice  && 
docker save  containerregistryhb.azurecr.io/useroperations:latest > useroperations  && 
docker save  containerregistryhb.azurecr.io/fillingframeservice:latest > fillingframe  &&
docker save  containerregistryhb.azurecr.io/extractframesfromvideoservice:latest > extractframes &&
docker save  containerregistryhb.azurecr.io/videotosoundservice:latest > videotosound &&
docker save  containerregistryhb.azurecr.io/audioanalyzeservice:latest > audioanalyze &&
docker save  containerregistryhb.azurecr.io/toneanalyzeservice:latest > toneanalyze &&
docker save  containerregistryhb.azurecr.io/audioanalyzescheduler:latest > audioanalyzescheduler &&
docker save  containerregistryhb.azurecr.io/dialoguestatuscheckerscheduler:latest > dialoguestatuschecker &&
docker save  containerregistryhb.azurecr.io/fillinghintservice:latest > fillinghints &&
docker save  containerregistryhb.azurecr.io/fillingsatisfactionservice:latest > fillingsatisfaction &&
docker save  containerregistryhb.azurecr.io/dialoguevideoassembleservice:latest > dialoguevideoassemble &&
docker save  containerregistryhb.azurecr.io/dialoguemarkupservice:latest > dialoguemarkup &&
docker save  containerregistryhb.azurecr.io/sessionstatusscheduler:latest > sessionstatus &&
docker save  containerregistryhb.azurecr.io/persondetectionservice:latest > persondetection 
