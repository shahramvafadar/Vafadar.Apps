# Official source/runtime identities are pinned; no host secret or unrelated repository file enters the context.
FROM mcr.microsoft.com/dotnet/sdk:10.0.403-noble@sha256:de4fe90a98845d75910b9769662465c84de1a934665c431279d1382b2bddc02b AS build
WORKDIR /source
COPY . .
RUN dotnet publish src/Apps/Zanance/Vafadar.Zanance.Api -c Release -r linux-x64 --self-contained false -p:ContinuousIntegrationBuild=true -o /publish/api \
 && dotnet publish eng/hosting/Bootstrap -c Release -r linux-x64 --self-contained false -p:ContinuousIntegrationBuild=true -o /publish/bootstrap
FROM mcr.microsoft.com/dotnet/aspnet:10.0.14-noble@sha256:de0ee358cdd672f7017a003bed21e9ac9ed48a27c922a59b18175b6e82fa7fee AS service
LABEL pro.vafadar.zanance.household-privacy="deletion-v1"
WORKDIR /app
COPY --from=build /publish/api/ ./
COPY --from=build /publish/bootstrap/ /probe/
USER 1654:1654
ENTRYPOINT ["dotnet", "Vafadar.Zanance.Api.dll"]
FROM mcr.microsoft.com/dotnet/aspnet:10.0.14-noble@sha256:de0ee358cdd672f7017a003bed21e9ac9ed48a27c922a59b18175b6e82fa7fee AS bootstrap
WORKDIR /app
COPY --from=build /publish/bootstrap/ ./
COPY eng/hosting/bootstrap-entrypoint.sh /configuration/bootstrap-entrypoint.sh
ENTRYPOINT ["sh", "/configuration/bootstrap-entrypoint.sh"]
