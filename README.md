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
dotnet nbgv prepare-release
```

This will create 2 commits - do not attempt to squash them. These are necessary for the git versioning to work properly.

