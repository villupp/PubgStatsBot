FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build-env
WORKDIR /Villupp.PubgStatsBot

# Copy everything
COPY . ./
# Restore as distinct layers
RUN dotnet restore
# Build and publish a release
RUN dotnet publish -c Release -o out

# Build runtime image
FROM mcr.microsoft.com/dotnet/runtime:8.0
WORKDIR /Villupp.PubgStatsBot
COPY --from=build-env /Villupp.PubgStatsBot/out .
ENTRYPOINT ["dotnet", "Villupp.PubgStatsBot.dll"]