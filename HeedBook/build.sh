docker build -t heedbookregistry.azurecr.io/heedbookdev/faceanalyzeservice:latest -f FaceAnalyzeService.Dockerfile . &&
docker build -t heedbookregistry.azurecr.io/heedbookdev/userservice:latest -f UserService.Dockerfile . && 
docker build -t heedbookregistry.azurecr.io/heedbookdev/fillingframeservice:latest -f FillingFrameService.Dockerfile . &&
docker build -t heedbookregistry.azurecr.io/heedbookdev/extractframesfromVideoservice:latest ExtractFramesFromVideoService.Dockerfile &&
docker build -t heedbookregistry.azurecr.io/heedbookdev/videotosoundservice:latest VideoToSoundService.Dockerfile &&
docker build -t heedbookregistry.azurecr.io/heedbookdev/audioanalyzeservice:latest AudioAnalyzeService.Dockerfile &&
docker build -t heedbookregistry.azurecr.io/heedbookdev/audioanalyzescheduler:latest AudioAnalyzeScheduler.Dockerfile &&

docker push heedbookregistry.azurecr.io/heedbookdev/faceanalyzeservice:latest &&
docker push heedbookregistry.azurecr.io/heedbookdev/userservice:latest && 
docker push heedbookregistry.azurecr.io/heedbookdev/fillingframeservice:latest &&
docker push heedbookregistry.azurecr.io/heedbookdev/extractframesfromVideoservice:latest && 
docker push heedbookregistry.azurecr.io/heedbookdev/videotosoundservice:latest &&
docker push heedbookregistry.azurecr.io/heedbookdev/audioanalyzeservice:latest &&
docker push heedbookregistry.azurecr.io/heedbookdev/audioanalyzescheduler:latest