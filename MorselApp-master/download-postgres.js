const https = require('https');
const fs = require('fs');
const path = require('path');
const { execSync } = require('child_process');

// PostgreSQL 16 embedded download URL
const POSTGRES_VERSION = '16.4-1';
const DOWNLOAD_URL = `https://get.enterprisedb.com/postgresql/postgresql-${POSTGRES_VERSION}-windows-x64-binaries.zip`;
const DEST_DIR = path.join(__dirname, 'pgsql');
const TEMP_ZIP = path.join(__dirname, 'postgresql.zip');

console.log('Downloading PostgreSQL portable...');
console.log('URL:', DOWNLOAD_URL);

// Download function using follow redirects
function downloadFile(url, destPath) {
  return new Promise((resolve, reject) => {
    const file = fs.createWriteStream(destPath);

    function makeRequest(requestUrl) {
      const protocol = requestUrl.startsWith('https') ? https : require('http');

      protocol.get(requestUrl, (response) => {
        // Handle redirects
        if (response.statusCode === 301 || response.statusCode === 302) {
          console.log('Following redirect to:', response.headers.location);
          makeRequest(response.headers.location);
          return;
        }

        if (response.statusCode !== 200) {
          reject(new Error(`HTTP ${response.statusCode}: ${response.statusMessage}`));
          return;
        }

        const totalSize = parseInt(response.headers['content-length'], 10);
        let downloadedSize = 0;

        response.pipe(file);

        response.on('data', (chunk) => {
          downloadedSize += chunk.length;
          const percent = ((downloadedSize / totalSize) * 100).toFixed(1);
          process.stdout.write(`\rDownloading: ${percent}% (${(downloadedSize / 1024 / 1024).toFixed(1)} MB)`);
        });

        file.on('finish', () => {
          file.close();
          console.log('\nDownload complete!');
          resolve();
        });
      }).on('error', (err) => {
        fs.unlink(destPath, () => {});
        reject(err);
      });
    }

    makeRequest(url);
  });
}

async function main() {
  try {
    // Check if already exists
    if (fs.existsSync(path.join(DEST_DIR, 'bin', 'postgres.exe'))) {
      console.log('PostgreSQL already exists, skipping download');
      return;
    }

    // Download
    await downloadFile(DOWNLOAD_URL, TEMP_ZIP);

    // Extract using PowerShell
    console.log('Extracting PostgreSQL...');
    execSync(`powershell -Command "Expand-Archive -Path '${TEMP_ZIP}' -DestinationPath '${__dirname}' -Force"`, {
      stdio: 'inherit'
    });

    // Cleanup
    fs.unlinkSync(TEMP_ZIP);

    console.log('PostgreSQL extracted to:', DEST_DIR);

    // Verify
    if (fs.existsSync(path.join(DEST_DIR, 'bin', 'postgres.exe'))) {
      console.log('PostgreSQL installation verified!');
    } else {
      console.error('PostgreSQL binary not found after extraction');
    }
  } catch (err) {
    console.error('Error:', err.message);
    process.exit(1);
  }
}

main();
