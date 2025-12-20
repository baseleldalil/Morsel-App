import { Injectable, signal, computed, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, tap, catchError, throwError, map } from 'rxjs';
import {
  Template,
  TemplateTab,
  ApiCampaignTemplate,
  CreateCampaignTemplateRequest,
  UpdateCampaignTemplateRequest,
  mapApiToTemplate
} from '../models';
import { AppConfig } from '../config';

// Response interface matching actual backend structure
interface TemplatesApiResponse {
  adminTemplates: ApiCampaignTemplate[];
  userTemplates: ApiCampaignTemplate[];
  totalCount: number;
  page: number;
  pageSize: number;
}

interface TemplateResponse {
  message: string;
  template?: ApiCampaignTemplate;
}

@Injectable({
  providedIn: 'root'
})
export class TemplatesService {
  private http = inject(HttpClient);
  private readonly API_URL = AppConfig.api.baseUrl;

  // State signals
  private allTemplatesSignal = signal<Template[]>([]);
  private activeTabSignal = signal<TemplateTab>('system');
  private searchQuerySignal = signal<string>('');
  private isLoadingSignal = signal<boolean>(false);
  private isSavingSignal = signal<boolean>(false);

  // Pagination signals
  private currentPageSignal = signal<number>(1);
  private pageSizeSignal = signal<number>(50);
  private totalPagesSignal = signal<number>(1);
  private totalCountSignal = signal<number>(0);

  // Public readonly signals
  readonly templates = this.allTemplatesSignal.asReadonly();
  readonly activeTab = this.activeTabSignal.asReadonly();
  readonly searchQuery = this.searchQuerySignal.asReadonly();
  readonly isLoading = this.isLoadingSignal.asReadonly();
  readonly isSaving = this.isSavingSignal.asReadonly();
  readonly currentPage = this.currentPageSignal.asReadonly();
  readonly pageSize = this.pageSizeSignal.asReadonly();
  readonly totalPages = this.totalPagesSignal.asReadonly();
  readonly totalCount = this.totalCountSignal.asReadonly();

  // Computed signals
  readonly filteredTemplates = computed<Template[]>(() => {
    const templates = this.allTemplatesSignal();
    const tab = this.activeTabSignal();
    const search = this.searchQuerySignal().toLowerCase();

    let filtered = templates.filter(t => t.type === tab);

    if (search) {
      filtered = filtered.filter(t =>
        t.title.toLowerCase().includes(search) ||
        t.content.toLowerCase().includes(search)
      );
    }

    return filtered;
  });

  readonly systemTemplatesCount = computed<number>(() => {
    return this.allTemplatesSignal().filter(t => t.type === 'system').length;
  });

  readonly userTemplatesCount = computed<number>(() => {
    return this.allTemplatesSignal().filter(t => t.type === 'user').length;
  });

  // Actions
  setActiveTab(tab: TemplateTab): void {
    this.activeTabSignal.set(tab);
  }

  setSearch(query: string): void {
    this.searchQuerySignal.set(query);
  }

  setPage(page: number): void {
    this.currentPageSignal.set(page);
    this.fetchTemplates();
  }

  setPageSize(size: number): void {
    this.pageSizeSignal.set(size);
    this.currentPageSignal.set(1);
    this.fetchTemplates();
  }

  // API Methods

  // Fetch all templates from API
  fetchTemplates(): void {
    this.isLoadingSignal.set(true);

    const params = new HttpParams()
      .set('page', this.currentPageSignal().toString())
      .set('pageSize', this.pageSizeSignal().toString());

    this.http.get<TemplatesApiResponse>(`${this.API_URL}${AppConfig.api.templates.base}`, { params })
      .subscribe({
        next: (response) => {
          // Combine adminTemplates (system) and userTemplates into one array
          const adminTemplates = response?.adminTemplates || [];
          const userTemplates = response?.userTemplates || [];

          // Map and combine both arrays
          const systemTemplates = Array.isArray(adminTemplates)
            ? adminTemplates.map(mapApiToTemplate)
            : [];
          const myTemplates = Array.isArray(userTemplates)
            ? userTemplates.map(mapApiToTemplate)
            : [];

          // Combine all templates
          const allTemplates = [...systemTemplates, ...myTemplates];
          this.allTemplatesSignal.set(allTemplates);

          // Update pagination info
          const totalPages = Math.ceil((response?.totalCount || 0) / this.pageSizeSignal()) || 1;
          this.totalPagesSignal.set(totalPages);
          this.totalCountSignal.set(response?.totalCount || 0);
          this.currentPageSignal.set(response?.page || 1);

          this.isLoadingSignal.set(false);
        },
        error: (error) => {
          console.error('Error fetching templates:', error);
          this.isLoadingSignal.set(false);
          this.allTemplatesSignal.set([]);
        }
      });
  }

