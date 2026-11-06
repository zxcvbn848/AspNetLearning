FROM mcr.microsoft.com/dotnet/sdk:10.0

RUN apt-get update \
    && apt-get install -y --no-install-recommends --only-upgrade libbrotli1 libpcre2-8-0 \
    && rm -rf /var/lib/apt/lists/*
