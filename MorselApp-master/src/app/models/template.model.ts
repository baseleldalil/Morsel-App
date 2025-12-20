// Campaign Template from API
export interface CampaignTemplate {
  id: number;
  name: string;
  description?: string;
  maleMessage: string;
  femaleMessage: string;
  isSystemTemplate: boolean;
  createdAt: string;
  updatedAt?: string;
  icon?: string;
}

// API response for templates list - actual backend structure
export interface TemplatesListResponse {
  adminTemplates: ApiCampaignTemplate[];
  userTemplates: ApiCampaignTemplate[];
  totalCount: number;
  page: number;
  pageSize: number;
}

// API model (camelCase from backend)
export interface ApiCampaignTemplate {
  id: number;
  name: string;
  content: string;
  maleContent: string | null;
  femaleContent: string | null;
  description?: string;
  isSystemTemplate: boolean;
  isActive: boolean;
  userId: string;
  createdAt: string;
  updatedAt?: string | null;
}

// Request to create a template
export interface CreateCampaignTemplateRequest {
  name: string;
  description?: string;
  content: string;
  maleContent?: string;
  femaleContent?: string;
}

// Request to update a template
export interface UpdateCampaignTemplateRequest {
  name: string;
  description?: string;
  content: string;
  maleContent?: string;
  femaleContent?: string;
}

// Legacy Template interface for backward compatibility
export interface Template {
  id: number;
  title: string;
  description?: string;
  content: string;
  type: 'system' | 'user';
  createdAt: Date;
  icon?: string;
  maleMessage?: string;
  femaleMessage?: string;
}

export type TemplateTab = 'system' | 'user';

// Helper to convert API template to UI template
export function mapApiToTemplate(api: ApiCampaignTemplate): Template {
  return {
    id: api.id,
    title: api.name,
    description: api.description || '',
    content: api.content || api.maleContent || '',
    type: api.isSystemTemplate ? 'system' : 'user',
    createdAt: new Date(api.createdAt),
    maleMessage: api.maleContent || api.content || '',
    femaleMessage: api.femaleContent || api.maleContent || api.content || ''
  };
}

// Helper to convert API template to CampaignTemplate
export function mapApiToCampaignTemplate(api: ApiCampaignTemplate): CampaignTemplate {
  return {
    id: api.id,
    name: api.name,
    description: api.description,
    maleMessage: api.maleContent || api.content || '',
    femaleMessage: api.femaleContent || api.maleContent || api.content || '',
    isSystemTemplate: api.isSystemTemplate,
    createdAt: api.createdAt,
    updatedAt: api.updatedAt || undefined
  };
}
