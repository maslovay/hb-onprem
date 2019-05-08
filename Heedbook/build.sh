docker build -t hbcontainerregistry.azurecr.io/faceanalyzeservice:latest -f FaceAnalyzeService.Dockerfile . &&
docker build -t hbcontainerregistry.azurecr.io/userservice:latest -f UserService.Dockerfile . && 
docker build -t hbcontainerregistry.azurecr.io/useroperations:latest -f UserOperations.Dockerfile . && 
docker build -t hbcontainerregistry.azurecr.io/fillingframeservice:latest -f FillingFrameService.Dockerfile . &&
docker build -t hbcontainerregistry.azurecr.io/extractframesfromvideoservice:latest -f ExtractFramesFromVideoService.Dockerfile . &&
docker build -t hbcontainerregistry.azurecr.io/videotosoundservice:latest -f VideoToSoundService.Dockerfile . &&
docker build -t hbcontainerregistry.azurecr.io/audioanalyzeservice:latest -f AudioAnalyzeService.Dockerfile . &&
docker build -t hbcontainerregistry.azurecr.io/audioanalyzescheduler:latest -f AudioAnalyzeScheduler.Dockerfile . &&
docker build -t hbcontainerregistry.azurecr.io/dialoguestatuscheckerschecduler:latest -f DialogueStatusCheckerScheduler.Dockerfile . &&
docker build -t hbcontainerregistry.azurecr.io/dialoguevideomergeservice:latest -f DialogueVideoMergeService.Dockerfile . &&
docker build -t hbcontainerregistry.azurecr.io/fillinghintservice:latest -f FillingHintService.Dockerfile . &&
docker build -t hbcontainerregistry.azurecr.io/fillingsatisfactionservice:latest -f FillingSatisfactionService.Dockerfile . &&
docker build -t hbcontainerregistry.azurecr.io/userservicetests:latest -f UserService.Dockerfile.integration . && 
                                                               
docker push hbcontainerregistry.azurecr.io/faceanalyzeservice:latest &&
docker push hbcontainerregistry.azurecr.io/userservice:latest && 
docker push hbcontainerregistry.azurecr.io/useroperations:latest && 
docker push hbcontainerregistry.azurecr.io/fillingframeservice:latest &&
docker push hbcontainerregistry.azurecr.io/extractframesfromvideoservice:latest && 
docker push hbcontainerregistry.azurecr.io/videotosoundservice:latest &&
docker push hbcontainerregistry.azurecr.io/audioanalyzeservice:latest &&
docker push hbcontainerregistry.azurecr.io/dialoguestatuscheckerschecduler:latest &&
docker push hbcontainerregistry.azurecr.io/audioanalyzescheduler:latest &&
docker push hbcontainerregistry.azurecr.io/dialoguevideomergeservice:latest &&
docker push hbcontainerregistry.azurecr.io/fillinghintservice:latest &&
docker push hbcontainerregistry.azurecr.io/fillingsatisfactionservice:latest  &&
docker push hbcontainerregistry.azurecr.io/userservicetests:latest
