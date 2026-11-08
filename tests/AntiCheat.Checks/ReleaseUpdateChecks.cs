using AntiCheat.Core;

internal static class ReleaseUpdateChecks
{
    public static int Run()
    {
        int checks = 0;
        void Check(bool condition, string name)
        {
            if (!condition) throw new Exception(name);
            checks++;
        }
        ReleaseVersion Parse(string tag)
        {
            if (!ReleaseVersion.TryParse(tag, out ReleaseVersion? parsed))
                throw new Exception($"Expected valid release tag {tag}");
            return parsed!;
        }
        ReleaseUpdateCandidate Candidate(string tag, bool draft = false, bool prerelease = false,
            bool hasDownload = true) => new()
        {
            TagName = tag, Draft = draft, Prerelease = prerelease, HasDownload = hasDownload
        };

        foreach (string tag in new[] { "0.0.0", "v0.3.0", "v2147483647.2.3", "1.2.3-0",
            "1.2.3-alpha.beta.9", "v1.2.3+build.0001", "v1.2.3-rc.1+build.0001", "1.2.3-00alpha" })
            Check(ReleaseVersion.TryParse(tag, out _), $"accept valid tag {tag}");

        foreach (string? tag in new string?[] { null, "", "v", " 1.2.3", "1.2.3 ", "V1.2.3",
            "release-1.2.3", "1.2", "1.2.3.4", "01.2.3", "1.02.3", "1.2.03", "-1.2.3",
            "2147483648.2.3", "1.2147483648.3", "1.2.2147483648", "1.2.3-", "1.2.3-01",
            "1.2.3-rc.01", "1.2.3-rc..1", "1.2.3-rc_1", "1.2.3+", "1.2.3+build..1",
            "1.2.3+build+other", "1.2.3\n", "1.2.3-α", "1.2.3+space here" })
            Check(!ReleaseVersion.TryParse(tag, out _), $"reject malformed tag {tag}");

        // The SemVer precedence sequence exercises numeric, textual, and identifier-count ordering.
        string[] ordered = { "1.0.0-alpha", "1.0.0-alpha.1", "1.0.0-alpha.beta", "1.0.0-beta",
            "1.0.0-beta.2", "1.0.0-beta.11", "1.0.0-rc.1", "1.0.0" };
        for (int i = 0; i < ordered.Length; i++)
            for (int j = 0; j < ordered.Length; j++)
                Check(Math.Sign(Parse(ordered[i]).CompareTo(Parse(ordered[j]))) == Math.Sign(i.CompareTo(j)),
                    $"SemVer ordering {ordered[i]} versus {ordered[j]}");
        Check(Parse("1.2.3").CompareTo(Parse("v1.2.3+different.42")) == 0, "metadata does not change precedence");
        Check(Parse("1.2.3-999999999999999999999999999999").CompareTo(
            Parse("1.2.3-1000000000000000000000000000000")) < 0, "huge numeric identifiers compare without overflow");
        Check(Parse("1.2.3-999999999999999999999999999999").CompareTo(
            Parse("1.2.3-999999999999999999999999999998")) > 0, "equal length huge numeric identifiers compare correctly");
        Check(Parse("2.0.0").CompareTo(Parse("1.999.999")) > 0, "major precedes minor");
        Check(Parse("1.3.0").CompareTo(Parse("1.2.999")) > 0, "minor precedes patch");

        ReleaseVersion installed = Parse("v0.3.0");
        ReleaseUpdateCandidate latest = Candidate("v0.5.0");
        Check(ReferenceEquals(ReleaseUpdateSelector.PickNewest(installed, new[]
        {
            Candidate("v0.2.0"), Candidate("v0.3.0"), Candidate("v0.4.0"), latest
        }, false), latest), "choose newest release without relying on API order");
        Check(ReleaseUpdateSelector.PickNewest(installed, new[]
        {
            Candidate("v0.3.0+rebuilt"), Candidate("v0.2.0"), Candidate("v0.2.0-steam.1")
        }, true) == null, "older storefront tags and equal build metadata do not trigger update");
        Check(ReleaseUpdateSelector.PickNewest(installed, new[]
        {
            Candidate("v0.4.0", draft: true), Candidate("v0.5.0", hasDownload: false),
            Candidate("release-v0.6.0"), Candidate("v0.7.0", prerelease: true), Candidate("v0.8.0-beta.1")
        }, false) == null, "filter drafts malformed releases missing assets and prereleases");
        ReleaseUpdateCandidate preview = Candidate("v0.4.0-beta.1", prerelease: true);
        Check(ReferenceEquals(ReleaseUpdateSelector.PickNewest(installed, new[] { preview }, true), preview),
            "opt-in permits tagged and GitHub marked prereleases");
        Check(ReleaseUpdateSelector.PickNewest(Parse("v0.4.0"), new[] { preview }, true) == null,
            "prerelease of installed stable is older");
        ReleaseUpdateCandidate stable = Candidate("v0.4.0");
        Check(ReferenceEquals(ReleaseUpdateSelector.PickNewest(Parse("v0.4.0-beta.2"), new[] { stable }, false), stable),
            "stable updates prerelease installation");
        Check(ReleaseUpdateSelector.PickNewest(installed, Array.Empty<ReleaseUpdateCandidate>(), true) == null,
            "empty feed has no update");
        return checks;
    }
}
