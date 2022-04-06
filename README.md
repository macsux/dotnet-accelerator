## Tanzu Application Platform C# accelerator

This is a starter template for advanced enterprise projects built in .NET. It encompasses a number of architectural opinions that will enable best practices in a typical enterprise app. This template can be used both as a `dotnet new` template and as TAP accelerator.

### Installing into TAP

You can add this template as an accelerator into a TAP cluster by running the following command:

```
kubectl apply -f .template.config/myaccelerator.yaml -n accelerator-system
```

### Embedded opinions

- High level build system based on Nuke.build
  - Build shell script entrypoints for every OS
  - Automatic acquisition of all required dependencies, including .NET SDK 
  - Local environment initialization
    - Initialize local git repo for GitFlow workflow
  - Helper build targets for branching and release managements
- Project versioning derived from git history using Nerdbank.Gitversioning. This is integrated with branching strategy.
- Optional JWT authorization for API endpoints (template option)
- Integration with Tilt (TAP inner loop)
  - Helper build target to start tilt (can be used instead of IDE plugin)
  - Automatic generation of Tiltfile and deployment manifest
  - LiveSync patching of containers
  - Inner loop optimized container
    - Debugging agents for Visual Studio & Rider
    - SSH port exposed
    - Dependency caching between builds
    - Intelligent layering to maximize cache hits
    - Watchexec /w automatic restart of app after live container patch
- Configurable databases: SQLite, MySQL or PostgreSQL
- Entity Framework migrations with build target to automatically generating new ones
- Build target for publishing app as a version stamped zip artifact
- Architecture
  - Structured monolith easily upgradable to microservices
  - Module based project layout organized by feature rather than layers
  - Loose coupling via in-memory message bus implemented using MediatR
- Config managment
  - Optional integration with Spring Config Server (template option)
  - Ability to define default config values for all projects in solution
  - YAML based configuration
  - Support for profiles (Spring style)
- Observability
  - Actuator support /w Steeltoe exposed on management port
  - TAP LiveView support
  - Spring Boot Admin support (prewired for use via docker-compose)
  - Distributed tracing with Zipkin (prewired for use via docker-compose)
- Testing
  - Example on how to structure unit tests
  - Testing for exceptions / failures
  - Use of FluentAssertions to increase test readability
- CI/CD
  - Automatic generation of pipelines using Nuke.build for
    - Azure DevOps
    - Github Actions
- Docker-compose for all dependent services when iterating locally
  - Spring Config Server
  - Eureka (service discovery)
  - RabbitMQ
  - MySQL
  - PostgreSQL
  - PHPMyAdmin (Web GUI for MySQL)
  - OmniDB (WebGUI for PostgreSQL)
  - Zipkin (distributed tracing)
  - Spring Boot Admin (actuator visualizer)


## Quickstart

Run `.\build.cmd init` or `.\build.sh init` to setup local environment.

## Build

The project is instrumented with common set of tasks implemented as targets via [Nuke.Build](https://nuke.build/). These can be invoked via entry point helper script `build.cmd` (or one of the other extensions based on terminal of choice).

`.\build.cmd <TARGET_NAME> --my-arg=<value>...`

Common targets:

- `init` - initializes git repo, sets up versioning, and restores necessary cli tools. Installs .NET SDK if it's missing
- `compile` - compile the project
- `publish` - .net publish the project, and package output into version stamped zip files into `artifacts` folder
-  `clean` - clear out `bin`,`obj`, and `artifacts` folders

Additional build targets can be easily added by editing `/build/Build.cs`

## Branching and Versioning

The code uses [GitFlow](https://www.atlassian.com/git/tutorials/comparing-workflows/gitflow-workflow) for branching and release workflow.

![](https://wac-cdn.atlassian.com/dam/jcr:8f00f1a4-ef2d-498a-a2c6-8020bb97902f/03%20Release%20branches.svg?cdnVersion=1824)

Latest active branch developers commit to is `develop`, while `main` represents current production code. Release branches are created during stabilization phase, and represent next release being reading to be shipped to production.

Versioning is derived from Git commit history using [Nerdbank.Gitversioning](https://github.com/dotnet/Nerdbank.GitVersioning). Current version can be obtained by running

````
dotnet nbgv get-version
````

When the team is ready to start preparing next release, they *cut* a release branch from the `develop` branch. Release branch carries stabilization fixes for next release cycle, while `develop` increments next major version number and starts accumulating new features.

Cutting a release branch can be done by issuing this command:

```
.\build.ps1 prepare-release
```

### Prereleases

Builds created out of different branches will have the pre-release tags applied automatically to their version numbers:

`develop`: `<semver>-alpha`

`v.X.X`: `<semver>-beta`

`main`: `<semver>`

any other: `<semver>-<short-sha>`

### Creating production release

At the end of stabilization cycle when the release is made. The following steps should be performed:

- switch to release branch (ex. v2.0)
- edit `version.json` and change the `version` property to remove the `beta` tag
- commit
- merge release branch into master

## Config server

Config server allows configuration to be loaded from a dedicated service that is backed by one or more configuration sources. The default configuration source is git, allowing configuration changes to be stored in git, but loaded without having to redeploy the app.

![Config Server](docs/images/config-server-fig1.png)

The configuration is stored inside `/config` folder in the repository. You can launch config server locally via `docker-compose` from inside `services` folder vas following:

```
docker-compose up configserver
```

## Deploy to Tanzu Application Platform

Deploy this project to Tanzu Application Platform by executing either of the following commands.

For both commands, make sure you've specified the correct Git repository. For the first command, the Git repository needs to be specified in the ./config/workload.yaml file and it exists as a parameter to the second command.

`tanzu apps workload create dotnet-accelerator -f ./kubernetes/workload.yaml`

