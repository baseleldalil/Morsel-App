import { Injectable, signal, computed, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, tap, catchError, throwError } from 'rxjs';
import { Contact, ContactFilter, StatisticsResponse, ApiContactStatistics, ImportContactsResponse, ImportOptions, UpdateContactRequest, ContactResponse, StatusChangeResponse, ContactsListResponse } from '../models';
import { AppConfig } from '../config';

// Local statistics interface for UI
export interface UIContactStatistics {
  total: number;
  pending: number;
  sending: number;
  sent: number;
  delivered: number;
  failed: number;
  issues: number;
  blocked: number;
  responded: number;
  notInterested: number;
}

@Injectable({
  providedIn: 'root'
})
export class ContactsService {
  private http = inject(HttpClient);
  private readonly API_URL = AppConfig.api.baseUrl;

  // State signals - allContacts stores everything from API
  private allContactsSignal = signal<Contact[]>([]);
  private selectedFilterSignal = signal<ContactFilter>('All');
  private searchQuerySignal = signal<string>('');
  private currentPageSignal = signal<number>(1);
  private pageSizeSignal = signal<number>(50);
  private apiStatisticsSignal = signal<ApiContactStatistics | null>(null);
  private isLoadingSignal = signal<boolean>(false);
  private isImportingSignal = signal<boolean>(false);

  // Public readonly signals
  readonly selectedFilter = this.selectedFilterSignal.asReadonly();
  readonly searchQuery = this.searchQuerySignal.asReadonly();
  readonly currentPage = this.currentPageSignal.asReadonly();
  readonly pageSize = this.pageSizeSignal.asReadonly();
  readonly isLoading = this.isLoadingSignal.asReadonly();
  readonly isImporting = this.isImportingSignal.asReadonly();

  // Status priority order for sorting (lower number = higher priority)
  private readonly statusPriority: Record<string, number> = {
    'Pending': 1,
    'HasIssues': 2,
    'NotValid': 3,
    'Failed': 4,
    'Responded': 5,
    'Delivered': 6,
    'NotInterested': 7,
    'Sent': 8,
    'Blocked': 9
  };

  // Computed: Filter contacts locally based on selected filter
  readonly filteredContacts = computed<Contact[]>(() => {
    let contacts = this.allContactsSignal();
    const filter = this.selectedFilterSignal();
    const search = this.searchQuerySignal().toLowerCase();

    // Apply status filter
    if (filter !== 'All') {
      const statusMap: Record<ContactFilter, string[]> = {
        'All': [],
        'Pending': ['Pending'],
        'Sent': ['Sent'],
        'Delivered': ['Delivered'],
        'Failed': ['Failed'],
        'Issues': ['NotValid', 'HasIssues'],
        'Respond': ['Responded'],
        'NotInt': ['NotInterested']
      };
      const statuses = statusMap[filter];
      if (statuses.length > 0) {
        contacts = contacts.filter(c => statuses.includes(c.status));
      }
    }

    // Apply search filter
    if (search) {
      contacts = contacts.filter(c =>
        c.firstName.toLowerCase().includes(search) ||
        c.arabicName.toLowerCase().includes(search) ||
        c.englishName.toLowerCase().includes(search) ||
        c.number.includes(search)
      );
    }

    // Sort by status priority (Pending first, then HasIssues, Failed, Responded, Delivered, NotInterested)
    // Secondary sort by ID descending (newest/latest imported first within each status group)
    contacts = [...contacts].sort((a, b) => {
      const priorityA = this.statusPriority[a.status] ?? 99;
      const priorityB = this.statusPriority[b.status] ?? 99;

      // First sort by status priority
      if (priorityA !== priorityB) {
        return priorityA - priorityB;
      }

      // Then sort by ID descending (higher ID = newer import = shows first)
      return b.id - a.id;
    });

    return contacts;
  });

  // Computed: Total count of filtered contacts
  readonly totalContacts = computed<number>(() => {
    return this.filteredContacts().length;
  });

  // Computed: Total pages based on filtered contacts
  readonly totalPages = computed<number>(() => {
    const total = this.filteredContacts().length;
    const size = this.pageSizeSignal();
    return Math.max(1, Math.ceil(total / size));
  });

