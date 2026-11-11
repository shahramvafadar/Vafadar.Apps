# Runtime-only servicing keeps every original application/probe byte and never copies private volumes.
# Omission fails COPY from scratch while keeping a valid, fail-closed FROM argument.
ARG ORIGINAL_SERVICE_IMAGE=scratch
FROM ${ORIGINAL_SERVICE_IMAGE} AS original
FROM mcr.microsoft.com/dotnet/aspnet:10.0.15-noble@sha256:bfd912c83295ce66d4b706ebb8541b9f14259b0e76fbb88b2751f58ce2399d2e AS service
ARG ORIGINAL_SERVICE_ID
LABEL pro.vafadar.zanance.household-privacy="deletion-v1" \
      pro.vafadar.zanance.runtime-servicing="10.0.15" \
      pro.vafadar.zanance.runtime-source-image="${ORIGINAL_SERVICE_ID}"
WORKDIR /app
COPY --from=original /app/ /app/
COPY --from=original /probe/ /probe/
USER 1654:1654
ENTRYPOINT ["dotnet", "Vafadar.Zanance.Api.dll"]
