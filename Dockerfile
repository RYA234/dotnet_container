# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj and restore dependencies
COPY ["BlazorApp.csproj", "./"]
RUN dotnet restore "BlazorApp.csproj"

# Copy everything else and build
COPY . .
RUN dotnet build "BlazorApp.csproj" -c Release -o /app/build
RUN dotnet publish "BlazorApp.csproj" -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .

# Set environment variables
ENV ASPNETCORE_URLS=http://+:5000
ENV ASPNETCORE_ENVIRONMENT=Production

EXPOSE 5000

ENTRYPOINT ["dotnet", "BlazorApp.dll"]
