using System;
using System.Linq;
using LibGit2Sharp;
using Nuke.Common;
using Nuke.Common.CI;
using Nuke.Common.CI.AzurePipelines;
using Nuke.Common.Execution;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Tools.Git;
using Nuke.Common.Tools.NerdbankGitVersioning;
using Nuke.Common.Utilities.Collections;
using static Nuke.Common.IO.FileSystemTasks;
using static Nuke.Common.Tools.DotNet.DotNetTasks;
using static Nuke.Common.IO.CompressionTasks;

[CheckBuildProjectConfigurations]
[ShutdownDotNetAfterServerBuild]
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
    public static int Main () => Execute<Build>(x => x.Compile);

    [Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
    readonly Configuration Configuration = IsLocalBuild ? Configuration.Debug : Configuration.Release;

    [Solution] readonly Solution Solution;

    AbsolutePath SourceDirectory => RootDirectory / "src";
    AbsolutePath TestsDirectory => RootDirectory / "tests";
    AbsolutePath ArtifactsDirectory => RootDirectory / "artifacts";
    string TargetFramework => "net5.0";

    [NerdbankGitVersioning] NerdbankGitVersioning GitVersion;

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
        .DependsOn(SetupGit, RestoreTools);

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

            var publishDirs = Solution.Projects
                .Select(x => (x.Name, PublishDir: x.Directory / "bin" / Configuration / TargetFramework / "publish"))
                .Where(x => DirectoryExists(x.PublishDir))
                .ToArray();

            var version = GitVersion?.NuGetPackageVersion ?? "1.0";
            foreach (var (name, publishDir) in publishDirs)
            {
                var zipFile = ArtifactsDirectory / $"{name}-{version}.zip";
                DeleteFile(zipFile);
                Compress(publishDir, zipFile);
            }
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
            GitRepository.Commit("Initial", signature, signature);
            var devBranch = GitRepository.CreateBranch("develop");
            Commands.Checkout(GitRepository, devBranch);
        });

    Target RestoreTools => _ => _
        .Unlisted()
        .Executes(() => DotNetToolRestore(_ => _));
    
    #endregion
}
