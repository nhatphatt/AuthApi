# Use the official .NET 9.0 SDK image for building
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build

# Set working directory
WORKDIR /app

# Copy project file and restore dependencies
COPY *.csproj ./
RUN dotnet restore

# Copy source code and build the application
COPY . ./
RUN dotnet publish AuthApi.csproj -c Release -o out

# Use the official .NET 9.0 runtime image for running
FROM mcr.microsoft.com/dotnet/aspnet:9.0

# Set working directory
WORKDIR /app

# Copy the build output from the previous stage
COPY --from=build /app/out .

# Expose port 8080 for Railway/cloud deployment
EXPOSE 8080

# Set the entry point
ENTRYPOINT ["dotnet", "AuthApi.dll"]