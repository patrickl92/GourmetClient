using System;
using GourmetClient.Update;
using Semver;

namespace GourmetClient.Serialization
{
    internal class SerializableReleaseDescription
    {
        public SerializableReleaseDescription()
        {
            // Used for deserialization
        }

        public SerializableReleaseDescription(ReleaseDescription releaseDescription)
        {
            releaseDescription = releaseDescription ?? throw new ArgumentNullException(nameof(releaseDescription));

            ReleaseVersion = releaseDescription.Version.ToString();
            UpdatePackageDownloadUrl = releaseDescription.UpdatePackageDownloadUrl;
            UpdatePackageSize = releaseDescription.UpdatePackageSize;
            ChecksumDownloadUrl = releaseDescription.ChecksumDownloadUrl;
            ChecksumSize = releaseDescription.ChecksumSize;
        }

        public string ReleaseVersion { get; set; }

        public string UpdatePackageDownloadUrl { get; set; }

        public long? UpdatePackageSize { get; set; }

        public string ChecksumDownloadUrl { get; set; }

        public long? ChecksumSize { get; set; }

        public ReleaseDescription ToReleaseDescription()
        {
            if (string.IsNullOrEmpty(ReleaseVersion)
                || string.IsNullOrEmpty(UpdatePackageDownloadUrl)
                || !UpdatePackageSize.HasValue
                || string.IsNullOrEmpty(ChecksumDownloadUrl)
                || !ChecksumSize.HasValue)
            {
                throw new InvalidOperationException("At least one property has an invalid value");
            }

            return new ReleaseDescription(SemVersion.Parse(ReleaseVersion, SemVersionStyles.Strict), UpdatePackageDownloadUrl, UpdatePackageSize.Value, ChecksumDownloadUrl, ChecksumSize.Value);
        }
    }
}
