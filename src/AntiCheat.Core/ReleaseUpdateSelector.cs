using System;
using System.Collections.Generic;

namespace AntiCheat.Core;

/// <summary>A release after the caller has checked for an asset matching the installed edition.</summary>
public sealed class ReleaseUpdateCandidate
{
    public string TagName { get; set; } = string.Empty;
    public bool Draft { get; set; }
    public bool Prerelease { get; set; }
    public bool HasDownload { get; set; }
}

public static class ReleaseUpdateSelector
{
    public static ReleaseUpdateCandidate? PickNewest(ReleaseVersion installed,
        IEnumerable<ReleaseUpdateCandidate> candidates, bool includePrereleases)
    {
        if (installed == null) throw new ArgumentNullException(nameof(installed));
        if (candidates == null) throw new ArgumentNullException(nameof(candidates));

        ReleaseUpdateCandidate? newest = null;
        ReleaseVersion newestVersion = installed;
        foreach (ReleaseUpdateCandidate candidate in candidates)
        {
            if (candidate == null || candidate.Draft || !candidate.HasDownload ||
                !ReleaseVersion.TryParse(candidate.TagName, out ReleaseVersion? version))
                continue;
            if (!includePrereleases && (candidate.Prerelease || version!.IsPrerelease))
                continue;
            if (version!.CompareTo(newestVersion) <= 0) continue;
            newestVersion = version;
            newest = candidate;
        }
        return newest;
    }
}
