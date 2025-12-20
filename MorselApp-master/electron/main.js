const { app, BrowserWindow, ipcMain } = require('electron');
const path = require('path');
const { spawn, execSync } = require('child_process');
const fs = require('fs');
const net = require('net');

let mainWindow;
let apiProcess = null;
let automationProcess = null;
let postgresProcess = null;

// Determine if we're in development or production
const isDev = !app.isPackaged;

// Get the path to bundled executables
function getResourcePath(relativePath) {
  if (isDev) {
    return path.join(__dirname, '..', relativePath);
  }
  return path.join(process.resourcesPath, relativePath);
}

// Get user data path for persistent data
function getDataPath(relativePath) {
  const dataDir = path.join(app.getPath('userData'), 'data');
  if (!fs.existsSync(dataDir)) {
    fs.mkdirSync(dataDir, { recursive: true });
  }
  return path.join(dataDir, relativePath);
}

// Configuration
const API_PORT = 7000;
const AUTOMATION_PORT = 5036;
const POSTGRES_PORT = 5433; // Use non-standard port to avoid conflicts

// Check if a port is available
function isPortAvailable(port) {
  return new Promise((resolve) => {
    const server = net.createServer();
    server.once('error', () => resolve(false));
    server.once('listening', () => {
      server.close();
      resolve(true);
    });
    server.listen(port);
  });
}

