using Amazon.CDK;
using Amazon.CDK.AWS.ElasticBeanstalk;
using Amazon.CDK.AWS.IAM;
using Amazon.CDK.AWS.S3.Assets;
using Constructs;
using System.IO;

namespace Cdk
{
    public class LightningTalkStack : Stack
    {
        internal LightningTalkStack(Construct scope, string id, IStackProps props = null) : base(scope, id, props)
        {
            var application = new CfnApplication(this, "Application", new CfnApplicationProps
            {
                ApplicationName = "LightningTalkApplication"
            });

            var asset = new Asset(this, "Asset", new AssetProps
            {
                Path = Path.Combine(Directory.GetCurrentDirectory(), "publish")
            });

            var applicationVersion = new CfnApplicationVersion(this, "Version", new CfnApplicationVersionProps
            {
                ApplicationName = application.ApplicationName,
                SourceBundle = new CfnApplicationVersion.SourceBundleProperty
                {
                    S3Bucket = asset.S3BucketName,
                    S3Key = asset.S3ObjectKey
                }
            });
            applicationVersion.AddDependency(application);

            var role = new Role(this, "Role", new RoleProps
            {
                AssumedBy = new ServicePrincipal("ec2.amazonaws.com"),
                ManagedPolicies = new[]
                {
                    ManagedPolicy.FromAwsManagedPolicyName("AmazonS3ReadOnlyAccess")
                },
                RoleName = "LightningTalkRole"
            });

            var instanceProfile = new CfnInstanceProfile(this, "InstanceProfile", new CfnInstanceProfileProps
            {
                InstanceProfileName = "LightningTalkInstanceProfile",
                Roles = new[] { role.RoleName }
            });

            var environment = new CfnEnvironment(this, "Environment", new CfnEnvironmentProps
            {
                ApplicationName = application.ApplicationName,
                EnvironmentName = "LightningTalkEnvironment",
                OptionSettings = new []
                {
                    new CfnEnvironment.OptionSettingProperty
                    {
                        Namespace = "aws:autoscaling:launchconfiguration",
                        OptionName = "IamInstanceProfile",
                        Value = instanceProfile.Ref
                    }
                },
                SolutionStackName = "64bit Amazon Linux 2 v2.5.3 running .NET Core",
                VersionLabel = applicationVersion.Ref
            });
            environment.AddDependency(instanceProfile);
            environment.AddDependency(applicationVersion);
        }
    }
}