  // Get single template by ID
  getTemplateById(id: number): Observable<Template> {
    return new Observable(observer => {
      this.http.get<ApiCampaignTemplate>(`${this.API_URL}${AppConfig.api.templates.byId}/${id}`)
        .subscribe({
          next: (apiTemplate) => {
            observer.next(mapApiToTemplate(apiTemplate));
            observer.complete();
          },
          error: (error) => {
            observer.error(error);
          }
        });
    });
  }

  // Create new template
  createTemplate(data: CreateCampaignTemplateRequest): Observable<Template> {
    this.isSavingSignal.set(true);

    return this.http.post<TemplateResponse>(`${this.API_URL}${AppConfig.api.templates.base}`, data)
      .pipe(
        tap((response) => {
          if (response.template) {
            const newTemplate = mapApiToTemplate(response.template);
            this.allTemplatesSignal.update(templates => [...templates, newTemplate]);
          }
          this.isSavingSignal.set(false);
          // Refresh to get accurate data
          this.fetchTemplates();
        }),
        map((response) => {
          if (response.template) {
            return mapApiToTemplate(response.template);
          }
          // Return a default template if no template in response
          return { id: 0, title: '', content: '', type: 'user' as const, createdAt: new Date() };
        }),
        catchError(error => {
          console.error('Error creating template:', error);
          this.isSavingSignal.set(false);
          return throwError(() => error);
        })
      );
  }

  // Update existing template
  updateTemplate(id: number, data: UpdateCampaignTemplateRequest): Observable<Template> {
    this.isSavingSignal.set(true);

    return this.http.put<TemplateResponse>(`${this.API_URL}${AppConfig.api.templates.byId}/${id}`, data)
      .pipe(
        tap((response) => {
          if (response.template) {
            const updatedTemplate = mapApiToTemplate(response.template);
            this.allTemplatesSignal.update(templates =>
              templates.map(t => t.id === id ? updatedTemplate : t)
            );
          }
          this.isSavingSignal.set(false);
          this.fetchTemplates();
        }),
        map((response) => {
          if (response.template) {
            return mapApiToTemplate(response.template);
          }
          // Return a default template if no template in response
          return { id: 0, title: '', content: '', type: 'user' as const, createdAt: new Date() };
        }),
        catchError(error => {
          console.error('Error updating template:', error);
          this.isSavingSignal.set(false);
          return throwError(() => error);
        })
      );
  }

  // Delete template
  deleteTemplate(id: number): Observable<void> {
    return this.http.delete<void>(`${this.API_URL}${AppConfig.api.templates.byId}/${id}`)
      .pipe(
        tap(() => {
          this.allTemplatesSignal.update(templates =>
            templates.filter(t => t.id !== id)
          );
        }),
        catchError(error => {
          console.error('Error deleting template:', error);
          return throwError(() => error);
        })
      );
  }

  // Legacy methods for backward compatibility
  addTemplate(template: Omit<Template, 'id' | 'createdAt'>): void {
    const request: CreateCampaignTemplateRequest = {
      name: template.title,
      description: '',
      content: template.maleMessage || template.content,
      maleContent: template.maleMessage || template.content,
      femaleContent: template.femaleMessage || template.content
    };
    this.createTemplate(request).subscribe();
  }

  // Refresh templates
  refreshTemplates(): void {
    this.fetchTemplates();
  }
}
