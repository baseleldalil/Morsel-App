// Check if running in Electron
const isElectron = !!(window as any).electronAPI;

// Get base URLs based on environment
const getApiBaseUrl = () => {
  if (isElectron) {
    return 'http://localhost:7000/api';
  }
  return '/api';
};

const getWhatsAppApiBaseUrl = () => {
  if (isElectron) {
    return 'http://localhost:5036/api';
  }
  return '/whatsapp-api';
};

export const AppConfig = {
  // Environment
  isElectron,

  // API Base URLs
  api: {
    baseUrl: getApiBaseUrl(),
    whatsappBaseUrl: getWhatsAppApiBaseUrl(),
    auth: {
      login: '/Auth/login',
      logout: '/Auth/logout',
      register: '/Auth/register'
    },
    contacts: {
      base: '/Contacts',
      statistics: '/Contacts/statistics',
      import: '/Contacts/import',
      export: '/Contacts/export',
      update: '/Contacts', // PUT /Contacts/{id}
      delete: '/Contacts', // DELETE /Contacts/{id}
      notInterested: '/Contacts', // POST /Contacts/{id}/not-interested
      responded: '/Contacts', // POST /Contacts/{id}/responded
      resend: '/Contacts', // POST /Contacts/{id}/resend
      resendAllFailed: '/Contacts/resend-all-failed', // POST - resend all failed contacts
      resendAllDelivered: '/Contacts/resend-all-delivered', // POST - resend all delivered contacts
      resendAllResponded: '/Contacts/resend-all-responded', // POST - resend all responded contacts
      resendAllNotInterested: '/Contacts/resend-all-not-interested' // POST - resend all not interested contacts
    },
    templates: {
      base: '/CampaignTemplates',           // GET (list), POST (create)
      byId: '/CampaignTemplates',           // GET, PUT, DELETE /CampaignTemplates/{id}
      systemTemplate: '/CampaignTemplates/admin/system-template' // POST (admin only)
    },
    messages: {
      send: '/Messages/send',
      templates: '/Messages/templates'
    },
    campaign: {
      start: '/Campaign/start',
      stop: '/Campaign/stop',
      status: '/Campaign/status'
    },
    whatsapp: {
      status: '/WhatsApp/status',
      init: '/WhatsApp/init',
      send: '/WhatsApp/send',
      sendBulk: '/WhatsApp/send-bulk',
      close: '/WhatsApp/close',
      bulk: {
        start: '/WhatsApp/bulk/start',
        status: '/WhatsApp/bulk/status',
        pause: '/WhatsApp/bulk/pause',
        resume: '/WhatsApp/bulk/resume',
        stop: '/WhatsApp/bulk/stop'
      }
    }
  },

  // Storage Keys
  storage: {
    apiKey: 'wapp_api_key',
    user: 'wapp_user',
    rememberMe: 'wapp_remember_me',
    browserType: 'wapp_browser_type'
  },

  // App Settings
  settings: {
    maxMessageLength: 5000,
    defaultPageSize: 10,
    bulkStatusPollingInterval: 2000 // Poll bulk status every 2 seconds
  }
};