// Initialize embedded PostgreSQL
async function initPostgres() {
  const pgDir = getResourcePath('pgsql');
  const pgBinDir = path.join(pgDir, 'bin');
  const pgDataDir = getDataPath('pgdata');
  const pgLogFile = getDataPath('postgres.log');

  console.log('PostgreSQL Directory:', pgDir);
  console.log('PostgreSQL Data Directory:', pgDataDir);

  // Check if PostgreSQL binaries exist
  const pgCtl = path.join(pgBinDir, 'pg_ctl.exe');
  const initDb = path.join(pgBinDir, 'initdb.exe');
  const psql = path.join(pgBinDir, 'psql.exe');

  if (!fs.existsSync(pgCtl)) {
    console.log('PostgreSQL binaries not found at:', pgBinDir);
    console.log('Falling back to external PostgreSQL connection');
    return false;
  }

  // Initialize database if not exists
  if (!fs.existsSync(path.join(pgDataDir, 'PG_VERSION'))) {
    console.log('Initializing PostgreSQL database...');
    try {
      execSync(`"${initDb}" -D "${pgDataDir}" -U postgres -E UTF8 --locale=C`, {
        env: { ...process.env, PGPASSWORD: '1111' },
        stdio: 'pipe'
      });
      console.log('PostgreSQL database initialized successfully');

      // Configure PostgreSQL
      const pgConfPath = path.join(pgDataDir, 'postgresql.conf');
      let pgConf = fs.readFileSync(pgConfPath, 'utf8');
      pgConf = pgConf.replace(/#port = 5432/, `port = ${POSTGRES_PORT}`);
      pgConf = pgConf.replace(/#listen_addresses = 'localhost'/, "listen_addresses = 'localhost'");
      fs.writeFileSync(pgConfPath, pgConf);

      // Configure authentication
      const pgHbaPath = path.join(pgDataDir, 'pg_hba.conf');
      fs.writeFileSync(pgHbaPath, `
# TYPE  DATABASE        USER            ADDRESS                 METHOD
local   all             all                                     trust
host    all             all             127.0.0.1/32            trust
host    all             all             ::1/128                 trust
`);
    } catch (err) {
      console.error('Failed to initialize PostgreSQL:', err.message);
      return false;
    }
  }

  // Start PostgreSQL
  console.log('Starting PostgreSQL server...');
  try {
    // Check if already running
    const portAvailable = await isPortAvailable(POSTGRES_PORT);
    if (!portAvailable) {
      console.log(`PostgreSQL port ${POSTGRES_PORT} is already in use, assuming server is running`);
      return true;
    }

    postgresProcess = spawn(pgCtl, [
      'start',
      '-D', pgDataDir,
      '-l', pgLogFile,
      '-w'  // Wait for startup to complete
    ], {
      env: { ...process.env, PGDATA: pgDataDir },
      stdio: 'pipe',
      cwd: pgBinDir
    });

    postgresProcess.stdout.on('data', (data) => {
      console.log(`PostgreSQL: ${data}`);
    });

    postgresProcess.stderr.on('data', (data) => {
      console.error(`PostgreSQL Error: ${data}`);
    });

    // Wait for PostgreSQL to start
    await new Promise(resolve => setTimeout(resolve, 3000));

    // Create database if not exists and import data
    let isNewDatabase = false;
    try {
      execSync(`"${psql}" -h localhost -p ${POSTGRES_PORT} -U postgres -tc "SELECT 1 FROM pg_database WHERE datname = 'whatsapp_saas'" | findstr 1`, {
        stdio: 'pipe'
      });
      console.log('Database whatsapp_saas exists');
    } catch {
      console.log('Creating whatsapp_saas database...');
      isNewDatabase = true;
      try {
        execSync(`"${psql}" -h localhost -p ${POSTGRES_PORT} -U postgres -c "CREATE DATABASE whatsapp_saas"`, {
          stdio: 'pipe'
        });
        console.log('Database created successfully');
      } catch (err) {
        console.error('Error creating database:', err.message);
      }
    }

    // Set password for postgres user
    try {
      execSync(`"${psql}" -h localhost -p ${POSTGRES_PORT} -U postgres -c "ALTER USER postgres PASSWORD '1111'"`, {
        stdio: 'pipe'
      });
    } catch (err) {
      console.log('Password may already be set');
    }

    // Import database schema and data if this is a new database
    if (isNewDatabase) {
      const dataSqlPath = getResourcePath('database/data.sql');
      if (fs.existsSync(dataSqlPath)) {
        console.log('Importing database schema and data...');
        try {
          execSync(`"${psql}" -h localhost -p ${POSTGRES_PORT} -U postgres -d whatsapp_saas -f "${dataSqlPath}"`, {
            stdio: 'pipe',
            maxBuffer: 50 * 1024 * 1024 // 50MB buffer for large SQL files
          });
          console.log('Database imported successfully');
        } catch (err) {
          console.error('Error importing database:', err.message);
        }
      } else {
        console.log('Database dump file not found at:', dataSqlPath);
      }
    }

    console.log(`PostgreSQL server started on port ${POSTGRES_PORT}`);
    return true;
  } catch (err) {
    console.error('Failed to start PostgreSQL:', err.message);
    return false;
  }
}

// Stop PostgreSQL
async function stopPostgres() {
  const pgDir = getResourcePath('pgsql');
  const pgBinDir = path.join(pgDir, 'bin');
  const pgDataDir = getDataPath('pgdata');
  const pgCtl = path.join(pgBinDir, 'pg_ctl.exe');

  if (fs.existsSync(pgCtl) && fs.existsSync(pgDataDir)) {
    console.log('Stopping PostgreSQL server...');
    try {
      execSync(`"${pgCtl}" stop -D "${pgDataDir}" -m fast`, {
        stdio: 'pipe'
      });
      console.log('PostgreSQL server stopped');
    } catch (err) {
      console.log('PostgreSQL may already be stopped');
    }
  }
}

// Start the backend API
function startBackendServices(useEmbeddedDb = false) {
  const apiExePath = getResourcePath('backend/api/WhatsAppSender.API.exe');
  const automationExePath = getResourcePath('backend/automation/WhatsAppWebAutomation.exe');

  console.log('Starting backend services...');
  console.log('API Path:', apiExePath);
  console.log('Automation Path:', automationExePath);

  // Connection string environment variable - Use Supabase cloud database
  const connectionString = 'Host=db.ydmbjbhiasprzrexqkxy.supabase.co;Port=5432;Database=postgres;Username=postgres;Password=7216021mikavdodo;SSL Mode=Require;Trust Server Certificate=true';

  const env = {
    ...process.env,
    ASPNETCORE_ENVIRONMENT: 'Production',
    ConnectionStrings__DefaultConnection: connectionString
  };

  // Start WhatsApp Sender API on port 7000
  if (fs.existsSync(apiExePath)) {
    apiProcess = spawn(apiExePath, ['--urls', `http://localhost:${API_PORT}`], {
      cwd: path.dirname(apiExePath),
      detached: false,
      stdio: 'pipe',
      env: env
    });

    apiProcess.stdout.on('data', (data) => {
      console.log(`API: ${data}`);
    });

    apiProcess.stderr.on('data', (data) => {
      console.error(`API Error: ${data}`);
    });

    apiProcess.on('close', (code) => {
      console.log(`API process exited with code ${code}`);
    });

    apiProcess.on('error', (err) => {
      console.error('Failed to start API process:', err);
    });

    console.log(`WhatsApp Sender API started on port ${API_PORT}`);
  } else {
    console.log('API executable not found at:', apiExePath);
  }

  // Start WhatsApp Web Automation on port 5036
  if (fs.existsSync(automationExePath)) {
    automationProcess = spawn(automationExePath, ['--urls', `http://localhost:${AUTOMATION_PORT}`], {
      cwd: path.dirname(automationExePath),
      detached: false,
      stdio: 'pipe',
      env: env
    });

    automationProcess.stdout.on('data', (data) => {
      console.log(`Automation: ${data}`);
    });

    automationProcess.stderr.on('data', (data) => {
      console.error(`Automation Error: ${data}`);
    });

    automationProcess.on('close', (code) => {
      console.log(`Automation process exited with code ${code}`);
    });

    automationProcess.on('error', (err) => {
      console.error('Failed to start Automation process:', err);
    });

    console.log(`WhatsApp Web Automation started on port ${AUTOMATION_PORT}`);
  } else {
    console.log('Automation executable not found at:', automationExePath);
  }
}

// Stop backend services
function stopBackendServices() {
  console.log('Stopping backend services...');

  if (apiProcess) {
    try {
      // On Windows, we need to use taskkill to properly terminate the process tree
      if (process.platform === 'win32') {
        spawn('taskkill', ['/pid', apiProcess.pid.toString(), '/f', '/t']);
      } else {
        process.kill(-apiProcess.pid);
      }
    } catch (e) {
      console.log('Error killing API process:', e.message);
    }
    apiProcess = null;
  }

  if (automationProcess) {
    try {
      if (process.platform === 'win32') {
        spawn('taskkill', ['/pid', automationProcess.pid.toString(), '/f', '/t']);
      } else {
        process.kill(-automationProcess.pid);
      }
    } catch (e) {
      console.log('Error killing Automation process:', e.message);
    }
    automationProcess = null;
  }
}

// Wait for API to be ready
async function waitForApi(port, maxAttempts = 30) {
  const http = require('http');

  for (let i = 0; i < maxAttempts; i++) {
    try {
      await new Promise((resolve, reject) => {
        const req = http.get(`http://localhost:${port}/api/health`, (res) => {
          resolve(true);
        });
        req.on('error', reject);
        req.setTimeout(1000, () => {
          req.destroy();
          reject(new Error('timeout'));
        });
      });
      console.log(`API on port ${port} is ready`);
      return true;
    } catch (e) {
      console.log(`Waiting for API on port ${port}... (${i + 1}/${maxAttempts})`);
      await new Promise(r => setTimeout(r, 1000));
    }
  }
  console.log(`API on port ${port} not ready after ${maxAttempts} attempts`);
  return false;
}

function createWindow() {
  mainWindow = new BrowserWindow({
    width: 1400,
    height: 900,
    minWidth: 1200,
    minHeight: 700,
    icon: path.join(__dirname, 'assets', 'icon.ico'),
    webPreferences: {
      nodeIntegration: false,
      contextIsolation: true,
      preload: path.join(__dirname, 'preload.js')
    },
    show: false,
    titleBarStyle: 'default',
    autoHideMenuBar: true
  });

  // Load the Angular app
  if (isDev) {
    // Development: load from Angular dev server
    mainWindow.loadURL('http://localhost:4200');
    mainWindow.webContents.openDevTools();
  } else {
    // Production: load from built files
    mainWindow.loadFile(path.join(__dirname, '..', 'dist', 'WhatsAppSender', 'browser', 'index.html'));
  }

  // Show window when ready
  mainWindow.once('ready-to-show', () => {
    mainWindow.show();
    mainWindow.focus();
  });

  mainWindow.on('closed', () => {
    mainWindow = null;
  });
}

// App lifecycle events
app.whenReady().then(async () => {
  // Skip embedded PostgreSQL - using Supabase cloud database
  console.log('Using Supabase cloud database...');

  // Start backend services (always use cloud DB)
  startBackendServices(false);

  // Create window immediately so user sees the app
  console.log('Creating window...');
  createWindow();

  // Wait for APIs in background - the Angular app will retry connections
  console.log('Waiting for backend services to be ready...');
  const [apiReady, automationReady] = await Promise.all([
    waitForApi(API_PORT, 120),  // 2 minutes timeout
    waitForApi(AUTOMATION_PORT, 120)
  ]);

  if (!apiReady || !automationReady) {
    console.error('Warning: Some backend services may not be ready');
    console.error('API Ready:', apiReady, 'Automation Ready:', automationReady);
  } else {
    console.log('All backend services ready!');
  }

  app.on('activate', () => {
    if (BrowserWindow.getAllWindows().length === 0) {
      createWindow();
    }
  });
});

app.on('window-all-closed', () => {
  stopBackendServices();
  stopPostgres();
  if (process.platform !== 'darwin') {
    app.quit();
  }
});

app.on('before-quit', () => {
  stopBackendServices();
  stopPostgres();
});

// Handle any uncaught exceptions
process.on('uncaughtException', (error) => {
  console.error('Uncaught Exception:', error);
  stopBackendServices();
  stopPostgres();
});
