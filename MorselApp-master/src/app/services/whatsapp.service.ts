import { Injectable, signal, computed, inject, OnDestroy } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, Subject, interval, takeUntil, switchMap, tap, catchError, of } from 'rxjs';
import {
  BrowserType,
  InitRequest,
  StatusResultDto,
  SendBulkRequest,
  BulkControlResponse,
  BulkOperationState,
  BulkOperationStatus,
  ApiResponse
} from '../models';
import { AppConfig } from '../config';

@Injectable({
  providedIn: 'root'
})
export class WhatsAppService implements OnDestroy {
  private http = inject(HttpClient);
  private readonly API_URL = AppConfig.api.whatsappBaseUrl;
  private destroy$ = new Subject<void>();
  private pollingDestroy$ = new Subject<void>();

  // State signals
  private browserStatusSignal = signal<StatusResultDto | null>(null);
  private selectedBrowserSignal = signal<BrowserType>(this.getSavedBrowserType());
  private isInitializingSignal = signal<boolean>(false);
  private bulkOperationStateSignal = signal<BulkOperationState | null>(null);
  private isPollingSignal = signal<boolean>(false);
  private errorSignal = signal<string | null>(null);

  // Public readonly signals
  readonly browserStatus = this.browserStatusSignal.asReadonly();
  readonly selectedBrowser = this.selectedBrowserSignal.asReadonly();
  readonly isInitializing = this.isInitializingSignal.asReadonly();
  readonly bulkOperationState = this.bulkOperationStateSignal.asReadonly();
  readonly isPolling = this.isPollingSignal.asReadonly();
  readonly error = this.errorSignal.asReadonly();

  // Computed signals
  readonly isBrowserOpen = computed(() => this.browserStatusSignal()?.browserOpen ?? false);
  readonly isLoggedIn = computed(() => this.browserStatusSignal()?.loggedIn ?? false);
  readonly isReady = computed(() => this.isBrowserOpen() && this.isLoggedIn());

  readonly bulkStatus = computed<BulkOperationStatus>(() => {
    return this.bulkOperationStateSignal()?.status ?? 0;
  });

  readonly isBulkIdle = computed(() => this.bulkStatus() === 0);
  readonly isBulkRunning = computed(() => this.bulkStatus() === 1);
  readonly isBulkPaused = computed(() => this.bulkStatus() === 2);
  readonly isBulkCompleted = computed(() => this.bulkStatus() === 3);
  readonly isBulkStopped = computed(() => this.bulkStatus() === 4);

  readonly bulkProgress = computed(() => {
    const state = this.bulkOperationStateSignal();
    if (!state) {
      return {
        totalContacts: 0,
        processedContacts: 0,
        sent: 0,
        failed: 0,
        breaksTaken: 0,
        remainingContacts: 0,
        progressPercent: 0
      };
    }
    return {
      totalContacts: state.totalContacts,
      processedContacts: state.processedContacts,
      sent: state.sent,
      failed: state.failed,
      breaksTaken: state.breaksTaken,
      remainingContacts: state.remainingContacts,
      progressPercent: state.progressPercent
    };
  });

  constructor() {
    // Initialize by checking WhatsApp status
    this.checkStatus();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
    this.stopPolling();
  }

  // Browser type selection
  setBrowserType(type: BrowserType): void {
    this.selectedBrowserSignal.set(type);
    localStorage.setItem(AppConfig.storage.browserType, type);
  }

  private getSavedBrowserType(): BrowserType {
    const saved = localStorage.getItem(AppConfig.storage.browserType);
    return (saved === 'Chrome' || saved === 'Firefox') ? saved : 'Chrome';
  }

  // Check WhatsApp status
  checkStatus(): Observable<ApiResponse<StatusResultDto>> {
    return this.http.get<ApiResponse<StatusResultDto>>(
      `${this.API_URL}${AppConfig.api.whatsapp.status}`
    ).pipe(
      tap(response => {
        if (response.success && response.data) {
          this.browserStatusSignal.set(response.data);
          this.errorSignal.set(null);
        }
      }),
      catchError(error => {
        console.error('Error checking WhatsApp status:', error);
        this.errorSignal.set('Failed to check WhatsApp status');
        return of({ success: false, data: null as any, message: null, error: error.message, timestamp: new Date().toISOString() });
      })
    );
  }

  // Initialize browser
  initBrowser(browserType?: BrowserType): Observable<ApiResponse<StatusResultDto>> {
    const type = browserType || this.selectedBrowserSignal();
    this.isInitializingSignal.set(true);
    this.errorSignal.set(null);

    const request: InitRequest = { browserType: type };

    return this.http.post<ApiResponse<StatusResultDto>>(
      `${this.API_URL}${AppConfig.api.whatsapp.init}`,
      request
    ).pipe(
      tap(response => {
        this.isInitializingSignal.set(false);
        if (response.success && response.data) {
          this.browserStatusSignal.set(response.data);
          this.setBrowserType(type);
        } else {
          this.errorSignal.set(response.message || 'Failed to initialize browser');
        }
      }),
      catchError(error => {
        this.isInitializingSignal.set(false);
        console.error('Error initializing browser:', error);
        this.errorSignal.set(error.error?.message || 'Failed to initialize browser');
        return of({ success: false, data: null as any, message: null, error: error.message, timestamp: new Date().toISOString() });
      })
    );
  }

