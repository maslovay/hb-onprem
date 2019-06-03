docker build -t hbtestregistry.azurecr.io/heedbookdev/faceanalyzeservice:latest -f FaceAnalyzeService.Dockerfile . &&
docker build -t hbtestregistry.azurecr.io/heedbookdev/userservice:latest -f UserService.Dockerfile . && 
docker build -t hbtestregistry.azurecr.io/heedbookdev/fillingframeservice:latest -f FillingFrameService.Dockerfile . &&
docker build -t hbtestregistry.azurecr.io/heedbookdev/extractframesfromvideoservice:latest -f ExtractFramesFromVideoService.Dockerfile . &&
docker build -t hbtestregistry.azurecr.io/heedbookdev/videotosoundservice:latest -f VideoToSoundService.Dockerfile . &&
docker build -t hbtestregistry.azurecr.io/heedbookdev/audioanalyzeservice:latest -f AudioAnalyzeService.Dockerfile . &&
docker build -t hbtestregistry.azurecr.io/heedbookdev/audioanalyzescheduler:latest -f AudioAnalyzeScheduler.Dockerfile . &&
docker build -t hbtestregistry.azurecr.io/heedbookdev/dialoguestatuscheckerschecduler:latest -f DialogueStatusCheckerScheduler.Dockerfile . &&
docker build -t hbtestregistry.azurecr.io/heedbookdev/dialoguevideomergeservice:latest -f DialogueVideoMergeService.Dockerfile . &&

docker push hbtestregistry.azurecr.io/heedbookdev/faceanalyzeservice:latest &&
docker push hbtestregistry.azurecr.io/heedbookdev/userservice:latest && 
docker push hbtestregistry.azurecr.io/heedbookdev/fillingframeservice:latest &&
docker push hbtestregistry.azurecr.io/heedbookdev/extractframesfromvideoservice:latest && 
docker push hbtestregistry.azurecr.io/heedbookdev/videotosoundservice:latest &&
docker push hbtestregistry.azurecr.io/heedbookdev/audioanalyzeservice:latest &&
docker push hbtestregistry.azurecr.io/heedbookdev/dialoguestatuscheckerschecduler:latest &&
docker push hbtestregistry.azurecr.io/heedbookdev/audioanalyzescheduler:latest &&
docker push hbtestregistry.azurecr.io/heedbookdev/dialoguevideomergeservice:latest