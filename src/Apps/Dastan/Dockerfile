# The Dastan website as a container image (docs/operations.md, ADR 0010).
#
# Build from the root of the repository – the site uses the shared libraries and the build settings there:
#
#   docker build -f src/Apps/Dastan/Dockerfile -t dastan-site .
#
# The image holds the program and nothing of a collection: what the site shows is a folder that is mounted when
# the container is started (/app/content, read-only). What is sent to the build is named in Dockerfile.dockerignore.

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /repo
COPY . .

# The repository names two local package folders for other apps; they have to exist, and stay empty here.
RUN mkdir -p artifacts/native-sqlcipher/feed artifacts/maui-core/feed \
    && dotnet publish src/Apps/Dastan/Vafadar.Dastan.Web -c Release -o /site -p:ContinuousIntegrationBuild=true

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS site
WORKDIR /app
COPY --from=build /site .

# The site listens on 8080 without encryption: a proxy in front of it holds the certificate and says with which
# address the visitor came. The site believes only the proxies it is told to: Dastan__TrustedProxies__0, __1, …
ENV ASPNETCORE_HTTP_PORTS=8080 \
    DOTNET_gcServer=0
EXPOSE 8080

# Not the administrator of the container.
USER $APP_UID

# The program asks its own site; the image has no other tool to ask with.
HEALTHCHECK --interval=30s --timeout=6s --start-period=20s --retries=3 CMD ["dotnet", "Vafadar.Dastan.Web.dll", "--probe"]

ENTRYPOINT ["dotnet", "Vafadar.Dastan.Web.dll"]