  // Computed: Paginated contacts for display (client-side pagination)
  readonly paginatedContacts = computed<Contact[]>(() => {
    const filtered = this.filteredContacts();
    const page = this.currentPageSignal();
    const size = this.pageSizeSignal();

    const startIndex = (page - 1) * size;
    const endIndex = startIndex + size;

    return filtered.slice(startIndex, endIndex);
  });

  // Alias for backward compatibility
  readonly contacts = this.allContactsSignal.asReadonly();

  // Computed statistics from API or fallback to local calculation
  readonly statistics = computed<UIContactStatistics>(() => {
    const apiStats = this.apiStatisticsSignal();

    if (apiStats) {
      return {
        total: apiStats.totalContacts,
        pending: apiStats.pendingCount,
        sending: apiStats.sendingCount,
        sent: apiStats.sentCount,
        delivered: apiStats.deliveredCount,
        failed: apiStats.failedCount + apiStats.sendingCount, // Include stuck (Sending) as Failed
        issues: apiStats.hasIssuesCount + apiStats.notValidCount,
        blocked: apiStats.blockedCount,
        responded: apiStats.respondedCount,
        notInterested: apiStats.notInterestedCount
      };
    }

    // Fallback to local calculation from all contacts
    const contacts = this.allContactsSignal();
    return {
      total: contacts.length,
      pending: contacts.filter(c => c.status === 'Pending').length,
      sending: 0,
      sent: contacts.filter(c => c.status === 'Sent').length,
      delivered: contacts.filter(c => c.status === 'Delivered').length,
      failed: contacts.filter(c => c.status === 'Failed').length,
      issues: contacts.filter(c => c.status === 'NotValid' || c.status === 'HasIssues').length,
      blocked: 0,
      responded: contacts.filter(c => c.status === 'Responded').length,
      notInterested: contacts.filter(c => c.status === 'NotInterested').length
    };
  });

  // Selected contacts - maintain table display order (as received from backend)
  readonly selectedContacts = computed<Contact[]>(() => {
    return this.allContactsSignal().filter(c => c.selected);
  });

  readonly selectedCount = computed<number>(() => {
    return this.selectedContacts().length;
  });

  // Actions - These now just update local state, no API calls needed
  setFilter(filter: ContactFilter): void {
    this.selectedFilterSignal.set(filter);
    this.currentPageSignal.set(1); // Reset to first page when filter changes
  }

  setSearch(query: string): void {
    this.searchQuerySignal.set(query);
    this.currentPageSignal.set(1); // Reset to first page when search changes
  }

  setPage(page: number): void {
    const total = this.totalPages();
    if (page >= 1 && page <= total) {
      this.currentPageSignal.set(page);
    }
  }

  setPageSize(size: number): void {
    this.pageSizeSignal.set(size);
    this.currentPageSignal.set(1); // Reset to first page when page size changes
  }

  toggleSelect(contactId: number): void {
    this.allContactsSignal.update(contacts =>
      contacts.map(c => {
        if (c.id !== contactId) return c;
        // Only Pending contacts can be selected
        if (c.status !== 'Pending' && !c.selected) return c;
        return { ...c, selected: !c.selected };
      })
    );
  }

  // Select/deselect only visible (paginated) contacts
  // Only Pending contacts can be selected for campaigns
  selectAllVisible(selected: boolean): void {
    const visibleIds = new Set(this.paginatedContacts().map(c => c.id));
    this.allContactsSignal.update(contacts =>
      contacts.map(c => {
        if (!visibleIds.has(c.id)) return c;
        // Only Pending contacts can be selected
        if (selected && c.status !== 'Pending') return c;
        return { ...c, selected };
      })
    );
  }

  // Select/deselect all filtered contacts (all pages)
  // Only Pending contacts can be selected for campaigns
  selectAllFiltered(selected: boolean): void {
    const filteredIds = new Set(this.filteredContacts().map(c => c.id));
    this.allContactsSignal.update(contacts =>
      contacts.map(c => {
        if (!filteredIds.has(c.id)) return c;
        // Only Pending contacts can be selected
        if (selected && c.status !== 'Pending') return c;
        return { ...c, selected };
      })
    );
  }

  // Clear all selections
  clearSelection(): void {
    this.allContactsSignal.update(contacts =>
      contacts.map(c => ({ ...c, selected: false }))
    );
  }

