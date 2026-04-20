using UnityEngine;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Data structures for deserializing credits.json
/// </summary>
[System.Serializable]
public class CreditEntry
{
    public string name;
    public string artist;
    public string source;
}

[System.Serializable]
public class CreditSection
{
    public string title;
    public CreditEntry[] entries;
}

[System.Serializable]
public class CreditsData
{
    public CreditSection[] sections;
}

/// <summary>
/// Manages credit pages in the CreditsScene with auto-advance.
/// 
/// Loads credits from credits.json, organizes them into pages,
/// and automatically advances to the next page at set intervals.
/// </summary>
public class CreditsPageController : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI creditsDisplayText;
    [SerializeField] private TextMeshProUGUI pageCounterText;
    [SerializeField] private CanvasGroup fadeCanvasGroup;
    
    [SerializeField] private int linesPerPage = 18;  // visual rendered lines per page
    [SerializeField] private float secondsPerPage = 8f;      // How long to display each page
    [SerializeField] private float autoFadeDelay = 2f;       // Pause on final page before fading
    [SerializeField] private float fadeDuration = 2f;        // Fade-out duration

    private List<string> _pages = new List<string>();
    private int _currentPageIndex = 0;
    private bool _isAutoFadingAtEnd = false;
    private float _pageTimer = 0f;

    private void Start()
    {
        // Load and parse credits.json
        LoadCreditsFromJSON();

        // Display the first page
        if (_pages.Count > 0)
        {
            DisplayPage(0);
            _pageTimer = 0f;
        }
        else
        {
            Debug.LogError("[CreditsPageController] No pages were created. Check if credits.json was loaded correctly.");
        }
    }

    private void Update()
    {
        if (_pages.Count == 0 || _isAutoFadingAtEnd)
            return;

        // Increment timer
        _pageTimer += Time.deltaTime;

        // Check if it's time to advance
        if (_pageTimer >= secondsPerPage)
        {
            AdvancePageOrFinish();
        }
    }

    /// <summary>
    /// Loads credits.json and organizes entries into pages.
    /// </summary>
    private void LoadCreditsFromJSON()
    {
        // Load the JSON file from Resources or StreamingAssets
        TextAsset jsonFile = Resources.Load<TextAsset>("credits");
        if (jsonFile == null)
        {
            Debug.LogError("[CreditsPageController] Could not load credits.json from Resources/credits.json");
            return;
        }

        // Parse JSON
        CreditsData creditsData = JsonUtility.FromJson<CreditsData>(jsonFile.text);
        if (creditsData == null || creditsData.sections == null || creditsData.sections.Length == 0)
        {
            Debug.LogError("[CreditsPageController] Failed to parse credits.json or no sections found.");
            return;
        }

        // Organize sections into pages
        List<string> pageLines = new List<string>();

        foreach (CreditSection section in creditsData.sections)
        {
            // Add section title
            pageLines.Add($"<b><size=36>{section.title}</size></b>");
            pageLines.Add(""); // Blank line

            // Add entries
            foreach (CreditEntry entry in section.entries)
            {
                string line = $"<size=18><b>{entry.name}</b></size>\n" +
                             $"<size=14>by {entry.artist} • {entry.source}</size>";
                pageLines.Add(line);
            }

            pageLines.Add(""); // Blank line between sections
        }

        // Split items into pages based on actual rendered line count
        List<string> currentPage = new List<string>();
        int currentLineCount = 0;
        foreach (string item in pageLines)
        {
            int itemLineCount = item.Split('\n').Length;

            // Start a new page if this item would overflow (keep at least one item per page)
            if (currentLineCount + itemLineCount > linesPerPage && currentPage.Count > 0)
            {
                _pages.Add(string.Join("\n", currentPage));
                currentPage.Clear();
                currentLineCount = 0;
            }

            currentPage.Add(item);
            currentLineCount += itemLineCount;
        }

        // Add any remaining items as the final page
        if (currentPage.Count > 0)
        {
            _pages.Add(string.Join("\n", currentPage));
        }

        Debug.Log($"[CreditsPageController] Loaded credits into {_pages.Count} pages.");
    }

    /// <summary>
    /// Called when it's time to advance to the next page.
    /// If on the final page, triggers the fade-out and return to menu.
    /// </summary>
    private void AdvancePageOrFinish()
    {
        if (_currentPageIndex < _pages.Count - 1)
        {
            // Advance to next page
            DisplayPage(_currentPageIndex + 1);
            _pageTimer = 0f;
        }
        else
        {
            // On final page, trigger fade and return
            StartCoroutine(AutoFadeAndReturn());
        }
    }

    /// <summary>
    /// Displays the specified page number and resets the timer.
    /// </summary>
    private void DisplayPage(int pageIndex)
    {
        if (pageIndex < 0 || pageIndex >= _pages.Count)
        {
            Debug.LogWarning($"[CreditsPageController] Invalid page index: {pageIndex}");
            return;
        }

        _currentPageIndex = pageIndex;
        
        if (creditsDisplayText != null)
        {
            creditsDisplayText.text = _pages[pageIndex];
        }

        UpdatePageCounter();
        _pageTimer = 0f;
    }

    /// <summary>
    /// Updates the page counter display (e.g., "Page 1 of 3").
    /// </summary>
    private void UpdatePageCounter()
    {
        if (pageCounterText != null)
        {
            pageCounterText.text = $"Page {_currentPageIndex + 1} of {_pages.Count}";
        }
    }

    /// <summary>
    /// Auto-fades after delay on final page, then returns to TitleScene.
    /// </summary>
    private System.Collections.IEnumerator AutoFadeAndReturn()
    {
        _isAutoFadingAtEnd = true;

        // Wait before starting fade
        yield return new WaitForSeconds(autoFadeDelay);

        // Fade to black
        if (fadeCanvasGroup != null)
        {
            float elapsedTime = 0f;
            while (elapsedTime < fadeDuration)
            {
                elapsedTime += Time.deltaTime;
                fadeCanvasGroup.alpha = Mathf.Clamp01(elapsedTime / fadeDuration);
                yield return null;
            }
            fadeCanvasGroup.alpha = 1f;
        }

        // Load TitleScene
        UnityEngine.SceneManagement.SceneManager.LoadScene("TitleScene");
    }
}
