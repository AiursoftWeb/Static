ARG CSPROJ_PATH="./src/Aiursoft.Static/"
ARG PROJ_NAME="Aiursoft.Static"

# ============================
# Prepare Building Environment
FROM hub.aiursoft.cn/mcr.microsoft.com/dotnet/sdk:8.0 as build-env
ARG CSPROJ_PATH
ARG PROJ_NAME
WORKDIR /src
COPY . .

# Build
RUN dotnet publish ${CSPROJ_PATH}${PROJ_NAME}.csproj  --configuration Release --no-self-contained --runtime linux-x64 --output /app

# ============================
# Prepare Runtime Environment
FROM hub.aiursoft.cn/mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build-env /app .

RUN mkdir -p /data
RUN echo "<h1>Hello World</h1>" > /data/index.html

VOLUME /data
EXPOSE 5000
ENTRYPOINT /app/static --port 5000 --path /data