  // Legacy method for backward compatibility
  selectAll(selected: boolean): void {
    this.selectAllVisible(selected);
  }

  // Get count of selected contacts in current visible page
  readonly visibleSelectedCount = computed<number>(() => {
    return this.paginatedContacts().filter(c => c.selected).length;
  });

  // Get count of selected contacts in filtered list
  readonly filteredSelectedCount = computed<number>(() => {
    return this.filteredContacts().filter(c => c.selected).length;
  });

  updateContactStatus(contactId: number, status: Contact['status']): void {
    this.allContactsSignal.update(contacts =>
      contacts.map(c =>
        c.id === contactId ? { ...c, status } : c
      )
    );
  }

  clearContacts(): void {
    this.allContactsSignal.set([]);
  }

  importContacts(contacts: Contact[]): void {
    this.allContactsSignal.update(existing => [...existing, ...contacts]);
  }

  // API Methods
  fetchStatistics(): void {
    this.isLoadingSignal.set(true);
    this.http.get<StatisticsResponse>(`${this.API_URL}${AppConfig.api.contacts.statistics}`)
      .subscribe({
        next: (response) => {
          this.apiStatisticsSignal.set(response.statistics);
          this.isLoadingSignal.set(false);
        },
        error: (error) => {
          console.error('Error fetching statistics:', error);
          this.isLoadingSignal.set(false);
        }
      });
  }

  refreshStatistics(): void {
    this.fetchStatistics();
  }

  // Fetch ALL contacts from API (no pagination params - get everything)
  fetchContacts(): void {
    this.isLoadingSignal.set(true);

    // Preserve current selection state before fetching
    const selectedIds = new Set(
      this.allContactsSignal()
        .filter(c => c.selected)
        .map(c => c.id)
    );

    // Request a large page size to get all contacts, or no pagination at all
    const params = new HttpParams()
      .set('page', '1')
      .set('page_size', '10000'); // Get all contacts

    this.http.get<ContactsListResponse>(`${this.API_URL}${AppConfig.api.contacts.base}`, { params })
      .subscribe({
        next: (response) => {
          // Map API response fields to UI Contact model
          // Preserve selection state for existing contacts
          const contacts: Contact[] = response.contacts.map(c => ({
            id: c.id,
            firstName: c.first_name,
            arabicName: c.arabic_name,
            englishName: c.english_name,
            number: c.formatted_phone,
            gender: this.normalizeGender(c.gender), // Normalize to uppercase M/F/U
            status: this.mapStatus(c.status),
            selected: selectedIds.has(c.id), // Preserve selection!
            issueDescription: c.issue_description,
            sendAttemptCount: c.send_attempt_count
          }));
          this.allContactsSignal.set(contacts);
          this.isLoadingSignal.set(false);
        },
        error: (error) => {
          console.error('Error fetching contacts:', error);
          this.isLoadingSignal.set(false);
        }
      });
  }

  importContactsFromFile(
    file: File,
    options: ImportOptions = { allowInternational: true, skipDuplicates: true }
  ): Observable<ImportContactsResponse> {
    this.isImportingSignal.set(true);

    const formData = new FormData();
    formData.append('file', file);

    const params = new HttpParams()
      .set('allowInternational', options.allowInternational.toString())
      .set('skipDuplicates', options.skipDuplicates.toString());

    return new Observable<ImportContactsResponse>(observer => {
      this.http.post<ImportContactsResponse>(
        `${this.API_URL}${AppConfig.api.contacts.import}`,
        formData,
        { params }
      ).subscribe({
        next: (response) => {
          this.isImportingSignal.set(false);
          this.fetchContacts(); // Refresh all contacts after import
          this.fetchStatistics();
          observer.next(response);
          observer.complete();
        },
        error: (error) => {
          this.isImportingSignal.set(false);
          observer.error(error);
        }
      });
    });
  }

  // Update contact
  updateContact(contactId: number, data: UpdateContactRequest): Observable<ContactResponse> {
    return this.http.put<ContactResponse>(
      `${this.API_URL}${AppConfig.api.contacts.update}/${contactId}`,
      data
    ).pipe(
      tap(() => {
        // Update local state with mapped field names
        this.allContactsSignal.update(contacts =>
          contacts.map(c =>
            c.id === contactId
              ? {
                  ...c,
                  firstName: data.Name,
                  arabicName: data.ArabicName,
                  englishName: data.EnglishName,
                  number: data.Phone,
                  gender: this.normalizeGender(data.Gender) // Normalize to uppercase M/F/U
                }
              : c
          )
        );
        this.fetchStatistics();
      }),
      catchError(error => {
        console.error('Error updating contact:', error);
        return throwError(() => error);
      })
    );
  }

