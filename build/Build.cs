using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LibGit2Sharp;
using Nuke.Common;
using Nuke.Common.CI;
using Nuke.Common.CI.AzurePipelines;
using Nuke.Common.CI.GitHubActions;
using Nuke.Common.Execution;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Tools.Git;
using Nuke.Common.Tools.NerdbankGitVersioning;
using Nuke.Common.Utilities.Collections;
using static Nuke.Common.IO.FileSystemTasks;
using static Nuke.Common.Tools.DotNet.DotNetTasks;
using static Nuke.Common.Tools.NerdbankGitVersioning.NerdbankGitVersioningTasks;
using static Nuke.Common.IO.CompressionTasks;

[CheckBuildProjectConfigurations]
[ShutdownDotNetAfterServerBuild]
[GitHubActions("CI", GitHubActionsImage.Ubuntu1804, 
    InvokedTargets = new[]{nameof(Test),nameof(Publish)},
    On = new [] {GitHubActionsTrigger.Push},
    PublishArtifacts = true
    // OnPushBranchesIgnore = new[] { "main", "v*" }
    )]
[AzurePipelines(AzurePipelinesImage.Ubuntu1804, 
    InvokedTargets = new[]{nameof(Test), nameof(Publish)},
    ExcludedTargets = new[] { nameof(Clean)},
    CacheKeyFiles = new[] { "global.json", "source/**/*.csproj" })]
class Build : NukeBuild
{
    /// Support plugins are available for:
    ///   - JetBrains ReSharper        https://nuke.build/resharper
    ///   - JetBrains Rider            https://nuke.build/rider
    ///   - Microsoft VisualStudio     https://nuke.build/visualstudio
    ///   - Microsoft VSCode           https://nuke.build/vscode
    static Build()
    {
        Environment.SetEnvironmentVariable("NUKE_TELEMETRY_OPTOUT", "true");
        Environment.SetEnvironmentVariable("NO_LOGO", "true");
    }
    public static int Main () => Execute<Build>();

