# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["ElectricalBilling.csproj", "./"]
RUN dotnet restore "ElectricalBilling.csproj"
COPY . .
WORKDIR "/src/."
RUN dotnet build "ElectricalBilling.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "ElectricalBilling.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Final stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "ElectricalBilling.dll"]