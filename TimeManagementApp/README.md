# Tending To — Project Tracker

A time management and project tracking application with local development server support.

## 🌱 Features

- **Weekly Task Management** - Track tasks in todo, in-progress, and done states
- **Notes & References** - Keep project notes and helpful links
- **Documentation Hub** - Track screenshots, videos, sketches, and tests
- **Auto-Save (Local Mode)** - Automatically saves changes when running on local server
- **GitHub Pages Compatible** - Works in read-only mode when hosted statically

## 🚀 Running Locally (with Auto-Save)

### First Time Setup

1. Install Node.js (if not already installed)
2. Navigate to the TimeManagementApp directory:
   ```bash
   cd TimeManagementApp
   ```
3. Install dependencies:
   ```bash
   npm install
   ```

### Start the Server

Run the development server:
```bash
npm start
```

Then open your browser to:
```
http://localhost:3000
```

You'll see **"💾 Local Mode (Auto-Save)"** in the top-right corner. All changes will automatically save to `data.json` when you click the save button.

### Stop the Server

Press `Ctrl+C` in the terminal.

## 🌐 Running on GitHub Pages

When you deploy to GitHub Pages, the app runs in **"📋 Read-Only Mode"**. You can view data but saves will use the manual clipboard method:

1. Make changes
2. Click "💾 Save Changes"
3. Copy the JSON from the text area
4. Manually paste into your local `data.json`

## 📂 Project Structure

```
TimeManagementApp/
├── tending_to_project_tracker.html  # Main application
├── data.json                         # Your project data
├── server.js                         # Local development server
├── package.json                      # Node.js dependencies
└── README.md                         # This file
```

## 🔧 Troubleshooting

**Server won't start?**
- Make sure Node.js is installed: `node --version`
- Make sure dependencies are installed: `npm install`
- Check if port 3000 is already in use

**Changes not saving?**
- Check that you see "💾 Local Mode (Auto-Save)" in the top-right
- If you see "📋 Read-Only Mode", the server isn't running
- Check the terminal for server logs

**Need a different port?**
- Edit `server.js` and change the `PORT` constant