    [Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
    readonly Configuration Configuration = IsLocalBuild ? Configuration.Debug : Configuration.Release;
    [Parameter("Project to target")]
    readonly string TargetProject = "MyProjectGroup.DotnetAccelerator";
    [Parameter("The name of the migration to add")]
    string MigrationName;
    [Solution] readonly Solution Solution;

    AbsolutePath SourceDirectory => RootDirectory / "src";
    AbsolutePath TestsDirectory => RootDirectory / "tests";
    AbsolutePath ArtifactsDirectory => RootDirectory / "artifacts";
    string TargetFramework => "net5.0";

    [GitVersion] NerdbankGitVersioning GitVersion;

    Repository GitRepository;
    static bool IsGitInitialized => Repository.IsValid(RootDirectory);


    protected override void OnBuildInitialized()
    {
        if (IsGitInitialized)
        {
            GitRepository = new Repository(RootDirectory);
        }
    }

    Target Init => _ => _
        .DependsOn(SetupGit, RestoreTools, AddInitialMigration);

    Target Clean => _ => _
        .Before(Restore)
        .Executes(() =>
        {
            SourceDirectory.GlobDirectories("**/bin", "**/obj").ForEach(DeleteDirectory);
            TestsDirectory.GlobDirectories("**/bin", "**/obj").ForEach(DeleteDirectory);
            EnsureCleanDirectory(ArtifactsDirectory);
        });

    Target Restore => _ => _
        .Executes(() =>
        {
            DotNetRestore(s => s
                .SetProjectFile(Solution));
        });

    Target Compile => _ => _
        .DependsOn(Restore)
        .Executes(() =>
        {
            DotNetBuild(s => s
                .SetProjectFile(Solution)
                .SetConfiguration(Configuration)
                .EnableNoRestore());
        });
    
    Target Test => _ => _
        .DependsOn(Compile)
        .Executes(() =>
        {
            DotNetTest(s => s
                .SetProjectFile(Solution)
                .SetConfiguration(Configuration)
                .EnableNoRestore());
        });
    
    Target Publish => _ => _
        .OnlyWhenDynamic(() => IsGitInitialized)
        .DependsOn(Restore)
        .Produces(ArtifactsDirectory / "*.zip")
        .Executes(() =>
        {
            if (GitRepository.RetrieveStatus().IsDirty)
            {
                Logger.Warn("Git repository is dirty. Build version will change after commit is made.");
            }
            DotNetPublish(s => s
                .SetProject(Solution)
                .SetConfiguration(Configuration)
                .EnableNoRestore());

            var publishDirs = Solution.AllProjects
                .Where(x => x.Name is not "_build" and not "config" && 
                            !x.Name.EndsWith("Tests") && 
                            x.GetMSBuildProject().GetProperty("OutputType").EvaluatedValue == "Exe")
                .Select(x => (x.Name, PublishDir: x.Directory / "bin" / Configuration / TargetFramework / "publish"))
                .Where(x => DirectoryExists(x.PublishDir))
                .ToArray();

            var version = GitVersion?.NuGetPackageVersion ?? "1.0";
            var artifacts = new List<string>();
            foreach (var (name, publishDir) in publishDirs)
            {
                var zipFile = ArtifactsDirectory / $"{name}-{version}.zip";
                DeleteFile(zipFile);
                Compress(publishDir, zipFile);
                artifacts.Add(zipFile);
            }

            Logger.Block(string.Join("\n", artifacts));
        });

    Target AddInitialMigration => _ => _
        .OnlyWhenDynamic(() => MigrationsFolderExists())
        .Unlisted()
        .Executes(() =>
        {
            MigrationName ??= "Initial";
            DoAddMigration();
        });
    Target AddMigration => _ => _
        .Description("Adds database migration to the project")
        .Requires(() => MigrationName, () => TargetProject)
        .Executes(DoAddMigration);

    bool MigrationsFolderExists() => !Directory.Exists(RootDirectory / "src" / RootDirectory.GetRelativePathTo(Solution.GetProject(TargetProject).Directory) / "Migrations");

    void DoAddMigration()
    {
        var environmentVariables = new Dictionary<string, string>(Environment.GetEnvironmentVariables()
            .Cast<DictionaryEntry>()
            .Select(x => KeyValuePair.Create(x.Key.ToString(), x.Value.ToString())))
        {
            {"SPRING__CLOUD__CONFIG__ENABLED", "false"}
        };
        DotNet($"ef migrations add {MigrationName} --project src/{TargetProject}", environmentVariables: environmentVariables);
    }
    Target GetVersion => _ => _
        .Executes(() =>
        {
            Logger.Block(GitVersion.NuGetPackageVersion);
        });
    Target PrepareRelease => _ => _
        .Executes(() =>
        {
            NerdbankGitVersioningPrepareRelease(x => x.SetTag("beta"));
        });
    
    #region Init Subtasks
    Target SetupGit => _ => _
        .Unlisted()
        .OnlyWhenDynamic(() => !IsGitInitialized )
        .Executes(() =>
        {
            GitTasks.Git("init --initial-branch=main", workingDirectory: RootDirectory);
            GitRepository = new Repository(RootDirectory);
            var signature = GitRepository.Config.BuildSignature(DateTimeOffset.UtcNow);
            Commands.Stage(GitRepository, ".gitignore");
            Commands.Stage(GitRepository, "version.json");
            // commit all build scripts with execute permission to make sure they are runnable on linux based systems
            foreach (var extension in new[] {"sh", "cmd", "ps1"})
            {
                Commands.Stage(GitRepository, $"build.{extension}");
                GitTasks.Git("update-index --chmod=+x build.{extension}", workingDirectory: RootDirectory);
            }

            GitRepository.Commit("Initial", signature, signature);
            var devBranch = GitRepository.CreateBranch("develop");
            Commands.Checkout(GitRepository, devBranch);
        });

    Target RestoreTools => _ => _
        .Unlisted()
        .Executes(() => DotNetToolRestore(_ => _));
    
    #endregion
}
