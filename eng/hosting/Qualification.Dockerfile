FROM mcr.microsoft.com/dotnet/sdk:10.0.402-noble@sha256:0d99ebf1ccad15037b5c98c9fe8979d6b8dbac23812feae2277a0b277b5c5ffa AS build
WORKDIR /source
COPY . .
RUN dotnet publish eng/hosting/Qualification -c Release -r linux-x64 --self-contained false -p:ContinuousIntegrationBuild=true -o /publish
FROM mcr.microsoft.com/dotnet/aspnet:10.0.13-noble@sha256:d5633778c754e89a386882eddf8dbee6942447a8a4b8267c19cf1e1158d0b0ac
WORKDIR /app
COPY --from=build /publish/ ./
USER 1654:1654
ENTRYPOINT ["dotnet", "Qualification.dll"]
