# Tending To - Project Tracker

A VR installation project tracker with GitHub Pages deployment and JSON-based persistence.

## 🚀 Setting Up GitHub Pages

### 1. Create a GitHub Repository

```bash
cd /Users/tawnywhatmore/_src/Tending-To
git init
git add .
git commit -m "Initial commit: Tending To project tracker"
```

Create a new repository on GitHub, then:

```bash
git remote add origin https://github.com/YOUR-USERNAME/YOUR-REPO-NAME.git
git branch -M main
git push -u origin main
```

### 2. Enable GitHub Pages

1. Go to your repository on GitHub
2. Click **Settings** → **Pages**
3. Under "Source", select **Deploy from a branch**
4. Choose **main** branch and **/root** folder
5. Click **Save**

Your site will be live at: `https://YOUR-USERNAME.github.io/YOUR-REPO-NAME/TimeManagementApp/tending_to_project_tracker.html`

## 💾 Data Persistence Workflow

### How It Works

The app supports two modes:
- **JSON Mode** (default for GitHub Pages): Data loads from `data.json`
- **localStorage Mode**: Data saves to browser storage (for local development)

### Data Structure

All data is stored in `TimeManagementApp/data.json`:
- **taskStates**: Your current progress (which tasks are in todo/in-progress/done)
- **notes**: Research notes and findings
- **references**: Links and sources
- **documentation**: Screenshots, videos, sketches, and tests
- **defaultTasks**: The 9-week project timeline template (stays constant)
- **lastUpdated**: Timestamp of last update

### Workflow for Updating Progress

1. **Work Locally**: Open the HTML file locally and use **"Use Local Storage"** mode
2. **Make Changes**: Update tasks, add notes, references, etc.
3. **Export State**: Click **"📤 Export Current State"**
4. **Copy JSON**: Click **"📋 Copy to Clipboard"**
5. **Update data.json**: Paste the content into `TimeManagementApp/data.json`
6. **Commit & Push**:
   ```bash
   cd /Users/tawnywhatmore/_src/Tending-To
   git add TimeManagementApp/data.json
   git commit -m "Update project progress"
   git push
   ```
7. **Share**: Send your lecturer the GitHub Pages link - they'll see your updated progress!

### Quick Update Command

```bash
# From the project root
git add TimeManagementApp/data.json
git commit -m "Update progress: $(date)"
git push
```

## 🎯 Features

- **9-Week Timeline**: Kanban boards for each week
- **Drag & Drop**: Move tasks between todo/in-progress/done
- **Notes & Research**: Document your findings
- **References**: Track links and sources
- **Documentation**: Organize screenshots, videos, sketches, and tests
- **Progress Tracking**: Real-time statistics
- **Data Export**: JSON-based state management for GitHub Pages

## 📱 Data Sync Controls

- **📥 Load from data.json**: Fetch latest committed state
- **📤 Export Current State**: Generate JSON for committing
- **💻 Use Local Storage**: Switch to browser storage for local work

## 🔄 Typical Workflow

### Local Development
1. Open `tending_to_project_tracker.html` in browser
2. Click "💻 Use Local Storage"
3. Work on your project, update tasks
4. When ready to share, click "📤 Export Current State"
5. Copy the JSON and paste into `data.json`
6. Commit and push to GitHub

### Viewing on GitHub Pages
- The page automatically loads from `data.json`
- Anyone with the link sees your latest committed progress
- No login or authentication needed

## 🎨 Customization

The tracker is styled with an organic, garden-themed aesthetic matching the "Tending To" VR installation theme.

Colors:
- Soil/Earth browns: `#2a2118`, `#4a3728`, `#6b4e3d`
- Moss/Leaf greens: `#3d5a3d`, `#5a7a4f`, `#7a9668`
- Parchment background: `#f4ede4`
- Fire accents: `#e8734f`, `#f4a261`

## 📋 Project Timeline

**Deadline**: April 24, 2026

- Week 1 (Feb 23-Mar 1): Research & Pre-Production
- Week 2-8: Development phases
- Week 9 (Apr 20-24): Buffer & Submission

## 🛠 Technical Details

- Pure HTML/CSS/JavaScript (no build process needed)
- Responsive design
- Works offline after initial load
- Cross-browser compatible
- Mobile-friendly

## 📝 License

Personal academic project for VR installation coursework.
