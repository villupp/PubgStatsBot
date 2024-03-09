# PubgStatsBot

.NET 8 based Discord bot utilizing https://discordnet.dev/ library. Retrieves PUBG stats from PUBG API: https://developer.pubg.com/.

# Prerequisites

- .NET 8 SDK
- Docker (optional, to run in a container)

# Container setup

From guide here: https://learn.microsoft.com/en-us/dotnet/core/docker/build-container?tabs=windows&pivots=dotnet-8-0.

## To build Docker image

Where `Dockerfile` resides, run:

`docker build -t villupp-pubgstatsbot -f Dockerfile .`

## To run in Docker container

Create container:

`docker create --name pubgstatsbot-container villupp-pubgstatsbot`

Run container:

`docker start pubgstatsbot-container`

To see logs, connect to the container:

`docker attach --sig-proxy=false pubgstatsbot-container`

Stop container:

`docker stop pubgstatsbot-container`

# To maintain Azure Container App

## Set environment variables

Environment variable names can't contain colons `:`. Replace these with double underscore `__`.

Update with Azure CLI with `az containerapp update`, for example:

`az containerapp update -n vp-pubgstatsbot-ca -g vp-discordbots-rg --set-env-vars Logging__LogLevel__Azure.Core=Warning Logging__LogLevel__System.Net.Http=Warning`

To refer a secret, use `envvar=secretref:secretname`