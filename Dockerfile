# ============================
# Prepare Building Environment
FROM --platform=$BUILDPLATFORM hub.aiursoft.com/aiursoft/internalimages/dotnet AS build-env
ARG TARGETARCH
WORKDIR /src
COPY . .
RUN if [ "$TARGETARCH" = "arm64" ]; then \
        RID="linux-arm64"; \
    elif [ "$TARGETARCH" = "amd64" ]; then \
        RID="linux-x64"; \
    else \
        RID="linux-$TARGETARCH"; \
    fi && \
    echo "Building for arch: $TARGETARCH, using .NET RID: $RID" && \
    dotnet publish ./src/Aiursoft.Static/Aiursoft.Static.csproj --configuration Release --no-self-contained --runtime $RID --output /app

# ============================
# Prepare Runtime Environment
FROM hub.aiursoft.com/aiursoft/internalimages/dotnet
WORKDIR /app
COPY --from=build-env /app .

RUN mkdir -p /data
RUN echo "<h1>Hello World from Aiursoft Static!</h1>" > /data/index.html

#VOLUME /data
EXPOSE 5000
ENTRYPOINT /app/static --port 5000 --path /data
