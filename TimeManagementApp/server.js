const express = require('express');
const fs = require('fs');
const path = require('path');
const cors = require('cors');

const app = express();
const PORT = 3000;

// Middleware
app.use(cors());
app.use(express.json({ limit: '10mb' }));
app.use(express.static(__dirname));

// Serve the HTML file
app.get('/', (req, res) => {
    res.sendFile(path.join(__dirname, 'tending_to_project_tracker.html'));
});

// API endpoint to save data
app.post('/api/save', (req, res) => {
    try {
        const data = req.body;
        const jsonString = JSON.stringify(data, null, 2);
        
        fs.writeFileSync(
            path.join(__dirname, 'data.json'),
            jsonString,
            'utf8'
        );
        
        console.log('✅ Data saved successfully at', new Date().toLocaleString());
        res.json({ success: true, message: 'Data saved successfully' });
    } catch (error) {
        console.error('❌ Error saving data:', error);
        res.status(500).json({ success: false, message: error.message });
    }
});

// API endpoint to check if server is available
app.get('/api/ping', (req, res) => {
    res.json({ status: 'ok', mode: 'local' });
});

app.listen(PORT, () => {
    console.log('\n🌱 Tending To — Project Tracker Server');
    console.log('━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━');
    console.log(`✅ Server running at http://localhost:${PORT}`);
    console.log('📝 Auto-save enabled for local development');
    console.log('💾 Changes will be saved to data.json automatically');
    console.log('\nPress Ctrl+C to stop the server\n');
});
