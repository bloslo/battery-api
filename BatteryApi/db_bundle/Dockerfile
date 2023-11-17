FROM mcr.microsoft.com/dotnet/sdk:7.0

WORKDIR /App

COPY . ./

RUN dotnet restore

RUN dotnet tool install --global dotnet-ef --version 7.0.14

ENV PATH="$PATH:/root/.dotnet/tools"

# Generate a self-contained bundle for Linux
RUN dotnet ef migrations bundle --self-contained -r linux-x64 -f

RUN chmod +x ./db_bundle/run_migrations.sh

ENTRYPOINT [ "db_bundle/run_migrations.sh" ]
