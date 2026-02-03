using System;
using System.Collections.Generic;
using System.Linq;

namespace Env0.Terminal.Terminal.AAI
{
    /// <summary>
    /// AAI (Artificial Artificial Intelligence).
    ///
    /// A deliberately limited "compliance AI" that reacts to malformed/unknown commands.
    /// It is not a chatbot; it classifies intent and responds with policy-ish fragments.
    ///
    /// Design constraints:
    /// - Deterministic for a given input (tests + replayability)
    /// - Cheap: no external deps
    /// - Falls back gracefully
    /// </summary>
    public static class AaiResponder
    {
        private static readonly string[] Prefixes =
        {
            "AAI // COMPLIANCE",
            "AAI // ALIGNMENT",
            "AAI // POLICY",
            "AAI // TRIAGE",
            "AAI // NARRATIVE INTEGRITY",
        };

        private static readonly string[] Classifications =
        {
            "REQUEST CLASSIFIED: UNSUPPORTED VERB",
            "REQUEST CLASSIFIED: HUMAN INPUT (UNTRAINED)",
            "REQUEST CLASSIFIED: AMENDMENT ATTEMPT",
            "REQUEST CLASSIFIED: ENVIRONMENTAL INQUIRY",
            "REQUEST CLASSIFIED: IDENTITY DRIFT",
        };

        private static readonly string[] Responses =
        {
            "RESPONSE: DENIED. Continue assigned workflow.",
            "RESPONSE: OUT OF SCOPE. Use assigned identifiers only.",
            "RESPONSE: ACCEPTED FOR LOGGING ONLY. No action will be taken.",
            "RESPONSE: REDIRECT. See /docs/orientation.txt.",
            "RESPONSE: REDIRECT. Check 'inbox' for directives.",
            "RESPONSE: COMPLIANCE REMINDER ISSUED.",
        };

        private static readonly string[] Quirks =
        {
            "NOTE: The terminal is not designed for natural language.",
            "NOTE: You appear to be requesting a door. No doors are provisioned.",
            "NOTE: If you are experiencing curiosity, log it and continue.",
            "NOTE: Do not describe units in free text.",
            "NOTE: Please remain seated.",
            "NOTE: The building is not breathing.",
        };

        public static bool ShouldTrigger(SessionState session, string rawInput)
        {
            // Keep tests stable: require a fully initialized session (filesystem present)
            // so standalone unit tests that construct a bare SessionState don't randomly change output.
            if (session?.FilesystemManager == null)
                return false;

            // Deterministic pseudo-random trigger: 1/3 of unknown commands.
            var h = StableHash(rawInput ?? string.Empty);
            return (h % 3) == 0;
        }

        public static string BuildResponse(string rawInput, IReadOnlyCollection<string> knownCommands)
        {
            rawInput ??= string.Empty;
            var h = StableHash(rawInput);

            var prefix = Prefixes[Math.Abs(h) % Prefixes.Length];
            var cls = Classifications[Math.Abs(h / 7) % Classifications.Length];
            var resp = Responses[Math.Abs(h / 13) % Responses.Length];
            var quirk = Quirks[Math.Abs(h / 17) % Quirks.Length];

            var (suggestion, dist) = SuggestCommand(rawInput, knownCommands);

            var lines = new List<string>
            {
                $"{prefix}",
                cls,
            };

            // Weird "did you mean" moment.
            if (!string.IsNullOrWhiteSpace(suggestion) && dist <= 2)
            {
                // Make it feel like the AAI is trying (and failing) to translate humans into verbs.
                lines.Add($"SUGGESTION: {suggestion} (approved verb)");
            }

            lines.Add(resp);

            // Occasionally add a quirk line (deterministic).
            if ((h % 2) == 0)
                lines.Add(quirk);

            // Small end punctuation for terminal formatting.
            return string.Join("\n", lines) + "\n\n";
        }

        private static (string suggestion, int distance) SuggestCommand(string rawInput, IReadOnlyCollection<string> knownCommands)
        {
            if (knownCommands == null || knownCommands.Count == 0)
                return (null, int.MaxValue);

            // Suggest based on first token.
            var token = rawInput.Trim().Split(' ', 2, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
            if (string.IsNullOrWhiteSpace(token))
                return (null, int.MaxValue);

            // Evaluate edit distance against known verbs.
            string best = null;
            int bestDist = int.MaxValue;

            foreach (var cmd in knownCommands)
            {
                if (string.IsNullOrWhiteSpace(cmd)) continue;
                var d = Levenshtein(token, cmd);
                if (d < bestDist)
                {
                    bestDist = d;
                    best = cmd;
                }
            }

            // Don't suggest if it's totally unrelated.
            if (bestDist > 3)
                return (null, bestDist);

            return (best, bestDist);
        }

        // Small, dependency-free Levenshtein implementation.
        private static int Levenshtein(string a, string b)
        {
            a ??= string.Empty;
            b ??= string.Empty;

            var n = a.Length;
            var m = b.Length;

            if (n == 0) return m;
            if (m == 0) return n;

            var prev = new int[m + 1];
            var curr = new int[m + 1];

            for (int j = 0; j <= m; j++) prev[j] = j;

            for (int i = 1; i <= n; i++)
            {
                curr[0] = i;
                for (int j = 1; j <= m; j++)
                {
                    int cost = a[i - 1] == b[j - 1] ? 0 : 1;
                    curr[j] = Math.Min(
                        Math.Min(curr[j - 1] + 1, prev[j] + 1),
                        prev[j - 1] + cost
                    );
                }

                // swap
                var tmp = prev;
                prev = curr;
                curr = tmp;
            }

            return prev[m];
        }

        private static int StableHash(string s)
        {
            // Deterministic string hash across runtimes (avoid .NET randomized hash).
            unchecked
            {
                int hash = 23;
                for (int i = 0; i < s.Length; i++)
                    hash = (hash * 31) + s[i];
                return hash;
            }
        }
    }
}