  // Delete contact
  deleteContact(contactId: number): Observable<ContactResponse> {
    return this.http.delete<ContactResponse>(
      `${this.API_URL}${AppConfig.api.contacts.delete}/${contactId}`
    ).pipe(
      tap(() => {
        // Remove from local state
        this.allContactsSignal.update(contacts =>
          contacts.filter(c => c.id !== contactId)
        );
        this.fetchStatistics();
      }),
      catchError(error => {
        console.error('Error deleting contact:', error);
        return throwError(() => error);
      })
    );
  }

  // Bulk delete contacts - sends individual delete requests
  deleteContacts(contactIds: number[]): Observable<{ success: boolean; deleted: number; failed: number }> {
    return new Observable(observer => {
      let deleted = 0;
      let failed = 0;
      let completed = 0;
      const total = contactIds.length;

      if (total === 0) {
        observer.next({ success: true, deleted: 0, failed: 0 });
        observer.complete();
        return;
      }

      // Send all delete requests in parallel
      contactIds.forEach(id => {
        this.http.delete<any>(
          `${this.API_URL}${AppConfig.api.contacts.delete}/${id}`
        ).subscribe({
          next: () => {
            deleted++;
            completed++;
            // Remove from local state immediately
            this.allContactsSignal.update(contacts =>
              contacts.filter(c => c.id !== id)
            );
            if (completed === total) {
              this.fetchStatistics();
              observer.next({ success: failed === 0, deleted, failed });
              observer.complete();
            }
          },
          error: () => {
            failed++;
            completed++;
            if (completed === total) {
              this.fetchStatistics();
              observer.next({ success: failed === 0, deleted, failed });
              observer.complete();
            }
          }
        });
      });
    });
  }

  // Mark contact as not interested
  markNotInterested(contactId: number): Observable<StatusChangeResponse> {
    return this.http.post<StatusChangeResponse>(
      `${this.API_URL}${AppConfig.api.contacts.notInterested}/${contactId}/not-interested`,
      {}
    ).pipe(
      tap(() => {
        this.allContactsSignal.update(contacts =>
          contacts.map(c =>
            c.id === contactId ? { ...c, status: 'NotInterested' as const } : c
          )
        );
        this.fetchStatistics();
      }),
      catchError(error => {
        console.error('Error marking contact as not interested:', error);
        return throwError(() => error);
      })
    );
  }

  // Mark contact as responded
  markResponded(contactId: number): Observable<StatusChangeResponse> {
    return this.http.post<StatusChangeResponse>(
      `${this.API_URL}${AppConfig.api.contacts.responded}/${contactId}/responded`,
      {}
    ).pipe(
      tap(() => {
        this.allContactsSignal.update(contacts =>
          contacts.map(c =>
            c.id === contactId ? { ...c, status: 'Responded' as const } : c
          )
        );
        this.fetchStatistics();
      }),
      catchError(error => {
        console.error('Error marking contact as responded:', error);
        return throwError(() => error);
      })
    );
  }

  // Resend to contact (reset to pending)
  resendContact(contactId: number): Observable<StatusChangeResponse> {
    return this.http.post<StatusChangeResponse>(
      `${this.API_URL}${AppConfig.api.contacts.resend}/${contactId}/resend`,
      {}
    ).pipe(
      tap(() => {
        this.allContactsSignal.update(contacts =>
          contacts.map(c =>
            c.id === contactId ? { ...c, status: 'Pending' as const } : c
          )
        );
        this.fetchStatistics();
      }),
      catchError(error => {
        console.error('Error resending to contact:', error);
        return throwError(() => error);
      })
    );
  }

