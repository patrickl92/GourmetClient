using System.Collections.Generic;

namespace GourmetClient.Update
{
    public record ReleaseListQueryResult(string ETagHeaderValue, bool IsWeakETag, IReadOnlyList<ReleaseDescription> Releases);
}
