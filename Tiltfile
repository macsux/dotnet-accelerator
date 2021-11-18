os.putenv ('DOCKER_BUILDKIT' , '1' )
isWindows = True if os.name == "nt" else False

name = 'macsux/dotnet-accelerator'
caching_ref = name + ":tilt-build-pack-caching"
expected_ref = "%EXPECTED_REF%" if isWindows else "$EXPECTED_REF"

custom_build(
        name,
        'docker build . -f ./src/MyProjectGroup.DotnetAccelerator/Dockerfile -t ' + expected_ref,
        deps=["./src/MyProjectGroup.DotnetAccelerator/bin/.buildsync", "./src/MyProjectGroup.DotnetAccelerator/Dockerfile", "./config"],
        live_update=[
            sync('./src/MyProjectGroup.DotnetAccelerator/bin/.buildsync', '/app'),
            sync('./config', '/app/config'),
        ]
    )

rid = "ubuntu.18.04-x64"
configuration = "Debug"
isWindows = True if os.name == "nt" else False

local_resource(
  'live-update-build',
  cmd= 'dotnet publish src/MyProjectGroup.DotnetAccelerator --configuration ' + configuration + ' --runtime ubuntu.18.04-x64 --self-contained false --output ./src/MyProjectGroup.DotnetAccelerator/bin/.buildsync',
  deps=['./src/MyProjectGroup.DotnetAccelerator/bin/' + configuration]
)

k8s_yaml(['kubernetes/deployment.yaml'])
k8s_resource('dotnet-accelerator', port_forwards=[8080,22])