  // Resend all failed contacts (bulk reset to pending)
  resendAllFailed(): Observable<{ message: string; resent_count: number }> {
    return this.http.post<{ message: string; resent_count: number }>(
      `${this.API_URL}${AppConfig.api.contacts.resendAllFailed}`,
      {}
    ).pipe(
      tap((response) => {
        // Update local state - change all Failed contacts to Pending
        this.allContactsSignal.update(contacts =>
          contacts.map(c =>
            c.status === 'Failed' ? { ...c, status: 'Pending' as const } : c
          )
        );
        this.fetchStatistics();
      }),
      catchError(error => {
        console.error('Error resending all failed contacts:', error);
        return throwError(() => error);
      })
    );
  }

  // Resend all delivered contacts (bulk reset to pending)
  resendAllDelivered(): Observable<{ message: string; resent_count: number }> {
    return this.http.post<{ message: string; resent_count: number }>(
      `${this.API_URL}${AppConfig.api.contacts.resendAllDelivered}`,
      {}
    ).pipe(
      tap((response) => {
        // Update local state - change all Delivered contacts to Pending
        this.allContactsSignal.update(contacts =>
          contacts.map(c =>
            c.status === 'Delivered' ? { ...c, status: 'Pending' as const } : c
          )
        );
        this.fetchStatistics();
      }),
      catchError(error => {
        console.error('Error resending all delivered contacts:', error);
        return throwError(() => error);
      })
    );
  }

  // Resend all responded contacts (bulk reset to pending)
  resendAllResponded(): Observable<{ message: string; resent_count: number }> {
    return this.http.post<{ message: string; resent_count: number }>(
      `${this.API_URL}${AppConfig.api.contacts.resendAllResponded}`,
      {}
    ).pipe(
      tap((response) => {
        // Update local state - change all Responded contacts to Pending
        this.allContactsSignal.update(contacts =>
          contacts.map(c =>
            c.status === 'Responded' ? { ...c, status: 'Pending' as const } : c
          )
        );
        this.fetchStatistics();
      }),
      catchError(error => {
        console.error('Error resending all responded contacts:', error);
        return throwError(() => error);
      })
    );
  }

  // Resend all not interested contacts (bulk reset to pending)
  resendAllNotInterested(): Observable<{ message: string; resent_count: number }> {
    return this.http.post<{ message: string; resent_count: number }>(
      `${this.API_URL}${AppConfig.api.contacts.resendAllNotInterested}`,
      {}
    ).pipe(
      tap((response) => {
        // Update local state - change all NotInterested contacts to Pending
        this.allContactsSignal.update(contacts =>
          contacts.map(c =>
            c.status === 'NotInterested' ? { ...c, status: 'Pending' as const } : c
          )
        );
        this.fetchStatistics();
      }),
      catchError(error => {
        console.error('Error resending all not interested contacts:', error);
        return throwError(() => error);
      })
    );
  }

  // Map status from API (can be string or number) to ContactStatus string
  private mapStatus(status: string | number): Contact['status'] {
    // If it's already a valid string status, return it
    if (typeof status === 'string') {
      // Convert 'Sending' to 'Failed' - stuck in sending means it failed
      if (status === 'Sending') {
        return 'Failed';
      }
      const validStatuses = ['Pending', 'Sent', 'Delivered', 'Failed', 'NotValid', 'HasIssues', 'Blocked', 'NotInterested', 'Responded'];
      if (validStatuses.includes(status)) {
        return status as Contact['status'];
      }
    }

    // Map numeric status to string (matches backend ContactStatus enum)
    // Status 1 (Sending) is mapped to 'Failed' - stuck in sending means it failed
    const statusMap: Record<number, Contact['status']> = {
      0: 'Pending',
      1: 'Failed',    // Changed from 'Sending' to 'Failed'
      2: 'Sent',
      3: 'Delivered',
      4: 'Failed',
      5: 'NotValid',
      6: 'HasIssues',
      7: 'Blocked',
      8: 'NotInterested',
      9: 'Responded'
    };

    const numStatus = typeof status === 'number' ? status : parseInt(status, 10);
    return statusMap[numStatus] || 'Pending';
  }

  // Normalize gender to uppercase M/F/U (handles lowercase m/f and any other values)
  private normalizeGender(gender: string | null | undefined): 'M' | 'F' | 'U' {
    if (!gender) return 'U';
    const upper = gender.toUpperCase().trim();
    if (upper === 'M' || upper === 'MALE') return 'M';
    if (upper === 'F' || upper === 'FEMALE') return 'F';
    return 'U';
  }
}
