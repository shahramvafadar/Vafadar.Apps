FROM mcr.microsoft.com/dotnet/sdk:10.0.404-noble@sha256:fe0fd4e93bd2fbb47c883b7a6369e5c8d3db8bf01cdc5981f6ee35fbdf3ff4fb AS build
WORKDIR /source
COPY . .
RUN dotnet publish eng/hosting/Qualification -c Release -r linux-x64 --self-contained false -p:ContinuousIntegrationBuild=true -o /publish
FROM mcr.microsoft.com/dotnet/aspnet:10.0.15-noble@sha256:bfd912c83295ce66d4b706ebb8541b9f14259b0e76fbb88b2751f58ce2399d2e
WORKDIR /app
COPY --from=build /publish/ ./
USER 1654:1654
ENTRYPOINT ["dotnet", "Qualification.dll"]
