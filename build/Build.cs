class BuildProject : NukeBuild
{
    [Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
    readonly Configuration Configuration =
        IsLocalBuild ? Configuration.Debug : Configuration.Release;

    [Parameter(List = false)] readonly bool DotnetRunningInContainer;
    [GlobalJson] readonly GlobalJson GlobalJson;

    [Parameter("Don't open the coverage report")]
    readonly bool NoBrowse;

    [Solution] readonly Solution Solution;
    [Parameter] readonly string TestResultFile = "test_result.xml";

    AbsolutePath CoverageFiles => RootDirectory / "**" / "coverage.cobertura.xml";
    AbsolutePath TestReportDirectory => RootDirectory / "TestReport";

    Target Clean => _ => _
        .Description("Clean project directories")
        .OnlyWhenStatic(() => BuildProjectDirectory is not null)
        .Executes(() => RootDirectory
            .GlobDirectories("**/bin", "**/obj", "**/TestResults")
            .Where(x => !x.ToString().StartsWith(BuildProjectDirectory ?? string.Empty))
            .ForEach(d => d.CreateOrCleanDirectory()));

    Target Restore => _ => _
        .Description("Run dotnet restore in every project")
        .DependsOn(Clean)
        .Executes(() => DotNetRestore(s => s
            .SetProjectFile(Solution)));

    Target Build => _ => _
        .Description("Builds Solution")
        .DependsOn(Restore)
        .Executes(() =>
            DotNetBuild(s => s
                .SetProjectFile(Solution)
                .SetConfiguration(Configuration)
                .EnableNoLogo()
                .EnableNoRestore()));

    Target Test => _ => _
        .Description("Run all tests")
        .DependsOn(Build)
        .Executes(() =>
            DotNetTest(s => s
                .EnableNoBuild()
                .EnableNoRestore()
                .SetConfiguration(Configuration)
                .SetProjectFile(Solution)));

    Target TestCoverage => _ => _
        .Description("Run tests with coverage")
        .DependsOn(Build)
        .Executes(() => DotNetTest(s => s
            .SetVerbosity(DotNetVerbosity.Minimal)
            .EnableNoBuild()
            .EnableNoRestore()
            .SetConfiguration(Configuration)
            .SetProjectFile(Solution)
            .SetLoggers($"trx;LogFileName={TestResultFile}")
            .SetSettingsFile(RootDirectory / "coverlet.runsettings")
        ))
        .Executes(() =>
        {
            ReportGenerator(r => r.LocalTool("reportgenerator")
                .SetReports(CoverageFiles)
                .SetTargetDirectory(TestReportDirectory)
                .SetReportTypes(ReportTypes.TextSummary));
            (TestReportDirectory / "Summary.txt").ReadAllLines().ForEach(l => Console.WriteLine(l));
        });

    const string localstackContainerName = "sub-localstack";

    Target Localstack => _ => _
        .Description("Starts the localstack container in docker")
        .OnlyWhenStatic(() => !DotnetRunningInContainer)
        .ProceedAfterFailure()
        .Executes(() => DockerRun(c => c
            .SetName(localstackContainerName)
            .SetImage("localstack/localstack:1.1.0")
            .SetPublish("4566:4566")
            .EnableRm()))
        .Triggers(StopLocalstack);

    Target StopLocalstack => _ => _
        .Description("Stops the localstack container in docker")
        .AssuredAfterFailure()
        .Executes(() => DockerStop(s => s.SetContainers(localstackContainerName)));

    Target Lint => _ => _
        .Description("Check for codebase formatting and analysers")
        .DependsOn(Build)
        .Executes(() =>
            DotNet($"format -v normal --no-restore --verify-no-changes \"{Solution.Path}\""));

    Target Format => _ => _
        .Description("Try fix codebase formatting and analysers")
        .DependsOn(Build)
        .Executes(() => DotNet($"format -v normal --no-restore \"{Solution.Path}\""));

    Target Report => _ => _
        .Description("Run tests and generate coverage report")
        .DependsOn(TestCoverage)
        .Triggers(GenerateReport, BrowseReport);

    Target GenerateReport => _ => _
        .Description("Generate test coverage report")
        .After(TestCoverage)
        .OnlyWhenDynamic(() => CoverageFiles.GlobFiles().Any())
        .Executes(() =>
            ReportGenerator(r => r
                .LocalTool("reportgenerator")
                .SetReports(CoverageFiles)
                .SetTargetDirectory(TestReportDirectory)
                .SetReportTypes(
                    ReportTypes.Html,
                    ReportTypes.Clover,
                    ReportTypes.Cobertura,
                    ReportTypes.MarkdownSummary
                )));

    Target BrowseReport => _ => _
        .Description("Open coverage report")
        .OnlyWhenStatic(() => !NoBrowse && !DotnetRunningInContainer)
        .After(GenerateReport, GenerateBadges)
        .Unlisted()
        .Executes(() =>
        {
            var path = TestReportDirectory / "index.htm";
            Assert.FileExists(path);
            try
            {
                BrowseHtml(path.ToString().DoubleQuoteIfNeeded());
            }
            catch (Exception e)
            {
                if (!IsWin) // Windows explorer always return 1
                    Log.Error(e, "Unable to open report");
            }
        });

    Target GenerateBadges => _ => _
        .Description("Generate cool badges for readme")
        .After(TestCoverage)
        .Requires(() => CoverageFiles.GlobFiles().Any())
        .Executes(() =>
        {
            var output = RootDirectory / "Badges";
            output.CreateOrCleanDirectory();
            Badges.ForCoverage(output, CoverageFiles);
            Badges.ForDotNetVersion(output, GlobalJson);
            Badges.ForTests(output, TestResultFile);
        });

    public static int Main() => Execute<BuildProject>();

    protected override void OnBuildInitialized()
    {
        DockerLogger = (_, msg) => Log.Information(msg);
        DotNetToolRestore(c => c.DisableProcessLogOutput());
    }
}