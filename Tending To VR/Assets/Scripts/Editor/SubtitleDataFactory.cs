#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// Editor utility that creates all SubtitleData assets for the poem in one click.
/// Run via: Assets menu → TendingTo → Create All Subtitle Data Assets
///
/// Assets are written to Assets/SubtitleData/.
/// After creation, set the clipLengthForTiming on each asset and run
/// "Fill Even Timing" (right-click in Inspector) as a starting point,
/// then tune individual lineStartTimes values against the narration audio.
/// </summary>
public static class SubtitleDataFactory
{
    private const string OutputFolder = "Assets/SubtitleData";

    [MenuItem("Assets/TendingTo/Create All Subtitle Data Assets")]
    public static void CreateAllSubtitleAssets()
    {
        if (!AssetDatabase.IsValidFolder(OutputFolder))
        {
            AssetDatabase.CreateFolder("Assets", "SubtitleData");
            Debug.Log($"[SubtitleDataFactory] Created folder: {OutputFolder}");
        }

        foreach (var entry in SubtitleEntries)
        {
            string path = $"{OutputFolder}/{entry.fileName}.asset";

            if (AssetDatabase.LoadAssetAtPath<SubtitleData>(path) != null)
            {
                Debug.Log($"[SubtitleDataFactory] Skipping {entry.fileName} — asset already exists.");
                continue;
            }

            SubtitleData asset = ScriptableObject.CreateInstance<SubtitleData>();
            asset.stage           = entry.stage;
            asset.lines           = entry.lines;
            asset.lineStartTimes  = entry.lineStartTimes;
            asset.textColor       = entry.textColor;

            AssetDatabase.CreateAsset(asset, path);
            Debug.Log($"[SubtitleDataFactory] Created: {path}");
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog(
            "Subtitle Data Created",
            $"All SubtitleData assets written to {OutputFolder}.\n\n" +
            "Next steps:\n" +
            "1. Set Clip Length For Timing on each asset to match its audio clip length.\n" +
            "2. Right-click each asset → Fill Even Timing as a starting point.\n" +
            "3. Fine-tune lineStartTimes values against the narration audio.",
            "OK");
    }

    // -------------------------------------------------------------------------
    // Poem Data
    // -------------------------------------------------------------------------

    private static readonly Color ActionColor  = Color.white;
    private static readonly Color ReverseColor = new Color(0.75f, 0.88f, 1f); // pale blue

    private static readonly SubtitleEntry[] SubtitleEntries =
    {
        new SubtitleEntry
        {
            fileName       = "SD_Sub_CutTheGrass",
            stage          = Stage.CutTheGrass,
            textColor      = ActionColor,
            lines          = new[]
            {
                "The grass is high, unkempt and unruly,",
                "With daisies and clover flourishing newly.",
                "Here comes the mower with a steady low hum",
                "Stripe by stripe until it levels the lawn.",
            },
            lineStartTimes = new[] { 0f, 5f, 10f, 15f },
        },
        new SubtitleEntry
        {
            fileName       = "SD_Sub_ClearThePath",
            stage          = Stage.ClearThePath,
            textColor      = ActionColor,
            lines          = new[]
            {
                "Green shoots push through the gaps, a stubborn display.",
                "Quick! A mist from a bottle to clear them away.",
                "For order is found when the edges are clean",
                "And the paving a desert where life once had been",
            },
            lineStartTimes = new[] { 0f, 5f, 10f, 15f },
        },
        new SubtitleEntry
        {
            fileName       = "SD_Sub_SaveTheHostas",
            stage          = Stage.SaveTheHostas,
            textColor      = ActionColor,
            lines          = new[]
            {
                "Oh no, the hostas are ragged, their edges in tatters,",
                "No problem, a scattering of blue to settle the matters",
                "The leaves are now protected from silvery trails",
                "And a supper had here, quickly assail.",
            },
            lineStartTimes = new[] { 0f, 5f, 10f, 15f },
        },
        new SubtitleEntry
        {
            fileName       = "SD_Sub_FeedTheFlowers",
            stage          = Stage.FeedTheFlowers,
            textColor      = ActionColor,
            lines          = new[]
            {
                "The soil was thirsty, the rose-head was pale,",
                "Let\u2019s give it a boost before it\u2019ll fail.",
                "A handful of crystals, a drink for the root,",
                "A quieted bed with petalled thick fruit",
            },
            lineStartTimes = new[] { 0f, 5f, 10f, 15f },
        },
        new SubtitleEntry
        {
            fileName       = "SD_Sub_DoTheWindowBox",
            stage          = Stage.DoTheWindowBox,
            textColor      = ActionColor,
            lines          = new[]
            {
                "The plants came in plastic, all labelled with care,",
                "Pollinator friendly in bold, right there.",
                "Planted with compost deep, rich and dark",
                "To leave on the windowsill a colourful mark",
            },
            lineStartTimes = new[] { 0f, 5f, 10f, 15f },
        },
        new SubtitleEntry
        {
            fileName       = "SD_Sub_BrokenWindowBox",
            stage          = Stage.BrokenWindowBox,
            textColor      = ReverseColor,
            lines          = new[]
            {
                "Yet when these bee friendly flowers were tested for toxins inside,",
                "turns out 93% contained at least one pesticide.",
                "And the peat in your compost bag was a carbon-filled store",
                "Now a hollowed out wasteland, leaking and living no more.",
            },
            lineStartTimes = new[] { 0f, 5f, 10f, 15f },
        },
        new SubtitleEntry
        {
            fileName       = "SD_Sub_BrokenFlowers",
            stage          = Stage.BrokenFlowers,
            textColor      = ReverseColor,
            lines          = new[]
            {
                "So you fed the flower, but did you know you poisoned the soil?",
                "With synthetic salts that make the microbes recoil.",
                "The mycelium shrivels, the earth becomes dust,",
                "In chemical \u201ctonics\u201d we have misplaced our trust.",
            },
            lineStartTimes = new[] { 0f, 5f, 10f, 15f },
        },
        new SubtitleEntry
        {
            fileName       = "SD_Sub_BrokenHostas",
            stage          = Stage.BrokenHostas,
            textColor      = ReverseColor,
            lines          = new[]
            {
                "And the thrush found the pellets before finding the slug,",
                "And the hedgehog that followed lies still in the mud.",
                "Each link in the chain took the metaldehyde in",
                "The meal that was meant for the pest did them in.",
            },
            lineStartTimes = new[] { 0f, 5f, 10f, 15f },
        },
        new SubtitleEntry
        {
            fileName       = "SD_Sub_BrokenPath",
            stage          = Stage.BrokenPath,
            textColor      = ReverseColor,
            lines          = new[]
            {
                "How neat is this paving! So shrivelled and brown.",
                "Yet the label didn\u2019t say how it seeps underground.",
                "Its leached through the clay, into river and rain",
                "And has made its way silently into your own veins",
            },
            lineStartTimes = new[] { 0f, 5f, 10f, 15f },
        },
        new SubtitleEntry
        {
            fileName       = "SD_Sub_BrokenGrass",
            stage          = Stage.BrokenGrass,
            textColor      = ReverseColor,
            lines          = new[]
            {
                "The daisies were nectar, the clover was a feast,",
                "A larder laid open for all insects and beasts.",
                "Now the stripes you laid down are so even and green",
                "You made this place a desert and have called it \u2018clean\u2019.",
            },
            lineStartTimes = new[] { 0f, 5f, 10f, 15f },
        },
    };

    private struct SubtitleEntry
    {
        public string   fileName;
        public Stage    stage;
        public string[] lines;
        public float[]  lineStartTimes;
        public Color    textColor;
    }
}
#endif
