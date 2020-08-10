#!/usr/bin/env bash
#docker build -t nkrokhmal/faceanalyzeservice:open -f FaceAnalyzeService.Dockerfile . &&
#docker push nkrokhmal/faceanalyzeservice:open &&
#docker save -o ~/Yandex.Disk/Images/nkrokhmal/faceanalyzeservice.tar nkrokhmal/faceanalyzeservice:open &&

#docker build -t nkrokhmal/fillingframeservice:open -f FillingFrameService.Dockerfile . &&
#docker push nkrokhmal/fillingframeservice:open &&
#docker save -o ~/Yandex.Disk/Images/nkrokhmal/fillingframeservice.tar nkrokhmal/fillingframeservice:open &&

#docker build -t nkrokhmal/extractframesfromvideoservice:open -f ExtractFramesFromVideoService.Dockerfile . &&
#docker push nkrokhmal/extractframesfromvideoservice:open &&
#docker save -o ~/Yandex.Disk/Images/nkrokhmal/extractframesfromvideoservice.tar nkrokhmal/extractframesfromvideoservice:open &&

#docker build -t nkrokhmal/videotosoundservice:open -f VideoToSoundService.Dockerfile . &&
#docker push nkrokhmal/videotosoundservice:open &&
#docker save -o ~/Yandex.Disk/Images/nkrokhmal/videotosoundservice.tar nkrokhmal/videotosoundservice:open 

#-------------------------------------------

#docker build -t nkrokhmal/audioanalyzeservice:open -f AudioAnalyzeService.Dockerfile . &&
#docker push nkrokhmal/audioanalyzeservice:open &&
#docker save -o ~/Yandex.Disk/Images/nkrokhmal/audioanalyzeservice.tar nkrokhmal/audioanalyzeservice:open &&
	
#docker build -t nkrokhmal/toneanalyzeservice:open -f ToneAnalyzeService.Dockerfile . &&
#docker push nkrokhmal/toneanalyzeservice:open &&
#docker save -o ~/Yandex.Disk/Images/nkrokhmal/toneanalyzeservice.tar nkrokhmal/toneanalyzeservice:open &&

#docker build -t nkrokhmal/audioanalyzescheduler:open -f AudioAnalyzeScheduler.Dockerfile . &&
#docker push nkrokhmal/audioanalyzescheduler:open &&
#docker save -o ~/Yandex.Disk/Images/nkrokhmal/audioanalyzescheduler.tar nkrokhmal/audioanalyzescheduler:open &&

#docker build -t nkrokhmal/dialoguestatuscheckerscheduler:open -f DialogueStatusCheckerScheduler.Dockerfile . &&
#docker push nkrokhmal/dialoguestatuscheckerscheduler:open &&
#docker save -o ~/Yandex.Disk/Images/nkrokhmal/dialoguestatuscheckerscheduler.tar nkrokhmal/dialoguestatuscheckerscheduler:open &&

#docker build -t heedbook/dialoguevideoassembleservice:open -f DialogueVideoAssembleService.Dockerfile . &&
#docker push nkrokhmal/dialoguestatuscheckerscheduler:open &&
#docker save -o ~/Yandex.Disk/Images/nkrokhmal/dialoguestatuscheckerscheduler.tar nkrokhmal/dialoguestatuscheckerscheduler:open &&


#docker build -t heedbook/fillingsatisfactionservice:latest -f FillingSatisfactionService.Dockerfile . &&
docker build -t nkrokhmal/fillingsatisfactionservice:open -f FillingSatisfactionService.Dockerfile . &&
docker push nkrokhmal/fillingsatisfactionservice:open &&
docker save -o ~/Yandex.Disk/Images/nkrokhmal/fillingsatisfactionservice.tar nkrokhmal/fillingsatisfactionservice:open &&

#docker build -t heedbook/dialoguevideoassembleservice:latest -f DialogueVideoAssembleService.Dockerfile . &&
docker build -t nkrokhmal/dialoguevideoassembleservice:open -f DialogueVideoAssembleService.Dockerfile . &&
docker push nkrokhmal/dialoguevideoassembleservice:open &&
docker save -o ~/Yandex.Disk/Images/nkrokhmal/dialoguevideoassembleservice.tar nkrokhmal/dialoguevideoassembleservice:open &&

#docker build -t heedbook/detectfaceidextendedscheduler:latest -f DetectFaceIdExtendedScheduler.Dockerfile . 
docker build -t nkrokhmal/detectfaceidextendedscheduler:open -f DetectFaceIdExtendedScheduler.Dockerfile . &&
docker push nkrokhmal/detectfaceidextendedscheduler:open &&
docker save -o ~/Yandex.Disk/Images/nkrokhmal/detectfaceidextendedscheduler.tar nkrokhmal/detectfaceidextendedscheduler:open 

#docker build -t heedbook/dialoguecreator -f DialogueCreatorScheduler.Dockerfile . &&
docker build -t nkrokhmal/dialoguecreator:open -f DialogueCreatorScheduler.Dockerfile . &&
docker push nkrokhmal/dialoguecreator:open &&
docker save -o ~/Yandex.Disk/Images/nkrokhmal/dialoguecreator.tar nkrokhmal/dialoguecreator:open &&

#docker build -t heedbook/persondetectionservice -f PersonDetectionService.Dockerfile . &&
docker build -t nkrokhmal/persondetectionservice:open -f PersonDetectionService.Dockerfile . &&
docker push nkrokhmal/persondetectionservice:open &&
docker save -o ~/Yandex.Disk/Images/nkrokhmal/persondetectionservice.tar nkrokhmal/persondetectionservice:open &&

#docker build -t heedbook/persononlinedetectionservice -f PersonOnlineDetectionService.Dockerfile .
docker build -t nkrokhmal/persononlinedetectionservice:open -f PersonOnlineDetectionService.Dockerfile . &&
docker push nkrokhmal/persononlinedetectionservice:open &&
docker save -o ~/Yandex.Disk/Images/nkrokhmal/persononlinedetectionservice.tar nkrokhmal/persononlinedetectionservice:open &&

#docker build -t heedbook/useroperations -f UserOperations.Dockerfile . &&
docker build -t nkrokhmal/useroperations:open -f UserOperations.Dockerfile . &&
docker push nkrokhmal/useroperations:open &&
docker save -o ~/Yandex.Disk/Images/nkrokhmal/useroperations.tar nkrokhmal/useroperations:open &&

#docker build -t heedbook/userservice -f UserService.Dockerfile .
docker build -t nkrokhmal/userservice:open -f UserService.Dockerfile . &&
docker push nkrokhmal/userservice:open &&
docker save -o ~/Yandex.Disk/Images/nkrokhmal/userservice.tar nkrokhmal/userservice:open &&

#docker build -t heedbook/hbmlonlineservice -f HBMLOnlineService.Dockerfile .
docker build -t nkrokhmal/hbmlonlineservice:open -f HBMLOnlineService.Dockerfile . &&
docker push nkrokhmal/hbmlonlineservice:open &&
docker save -o ~/Yandex.Disk/Images/nkrokhmal/hbmlonlineservice.tar nkrokhmal/hbmlonlineservice:open 
