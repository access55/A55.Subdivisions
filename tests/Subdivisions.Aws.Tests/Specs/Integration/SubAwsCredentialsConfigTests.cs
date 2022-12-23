using Amazon.Runtime;
using AutoBogus;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Subdivisions.Aws.Tests.TestUtils.Fixtures;
using Subdivisions.Models;

namespace Subdivisions.Aws.Tests.Specs.Integration;

public class SubAwsCredentialsConfigConfigurationTests : ServicesFixture
{
    readonly SubAwsCredentialsConfig randomConfig = AutoFaker.Generate<SubAwsCredentialsConfig>();

    protected override void ConfigureServices(IServiceCollection services)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["SUBDIVISIONS_AWS_ACCESS_KEY_ID"] = randomConfig.SubdivisionsAwsAccessKey,
                ["SUBDIVISIONS_AWS_SECRET_ACCESS_KEY"] = randomConfig.SubdivisionsAwsSecretKey,
            })
            .Build();

        services
            .AddSubdivisionsServices(c => c.Source = "app")
            .AddSingleton<IConfiguration>(_ => configuration!);
    }

    [Test]
    public void ShouldUseIConfigurationFromContainer()
    {
        var subConfig = GetService<IOptions<SubAwsCredentialsConfig>>().Value;
        subConfig.Should().BeEquivalentTo(randomConfig);
    }

    [Test]
    public void ShouldReturnBasicCredentials()
    {
        var cred = GetService<SubAwsCredentialWrapper>().Credentials;
        cred.Should().BeOfType<BasicAWSCredentials>()
            .Which.GetCredentials()
            .Should().BeEquivalentTo(new
            {
                AccessKey = randomConfig.SubdivisionsAwsAccessKey,
                SecretKey = randomConfig.SubdivisionsAwsSecretKey,
            });
    }
}

public class SubAwsCredentialsConfigEnvironmentTests : ServicesFixture
{
    readonly SubAwsCredentialsConfig randomConfig = AutoFaker.Generate<SubAwsCredentialsConfig>();

    protected override void ConfigureServices(IServiceCollection services)
    {
        Environment.SetEnvironmentVariable("SUBDIVISIONS_AWS_ACCESS_KEY_ID",
            randomConfig.SubdivisionsAwsAccessKey,
            EnvironmentVariableTarget.Process);
        Environment.SetEnvironmentVariable("SUBDIVISIONS_AWS_SECRET_ACCESS_KEY",
            randomConfig.SubdivisionsAwsSecretKey, EnvironmentVariableTarget.Process);

        services
            .AddSubdivisionsServices();
    }

    [TearDown]
    protected void TearDown()
    {
        Environment.SetEnvironmentVariable("SUBDIVISIONS_AWS_ACCESS_KEY_ID", null,
            EnvironmentVariableTarget.Process);
        Environment.SetEnvironmentVariable("SUBDIVISIONS_AWS_SECRET_ACCESS_KEY",
            null, EnvironmentVariableTarget.Process);
    }

    [Test]
    public void ShouldUseIConfigurationFromContainer()
    {
        var subConfig = GetService<IOptions<SubAwsCredentialsConfig>>().Value;
        subConfig.Should().BeEquivalentTo(randomConfig);
    }

    [Test]
    public void ShouldReturnBasicCredentials()
    {
        var cred = GetService<SubAwsCredentialWrapper>().Credentials;
        cred.Should().BeOfType<BasicAWSCredentials>()
            .Which.GetCredentials()
            .Should().BeEquivalentTo(new
            {
                AccessKey = randomConfig.SubdivisionsAwsAccessKey,
                SecretKey = randomConfig.SubdivisionsAwsSecretKey,
            });
    }
}

public class SubAwsCredentialsConfigIgnoreTests : ServicesFixture
{
    protected override void ConfigureServices(IServiceCollection services)
    {
        var configuration = new ConfigurationBuilder()
            .Build();

        services
            .AddSingleton<IConfiguration>(_ => configuration!)
            .AddSubdivisionsServices();
    }

    [Test]
    public void ShouldUseIConfigurationFromContainer()
    {
        var subConfig = GetService<IOptions<SubAwsCredentialsConfig>>().Value;
        subConfig.Should().BeEquivalentTo(new SubAwsCredentialsConfig
        {
            SubdivisionsAwsAccessKey = null,
            SubdivisionsAwsSecretKey = null,
        });
    }

    [Test]
    public void ShouldReturnBasicCredentials()
    {
        var cred = GetService<SubAwsCredentialWrapper>().Credentials;
        cred.Should().NotBeOfType<BasicAWSCredentials>();
    }
}
