# Official source/runtime identities are pinned; no host secret or unrelated repository file enters the context.
FROM mcr.microsoft.com/dotnet/sdk:10.0.404-noble@sha256:fe0fd4e93bd2fbb47c883b7a6369e5c8d3db8bf01cdc5981f6ee35fbdf3ff4fb AS build
WORKDIR /source
COPY . .
RUN dotnet publish src/Apps/Zanance/Vafadar.Zanance.Api -c Release -r linux-x64 --self-contained false -p:ContinuousIntegrationBuild=true -o /publish/api \
 && dotnet publish eng/hosting/Bootstrap -c Release -r linux-x64 --self-contained false -p:ContinuousIntegrationBuild=true -o /publish/bootstrap
FROM mcr.microsoft.com/dotnet/aspnet:10.0.15-noble@sha256:bfd912c83295ce66d4b706ebb8541b9f14259b0e76fbb88b2751f58ce2399d2e AS service
LABEL pro.vafadar.zanance.household-privacy="deletion-v1"
WORKDIR /app
COPY --from=build /publish/api/ ./
COPY --from=build /publish/bootstrap/ /probe/
USER 1654:1654
ENTRYPOINT ["dotnet", "Vafadar.Zanance.Api.dll"]
FROM mcr.microsoft.com/dotnet/aspnet:10.0.15-noble@sha256:bfd912c83295ce66d4b706ebb8541b9f14259b0e76fbb88b2751f58ce2399d2e AS bootstrap
WORKDIR /app
COPY --from=build /publish/bootstrap/ ./
COPY eng/hosting/bootstrap-entrypoint.sh /configuration/bootstrap-entrypoint.sh
ENTRYPOINT ["sh", "/configuration/bootstrap-entrypoint.sh"]
