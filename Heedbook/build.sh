docker build -t hbtestregistry.azurecr.io/faceanalyzeservice:latest -f FaceAnalyzeService.Dockerfile . &&
docker build -t hbtestregistry.azurecr.io/userservice:latest -f UserService.Dockerfile . && 
docker build -t hbtestregistry.azurecr.io/useroperations:latest -f UserOperations.Dockerfile . && 
docker build -t hbtestregistry.azurecr.io/fillingframeservice:latest -f FillingFrameService.Dockerfile . &&
docker build -t hbtestregistry.azurecr.io/extractframesfromvideoservice:latest -f ExtractFramesFromVideoService.Dockerfile . &&
docker build -t hbtestregistry.azurecr.io/videotosoundservice:latest -f VideoToSoundService.Dockerfile . &&
docker build -t hbtestregistry.azurecr.io/audioanalyzeservice:latest -f AudioAnalyzeService.Dockerfile . &&
docker build -t hbtestregistry.azurecr.io/audioanalyzescheduler:latest -f AudioAnalyzeScheduler.Dockerfile . &&
docker build -t hbtestregistry.azurecr.io/dialoguestatuscheckerscheduler:latest -f DialogueStatusCheckerScheduler.Dockerfile . &&
docker build -t hbtestregistry.azurecr.io/dialoguevideomergeservice:latest -f DialogueVideoMergeService.Dockerfile . &&
docker build -t hbtestregistry.azurecr.io/fillinghintservice:latest -f FillingHintService.Dockerfile . &&
docker build -t hbtestregistry.azurecr.io/fillingsatisfactionservice:latest -f FillingSatisfactionService.Dockerfile . &&
docker build -t hbtestregistry.azurecr.io/dialoguevideoassembleservice:latest -f DialogueVideoAssembleService.Dockerfile . &&
docker build -t hbtestregistry.azurecr.io/dialoguemarkupservice:latest -f DialogueMarkUpService.Dockerfile . &&
docker build -t hbtestregistry.azurecr.io/sessionstatusscheduler:latest -f SessionStatusScheduler.Dockerfile . &&
docker build -t hbtestregistry.azurecr.io/onlinetuiofficesscheduler:latest -f OnlineTuiOfficesScheduler.Dockerfile . &&
docker build -t hbtestregistry.azurecr.io/heedbookdevelopmentstatisticsscheduler:latest -f HeedbookDevelopmentStatisticsScheduler.Dockerfile .

docker push hbtestregistry.azurecr.io/faceanalyzeservice:latest &&
docker push hbtestregistry.azurecr.io/userservice:latest && 
docker push hbtestregistry.azurecr.io/useroperations:latest && 
docker push hbtestregistry.azurecr.io/fillingframeservice:latest &&
docker push hbtestregistry.azurecr.io/extractframesfromvideoservice:latest && 
docker push hbtestregistry.azurecr.io/videotosoundservice:latest &&
docker push hbtestregistry.azurecr.io/audioanalyzeservice:latest &&
docker push hbtestregistry.azurecr.io/dialoguestatuscheckerscheduler:latest &&
docker push hbtestregistry.azurecr.io/audioanalyzescheduler:latest &&
docker push hbtestregistry.azurecr.io/dialoguevideomergeservice:latest &&
docker push hbtestregistry.azurecr.io/fillinghintservice:latest &&
docker push hbtestregistry.azurecr.io/fillingsatisfactionservice:latest &&
docker push hbtestregistry.azurecr.io/dialoguevideoassembleservice:latest &&
docker push hbtestregistry.azurecr.io/dialoguemarkupservice:latest &&
docker push hbtestregistry.azurecr.io/sessionstatusscheduler:latest &&
docker push hbtestregistry.azurecr.io/onlinetuiofficesscheduler:latest &&
docker push hbtestregistry.azurecr.io/heedbookdevelopmentstatisticsscheduler:latest