  // Close browser
  closeBrowser(): Observable<ApiResponse<string>> {
    return this.http.post<ApiResponse<string>>(
      `${this.API_URL}${AppConfig.api.whatsapp.close}`,
      {}
    ).pipe(
      tap(response => {
        if (response.success) {
          this.browserStatusSignal.set({
            browserOpen: false,
            loggedIn: false,
            browserType: null,
            message: 'Browser closed'
          });
        }
      }),
      catchError(error => {
        console.error('Error closing browser:', error);
        return of({ success: false, data: '', message: null, error: error.message, timestamp: new Date().toISOString() });
      })
    );
  }

  // Start bulk operation
  startBulkOperation(request: SendBulkRequest): Observable<ApiResponse<BulkControlResponse>> {
    this.errorSignal.set(null);

    return this.http.post<ApiResponse<BulkControlResponse>>(
      `${this.API_URL}${AppConfig.api.whatsapp.bulk.start}`,
      request
    ).pipe(
      tap(response => {
        if (response.success && response.data) {
          this.bulkOperationStateSignal.set(response.data.state);
          // Start polling for status updates
          this.startPolling();
        } else {
          this.errorSignal.set(response.message || response.data?.message || 'Failed to start bulk operation');
        }
      }),
      catchError(error => {
        console.error('Error starting bulk operation:', error);
        this.errorSignal.set(error.error?.message || 'Failed to start bulk operation');
        return of({ success: false, data: null as any, message: null, error: error.message, timestamp: new Date().toISOString() });
      })
    );
  }

  // Get bulk status
  getBulkStatus(): Observable<ApiResponse<BulkControlResponse>> {
    return this.http.get<ApiResponse<BulkControlResponse>>(
      `${this.API_URL}${AppConfig.api.whatsapp.bulk.status}`
    ).pipe(
      tap(response => {
        if (response.success && response.data) {
          this.bulkOperationStateSignal.set(response.data.state);
          // Stop polling if operation is completed or stopped
          if (response.data.state.status === 3 || response.data.state.status === 4) {
            this.stopPolling();
          }
        }
      }),
      catchError(error => {
        console.error('Error getting bulk status:', error);
        return of({ success: false, data: null as any, message: null, error: error.message, timestamp: new Date().toISOString() });
      })
    );
  }

  // Pause bulk operation
  pauseBulkOperation(): Observable<ApiResponse<BulkControlResponse>> {
    return this.http.post<ApiResponse<BulkControlResponse>>(
      `${this.API_URL}${AppConfig.api.whatsapp.bulk.pause}`,
      {}
    ).pipe(
      tap(response => {
        if (response.success && response.data) {
          this.bulkOperationStateSignal.set(response.data.state);
          this.stopPolling();
        } else {
          this.errorSignal.set(response.message || 'Failed to pause operation');
        }
      }),
      catchError(error => {
        console.error('Error pausing bulk operation:', error);
        this.errorSignal.set(error.error?.message || 'Failed to pause operation');
        return of({ success: false, data: null as any, message: null, error: error.message, timestamp: new Date().toISOString() });
      })
    );
  }

  // Resume bulk operation
  resumeBulkOperation(): Observable<ApiResponse<BulkControlResponse>> {
    return this.http.post<ApiResponse<BulkControlResponse>>(
      `${this.API_URL}${AppConfig.api.whatsapp.bulk.resume}`,
      {}
    ).pipe(
      tap(response => {
        if (response.success && response.data) {
          this.bulkOperationStateSignal.set(response.data.state);
          this.startPolling();
        } else {
          this.errorSignal.set(response.message || 'Failed to resume operation');
        }
      }),
      catchError(error => {
        console.error('Error resuming bulk operation:', error);
        this.errorSignal.set(error.error?.message || 'Failed to resume operation');
        return of({ success: false, data: null as any, message: null, error: error.message, timestamp: new Date().toISOString() });
      })
    );
  }

  // Stop bulk operation
  stopBulkOperation(): Observable<ApiResponse<BulkControlResponse>> {
    return this.http.post<ApiResponse<BulkControlResponse>>(
      `${this.API_URL}${AppConfig.api.whatsapp.bulk.stop}`,
      {}
    ).pipe(
      tap(response => {
        if (response.success && response.data) {
          this.bulkOperationStateSignal.set(response.data.state);
          this.stopPolling();
        } else {
          this.errorSignal.set(response.message || 'Failed to stop operation');
        }
      }),
      catchError(error => {
        console.error('Error stopping bulk operation:', error);
        this.errorSignal.set(error.error?.message || 'Failed to stop operation');
        return of({ success: false, data: null as any, message: null, error: error.message, timestamp: new Date().toISOString() });
      })
    );
  }

  // Polling management
  startPolling(): void {
    if (this.isPollingSignal()) return;

    this.isPollingSignal.set(true);
    this.pollingDestroy$ = new Subject<void>();

    interval(AppConfig.settings.bulkStatusPollingInterval).pipe(
      takeUntil(this.pollingDestroy$),
      switchMap(() => this.getBulkStatus())
    ).subscribe();
  }

  stopPolling(): void {
    this.isPollingSignal.set(false);
    this.pollingDestroy$.next();
    this.pollingDestroy$.complete();
  }

  // Reset state
  resetBulkState(): void {
    this.bulkOperationStateSignal.set(null);
    this.errorSignal.set(null);
    this.stopPolling();
  }

  clearError(): void {
    this.errorSignal.set(null);
  }
}
