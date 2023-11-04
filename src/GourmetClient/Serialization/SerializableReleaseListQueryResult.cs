using System;
using System.Linq;
using GourmetClient.Update;

namespace GourmetClient.Serialization
{
    internal class SerializableReleaseListQueryResult
    {
        public SerializableReleaseListQueryResult()
        {
            // Used for deserialization
        }

        public SerializableReleaseListQueryResult(ReleaseListQueryResult releaseListQueryResult)
        {
            releaseListQueryResult = releaseListQueryResult ?? throw new ArgumentNullException(nameof(releaseListQueryResult));

            Version = 1;
            ETagHeaderValue = releaseListQueryResult.ETagHeaderValue;
            IsWeakETag = releaseListQueryResult.IsWeakETag;
            Releases = releaseListQueryResult.Releases.Select(release => new SerializableReleaseDescription(release)).ToArray();
        }

        public int? Version { get; set; }

        public string ETagHeaderValue { get; set; }

        public bool? IsWeakETag { get; set; }

        public SerializableReleaseDescription[] Releases { get; set; }

        public ReleaseListQueryResult ToReleaseListQueryResult()
        {
            if (Version is not 1)
            {
                throw new InvalidOperationException($"Unsupported version of serialized data: {Version}");
            }

            if (string.IsNullOrEmpty(ETagHeaderValue) || !IsWeakETag.HasValue || Releases == null)
            {
                throw new InvalidOperationException("At least one property has an invalid value");
            }

            return new ReleaseListQueryResult(ETagHeaderValue, IsWeakETag.Value, Releases.Select(release => release.ToReleaseDescription()).ToList());
        }
    }
}
