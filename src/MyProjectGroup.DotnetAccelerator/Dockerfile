# THIS DOCKERFILE IS OPTIMIZED FOR INNERLOOP AND IS NOT APPROPRIATE FOR PRODUCTION USE. FOR OUTERLOOP, USE .NET BUILDPACK
# Features:
# - multistage build (compile in sdk container -> copy to run image)
# - remote debugging agents for Visual Studio & Rider (automatically downloaded)
# - watchexec to automatically restart the app if any dlls are hotpached in the container (tilt liveupdate)
# - openSSH server to allow remote debugging sessions (user: root  pass: admin) 
# - optimized layering to maximize cache hits (using NMica)
# - caching for downloaded dependencies:
#    - nuget packages
#    - remote debugging agents
#    - watchexec

# - 
# expected docker context is git root
FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
RUN apt-get update && apt-get install unzip
# download extra dependencies if they are not already in cache
RUN --mount=type=cache,id=download,target=/download if [ ! -f /download/watchexec.deb ]; then wget -O /download/watchexec.deb https://github.com/watchexec/watchexec/releases/download/cli-v1.17.1/watchexec-1.17.1-x86_64-unknown-linux-musl.deb; fi
RUN --mount=type=cache,id=download,target=/download dpkg -i /download/watchexec.deb
RUN --mount=type=cache,id=download,target=/download if [ ! -f /download/remotedebug.zip ]; then wget -O /download/remotedebug.zip https://github.com/macsux/dotnet-accelerator/releases/download/tools/JetBrains.Rider.RemoteDebuggerUploads.linux.2021.3EAP7.zip; fi
RUN --mount=type=cache,id=download,target=/download if [ ! -d /download/remotedebug ]; then unzip -o /download/remotedebug.zip -d /download/remotedebug/; fi 
RUN --mount=type=cache,id=download,target=/download if [ ! -d /download/vsdbg ]; then curl -sSL https://aka.ms/getvsdbgsh | bash /dev/stdin -r linux-arm -v latest -l /download/vsdbg; fi 


WORKDIR /
COPY ["src/MyProjectGroup.Common/MyProjectGroup.Common.csproj", "/src/MyProjectGroup.Common/MyProjectGroup.Common.csproj"]
COPY ["src/MyProjectGroup.DotnetAccelerator/MyProjectGroup.DotnetAccelerator.csproj", "/src/MyProjectGroup.DotnetAccelerator/MyProjectGroup.DotnetAccelerator.csproj"]
ENV NUGET_PACKAGES=/nuget
RUN --mount=type=cache,id=nuget,target=/nuget dotnet restore -r ubuntu-x64  "/src/MyProjectGroup.DotnetAccelerator/MyProjectGroup.DotnetAccelerator.csproj"
COPY . .
RUN --mount=type=cache,id=nuget,target=/nuget dotnet msbuild /p:RuntimeIdentifier=ubuntu-x64 /p:SelfContained=false /p:_CommandLineDefinedRuntimeIdentifier=true /p:RestorePackages=false /t:PublishLayer /p:PublishDir=/layer/ /p:DockerLayer=All "/src/MyProjectGroup.DotnetAccelerator/MyProjectGroup.DotnetAccelerator.csproj"

 
FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS run
# install watchexec which will restart the process when any of the files are updated (to support hotpatching container)
COPY --from=build /usr/bin/watchexec /usr/bin/watchexec
# install JetBrains Rider debugging agent
RUN --mount=type=cache,id=download,target=/download mkdir -p /root/.local/share/JetBrains/RiderRemoteDebugger/ && cp -R /download/remotedebug /root/.local/share/JetBrains/RiderRemoteDebugger/2021.3EAP7
# install Visual Studio debugging agent
RUN --mount=type=cache,id=download,target=/download cp -R /download/vsdbg /root/vsdbg
# setup open ssh server to allow Rider to establish debugging session
RUN apt-get update && apt-get install -y openssh-server
RUN echo 'root:admin' | chpasswd
RUN echo "PasswordAuthentication yes" >> /etc/ssh/sshd_config
RUN echo "PermitRootLogin yes" >> /etc/ssh/sshd_config
RUN update-rc.d ssh enable

WORKDIR /app
# apply inteligent layering to maximize cache hits and reduce image size between builds
COPY --from=build /layer/package ./
COPY --from=build /layer/earlypackage ./
COPY --from=build /layer/project ./
COPY --from=build /layer/app ./

ENTRYPOINT ["bash", "-c", "/etc/init.d/ssh start && watchexec --ignore *.yaml --restart --watch . 'dotnet MyProjectGroup.DotnetAccelerator.dll'"]
ENV PORT=8080
ENV MANAGMENTPORT=8090
ENV ASPNETCORE_URLS="http://0.0.0.0:${PORT};http://0.0.0.0:${MANAGMENTPORT}"
ENV ASPNETCORE_LOGGING__CONSOLE__DISABLECOLORS=true
ENV LOGGING__CONSOLE__DISABLECOLORS=true
EXPOSE 8080
