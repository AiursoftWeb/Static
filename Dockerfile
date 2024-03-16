# ============================
# Prepare Building Environment
FROM hub.aiursoft.cn/mcr.microsoft.com/dotnet/sdk:8.0 as build-env
WORKDIR /src
COPY . .
RUN dotnet publish ./src/Aiursoft.Static/Aiursoft.Static.csproj  --configuration Release --no-self-contained --runtime linux-x64 --output /app

# ============================
# Prepare Runtime Environment
FROM hub.aiursoft.cn/mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build-env /app .

RUN mkdir -p /data
RUN echo "<h1>Hello World from Aiursoft Static!</h1>" > /data/index.html

#VOLUME /data
EXPOSE 5000
ENTRYPOINT /app/static --port 5000 --path /data
