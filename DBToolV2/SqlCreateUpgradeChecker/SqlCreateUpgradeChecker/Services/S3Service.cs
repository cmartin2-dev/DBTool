using System.IO;
using Amazon.Runtime;
using Amazon.Runtime.CredentialManagement;
using Amazon.S3;
using Amazon.S3.Transfer;

namespace SqlCreateUpgradeChecker.Services;

public static class S3Service
{
    /// <summary>
    /// Reads AWS profile names from ~/.aws/credentials.
    /// </summary>
    public static List<string> GetAwsProfiles()
    {
        var chain = new CredentialProfileStoreChain();
        var profiles = new List<string>();

        var credFile = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".aws", "credentials");

        if (!File.Exists(credFile))
            return profiles;

        var store = new SharedCredentialsFile(credFile);
        profiles.AddRange(store.ListProfiles().Select(p => p.Name));

        return profiles;
    }

    /// <summary>
    /// Uploads a file to S3.
    /// </summary>
    public static async Task UploadFileAsync(string profileName, string region, string bucket, string s3Key, string localFilePath)
    {
        var chain = new CredentialProfileStoreChain();
        if (!chain.TryGetAWSCredentials(profileName, out var credentials))
            throw new Exception($"Could not load AWS credentials for profile '{profileName}'.");

        var config = new AmazonS3Config
        {
            RegionEndpoint = Amazon.RegionEndpoint.GetBySystemName(region)
        };

        using var client = new AmazonS3Client(credentials, config);
        var transferUtility = new TransferUtility(client);

        await transferUtility.UploadAsync(localFilePath, bucket, s3Key);
    }
}
