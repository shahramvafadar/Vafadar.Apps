# Official source/runtime identities are pinned; no host secret or unrelated repository file enters the context.
FROM mcr.microsoft.com/dotnet/sdk:10.0.402-noble@sha256:0d99ebf1ccad15037b5c98c9fe8979d6b8dbac23812feae2277a0b277b5c5ffa AS build
WORKDIR /source
COPY . .
RUN dotnet publish src/Apps/Zanance/Vafadar.Zanance.Api -c Release -r linux-x64 --self-contained false -p:ContinuousIntegrationBuild=true -o /publish/api \
 && dotnet publish eng/hosting/Bootstrap -c Release -r linux-x64 --self-contained false -p:ContinuousIntegrationBuild=true -o /publish/bootstrap
FROM mcr.microsoft.com/dotnet/aspnet:10.0.13-noble@sha256:d5633778c754e89a386882eddf8dbee6942447a8a4b8267c19cf1e1158d0b0ac AS service
WORKDIR /app
COPY --from=build /publish/api/ ./
COPY --from=build /publish/bootstrap/ /probe/
USER 1654:1654
ENTRYPOINT ["dotnet", "Vafadar.Zanance.Api.dll"]
FROM mcr.microsoft.com/dotnet/aspnet:10.0.13-noble@sha256:d5633778c754e89a386882eddf8dbee6942447a8a4b8267c19cf1e1158d0b0ac AS bootstrap
WORKDIR /app
COPY --from=build /publish/bootstrap/ ./
COPY eng/hosting/bootstrap-entrypoint.sh /configuration/bootstrap-entrypoint.sh
ENTRYPOINT ["sh", "/configuration/bootstrap-entrypoint.sh"]
