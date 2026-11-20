# The Dastan website as a container image (docs/operations.md, ADR 0010).
#
# Build from the root of the repository – the site uses the shared libraries and the build settings there:
#
#   docker build -f src/Apps/Dastan/Dockerfile -t dastan-site .
#
# The image holds the program and nothing of a collection: what the site shows is a folder that is mounted when
# the container is started (/app/content, read-only). What is sent to the build is named in Dockerfile.dockerignore.

# The script of the site is built from its TypeScript in a stage of its own (ADR 0019): the compiler and the
# bundler come from the lock file, are used here and are not in the image that runs.
FROM node:24-bookworm-slim AS scripts
WORKDIR /site/Scripts
COPY src/Apps/Dastan/Vafadar.Dastan.Web/Scripts/package.json src/Apps/Dastan/Vafadar.Dastan.Web/Scripts/package-lock.json src/Apps/Dastan/Vafadar.Dastan.Web/Scripts/.npmrc ./
RUN npm ci
COPY src/Apps/Dastan/Vafadar.Dastan.Web/Scripts/ ./
RUN node node_modules/typescript/bin/tsc --noEmit -p tsconfig.json && node build.mjs

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /repo
COPY . .
COPY --from=scripts /site/wwwroot/app.js src/Apps/Dastan/Vafadar.Dastan.Web/wwwroot/app.js

# The repository names two local package folders for other apps; they have to exist, and stay empty here.
RUN mkdir -p artifacts/native-sqlcipher/feed artifacts/maui-core/feed \
    && dotnet publish src/Apps/Dastan/Vafadar.Dastan.Web -c Release -o /site -p:ContinuousIntegrationBuild=true -p:BuildDastanScripts=false \
    && dotnet publish src/Apps/Dastan/Vafadar.Dastan.Content -c Release -o /command -p:ContinuousIntegrationBuild=true

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS site

# The driver of PostgreSQL looks for the Kerberos library of the system when it first connects. The site does
# not use Kerberos, but without the library the runtime writes a complaint into the log of every start.
RUN apt-get update \
    && apt-get install -y --no-install-recommends libgssapi-krb5-2 \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /app
COPY --from=build /site .

# The command of the operator (operations.md) comes with the site: the tables of the database are made and
# brought up with it, notices are read, and what may not stay is restricted – by a person, never by the site.
COPY --from=build /command /command

# The site listens on 8080 without encryption: a proxy in front of it holds the certificate and says with which
# address the visitor came. The site believes only the proxies it is told to: Dastan__TrustedProxies__0, __1, …
ENV ASPNETCORE_HTTP_PORTS=8080 \
    DOTNET_gcServer=0
EXPOSE 8080

# What the site keeps – the store of the visitors, their photographs, the keys of the forms, the reports – lies
# on a volume mounted at /data. The folder exists in the image and belongs to the user of the container, so that
# a new volume does too.
RUN mkdir -p /data && chown $APP_UID /data

# Not the administrator of the container.
USER $APP_UID

# The program asks its own site; the image has no other tool to ask with.
HEALTHCHECK --interval=30s --timeout=6s --start-period=20s --retries=3 CMD ["dotnet", "Vafadar.Dastan.Web.dll", "--probe"]

ENTRYPOINT ["dotnet", "Vafadar.Dastan.Web.dll"]
