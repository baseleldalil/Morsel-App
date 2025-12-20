import { Component, inject, signal, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { CampaignService, ContactsService, ToastService } from '../../services';
import { BrowserType } from '../../models';

@Component({
  selector: 'app-campaign-progress',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './campaign-progress.html',
  styleUrl: './campaign-progress.css'
})
export class CampaignProgressComponent implements OnInit, OnDestroy {
  private campaignService = inject(CampaignService);
  private contactsService = inject(ContactsService);
  private toastService = inject(ToastService);

  // Countdown timer
  private countdownInterval: ReturnType<typeof setInterval> | null = null;
  breakCountdown = signal<string>('--:--');

  // Expose signals
  readonly status = this.campaignService.status;
  readonly progress = this.campaignService.progress;
  readonly isRunning = this.campaignService.isRunning;
  readonly isPaused = this.campaignService.isPaused;
  readonly isIdle = this.campaignService.isIdle;
  readonly isInitializing = this.campaignService.isInitializing;
  readonly isCompleted = this.campaignService.isCompleted;
  readonly isStopping = this.campaignService.isStopping;
  readonly canStart = this.campaignService.canStart;
  readonly selectedCount = this.contactsService.selectedCount;

  // WhatsApp status signals
  readonly selectedBrowser = this.campaignService.selectedBrowser;
  readonly isBrowserOpen = this.campaignService.isBrowserOpen;
  readonly isLoggedIn = this.campaignService.isLoggedIn;
  readonly isWhatsAppReady = this.campaignService.isWhatsAppReady;
  readonly isInitializingBrowser = this.campaignService.isInitializingBrowser;
  readonly whatsappError = this.campaignService.whatsappError;

  // Message signal from campaign service
  readonly message = this.campaignService.message;

  // Message validation signals
  readonly messagesValid = this.campaignService.messagesValid;
  readonly messageValidationErrors = this.campaignService.messageValidationErrors;

  // Break state signals
  readonly isOnBreak = this.campaignService.isOnBreak;
  readonly breakEndsAt = this.campaignService.breakEndsAt;
  readonly breakTriggeredAtMessage = this.campaignService.breakTriggeredAtMessage;
  readonly breakDurationMinutes = this.campaignService.breakDurationMinutes;
  readonly nextBreakAfterMessages = this.campaignService.nextBreakAfterMessages;
  readonly messagesSinceLastBreak = this.campaignService.messagesSinceLastBreak;

  get progressPercent(): number {
    const p = this.progress();
    if (p.selectedContacts === 0) return 0;
    return Math.round((p.sent / p.selectedContacts) * 100);
  }

  get processedCount(): number {
    const p = this.progress();
    return p.sent + p.failed;
  }

  hasMessage(): boolean {
    const msg = this.message();
    return msg.maleMessage.length > 0 || msg.femaleMessage.length > 0 || msg.attachments.length > 0;
  }

  get browserStatusText(): string {
    if (this.isInitializingBrowser()) return 'Initializing...';
    if (!this.isBrowserOpen()) return 'Browser not open';
    if (!this.isLoggedIn()) return 'Scan QR code to login';
    return 'Ready';
  }

  get browserStatusClass(): string {
    if (this.isInitializingBrowser()) return 'initializing';
    if (!this.isBrowserOpen()) return 'closed';
    if (!this.isLoggedIn()) return 'waiting';
    return 'ready';
  }

  setBrowserType(type: BrowserType): void {
    this.campaignService.setBrowserType(type);
  }

  initBrowser(): void {
    this.campaignService.initBrowser();
  }

  closeBrowser(): void {
    this.campaignService.closeBrowser();
  }

  startCampaign(): void {
    // Validate before starting
    if (this.selectedCount() === 0) {
      this.toastService.warning('Please select at least one contact');
      return;
    }

    if (!this.isBrowserOpen()) {
      this.toastService.warning('Please initialize browser first');
      return;
    }

    if (!this.isLoggedIn()) {
      this.toastService.warning('Please scan QR code to login WhatsApp');
      return;
    }

    if (!this.hasMessage()) {
      this.toastService.warning('Please compose a message or add attachments');
      return;
    }

    // Check message validation (placeholders and name variables)
    if (!this.messagesValid()) {
      const errors = this.messageValidationErrors();
      if (errors.length > 0) {
        this.toastService.error(`Message validation failed: ${errors[0]}`);
      } else {
        this.toastService.error('Messages must have at least 3 placeholders {} and include {arabic_name} or {english_name}');
      }
      return;
    }

    // All validations passed, start campaign
    this.campaignService.startCampaign();
  }

  pauseCampaign(): void {
    this.campaignService.pauseCampaign();
  }

  resumeCampaign(): void {
    this.campaignService.resumeCampaign();
  }

  stopCampaign(): void {
    this.campaignService.stopCampaign();
  }

  resetProgress(): void {
    this.campaignService.resetProgress();
  }

  ngOnInit(): void {
    // Start countdown timer that updates every second
    this.countdownInterval = setInterval(() => {
      this.updateCountdown();
    }, 1000);
  }

  ngOnDestroy(): void {
    if (this.countdownInterval) {
      clearInterval(this.countdownInterval);
    }
  }

  private updateCountdown(): void {
    const breakEndsAt = this.breakEndsAt();
    if (!breakEndsAt || !this.isOnBreak()) {
      this.breakCountdown.set('--:--');
      return;
    }

    const endTime = new Date(breakEndsAt).getTime();
    const now = Date.now();
    const diff = endTime - now;

    if (diff <= 0) {
      this.breakCountdown.set('00:00');
      return;
    }

    const minutes = Math.floor(diff / 60000);
    const seconds = Math.floor((diff % 60000) / 1000);
    this.breakCountdown.set(
      `${minutes.toString().padStart(2, '0')}:${seconds.toString().padStart(2, '0')}`
    );
  }
}
