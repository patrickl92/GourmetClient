using Semver;

namespace GourmetClient.Update
{
    public record ReleaseDescription(
        SemVersion Version,
        string UpdatePackageDownloadUrl,
        long UpdatePackageSize,
        string ChecksumDownloadUrl,
        long ChecksumSize);
}
