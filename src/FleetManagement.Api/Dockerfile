FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY src/FleetManagement.Core/ src/FleetManagement.Core/
COPY src/FleetManagement.Api/ src/FleetManagement.Api/
RUN dotnet publish src/FleetManagement.Api -c Release -o /out
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /out .
USER app
EXPOSE 8080
ENTRYPOINT ["dotnet","FleetManagement.Api.dll"]
