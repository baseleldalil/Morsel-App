import { Injectable, signal, computed, inject, effect } from '@angular/core';
import {
  CampaignProgress,
  CampaignMessage,
  CampaignStatus,
  TimingSettings,
  Attachment,
  BrowserType,
  SendBulkRequest,
  ContactDto,
  AttachmentDto,
  BulkOperationStatus,
  DelaySettingsDto,
  BreakSettingsDto
} from '../models';
import { WhatsAppService } from './whatsapp.service';
import { ContactsService } from './contacts.service';
import { ToastService } from './toast.service';

@Injectable({
  providedIn: 'root'
})
export class CampaignService {
  private whatsappService = inject(WhatsAppService);
  private contactsService = inject(ContactsService);
  private toastService = inject(ToastService);

  // State signals
  private statusSignal = signal<CampaignStatus>('idle');
  private messageSignal = signal<CampaignMessage>({
    maleMessage: '',
    femaleMessage: '',
    attachments: []
  });
  private templateNameSignal = signal<string>('');
  private templateDescriptionSignal = signal<string>('');
  private editingTemplateIdSignal = signal<number | null>(null);
  private progressSignal = signal<CampaignProgress>({
    selectedContacts: 0,
    sent: 0,
    delivered: 0,
    failed: 0,
    pending: 0,
    remaining: 0,
    inProgress: 0,
    successRate: 0,
    estimatedTime: 'Calculating...',
    speed: 0,
    breaksTaken: 0
  });
  private timingSignal = signal<TimingSettings>({
    messageDelayMin: 30,  // Minimum 30 seconds enforced
    messageDelayMax: 60,
    randomBreaksEnabled: true,  // Always enabled
    messagesCountMin: 13,
    messagesCountMax: 20,  // Maximum 35 messages enforced
    breakDurationMin: 3,  // 3 minutes minimum break
    breakDurationMax: 9,
    decimalRandomEnabled: true,  // Always enabled
    precision: 0.1
  });
  private startTimeSignal = signal<Date | null>(null);

  // Public readonly signals
  readonly status = this.statusSignal.asReadonly();
  readonly message = this.messageSignal.asReadonly();
  readonly progress = this.progressSignal.asReadonly();
  readonly timing = this.timingSignal.asReadonly();
  readonly templateName = this.templateNameSignal.asReadonly();
  readonly templateDescription = this.templateDescriptionSignal.asReadonly();
  readonly editingTemplateId = this.editingTemplateIdSignal.asReadonly();

  // WhatsApp service signals exposed
  readonly selectedBrowser = this.whatsappService.selectedBrowser;
  readonly isBrowserOpen = this.whatsappService.isBrowserOpen;
  readonly isLoggedIn = this.whatsappService.isLoggedIn;
  readonly isWhatsAppReady = this.whatsappService.isReady;
  readonly isInitializing = this.whatsappService.isInitializing;
  readonly whatsappError = this.whatsappService.error;
  readonly bulkOperationState = this.whatsappService.bulkOperationState;

  // Break state computed signals
  readonly isOnBreak = computed(() => this.bulkOperationState()?.isOnBreak ?? false);
  readonly breakEndsAt = computed(() => this.bulkOperationState()?.breakEndsAt ?? null);
  readonly breakTriggeredAtMessage = computed(() => this.bulkOperationState()?.breakTriggeredAtMessage ?? 0);
  readonly breakDurationMinutes = computed(() => this.bulkOperationState()?.breakDurationMinutes ?? 0);
  readonly nextBreakAfterMessages = computed(() => this.bulkOperationState()?.nextBreakAfterMessages ?? 0);
  readonly messagesSinceLastBreak = computed(() => this.bulkOperationState()?.messagesSinceLastBreak ?? 0);

  // Computed signals
  readonly maleMessage = computed(() => this.messageSignal().maleMessage);
  readonly femaleMessage = computed(() => this.messageSignal().femaleMessage);
  readonly attachments = computed(() => this.messageSignal().attachments);

  readonly isRunning = computed(() => this.statusSignal() === 'running');
  readonly isPaused = computed(() => this.statusSignal() === 'paused');
  readonly isIdle = computed(() => this.statusSignal() === 'idle');
  readonly isInitializingBrowser = computed(() => this.statusSignal() === 'initializing');
  readonly isCompleted = computed(() => this.statusSignal() === 'completed');
  readonly isStopping = computed(() => this.statusSignal() === 'stopping');

  readonly maleMessageLength = computed(() => this.messageSignal().maleMessage.length);
  readonly femaleMessageLength = computed(() => this.messageSignal().femaleMessage.length);

  // Message validation constants
  private readonly requiredOtherPlaceholders = 3;
  private readonly namePlaceholders = ['{arabic_name}', '{english_name}'];

  // Message validation computed signals
  readonly maleMessageValid = computed(() => {
    const msg = this.messageSignal().maleMessage;
    return this.isMessageValid(msg);
  });

  readonly femaleMessageValid = computed(() => {
    const msg = this.messageSignal().femaleMessage;
    return this.isMessageValid(msg);
  });

  readonly messagesValid = computed(() => {
    const msg = this.messageSignal();
    const hasMale = msg.maleMessage.length > 0;
    const hasFemale = msg.femaleMessage.length > 0;

    // If both empty, not valid (no message)
    if (!hasMale && !hasFemale) return false;

    // Validate only non-empty messages
    const maleValid = !hasMale || this.maleMessageValid();
    const femaleValid = !hasFemale || this.femaleMessageValid();

    return maleValid && femaleValid;
  });

  readonly messageValidationErrors = computed(() => {
    const errors: string[] = [];
    const msg = this.messageSignal();

    if (msg.maleMessage.length > 0 && !this.maleMessageValid()) {
      const maleErrors = this.getMessageValidationErrors(msg.maleMessage);
      errors.push(...maleErrors.map(e => `Male: ${e}`));
    }

    if (msg.femaleMessage.length > 0 && !this.femaleMessageValid()) {
      const femaleErrors = this.getMessageValidationErrors(msg.femaleMessage);
      errors.push(...femaleErrors.map(e => `Female: ${e}`));
    }

    return errors;
  });

  private isMessageValid(message: string): boolean {
    if (message.length === 0) return true;

    const placeholderRegex = /\{[^}]+\}/g;
    const placeholders = message.match(placeholderRegex) || [];
    const hasArabicName = message.includes('{arabic_name}');
    const hasEnglishName = message.includes('{english_name}');
    const otherPlaceholders = placeholders.filter(p => !this.namePlaceholders.includes(p));

    return (hasArabicName || hasEnglishName) &&
           otherPlaceholders.length >= this.requiredOtherPlaceholders;
  }

  private getMessageValidationErrors(message: string): string[] {
    const errors: string[] = [];
    const placeholderRegex = /\{[^}]+\}/g;
    const placeholders = message.match(placeholderRegex) || [];
    const hasArabicName = message.includes('{arabic_name}');
    const hasEnglishName = message.includes('{english_name}');
    const otherPlaceholders = placeholders.filter(p => !this.namePlaceholders.includes(p));

    if (!hasArabicName && !hasEnglishName) {
      errors.push('Must include {arabic_name} or {english_name}');
    }
    if (otherPlaceholders.length < this.requiredOtherPlaceholders) {
      errors.push(`Need at least ${this.requiredOtherPlaceholders} randomization placeholders (found ${otherPlaceholders.length})`);
    }

    return errors;
  }

  // Count of selected contacts (all selectable contacts)
  readonly selectedContactsCount = computed(() => {
    return this.contactsService.selectedContacts().length;
  });

  readonly canStart = computed(() => {
    const msg = this.messageSignal();
    const hasMessage = msg.maleMessage.length > 0 || msg.femaleMessage.length > 0 || msg.attachments.length > 0;
    const hasSelectedContacts = this.selectedContactsCount() > 0;
    const isIdle = this.statusSignal() === 'idle' || this.statusSignal() === 'completed';
    const isValidMessage = this.messagesValid();
    return hasMessage && hasSelectedContacts && isIdle && isValidMessage;
  });

  constructor() {
    // Effect to sync WhatsApp bulk operation state with campaign progress
    effect(() => {
      const bulkState = this.whatsappService.bulkOperationState();
      if (bulkState) {
        this.syncProgressFromBulkState(bulkState.status, {
          totalContacts: bulkState.totalContacts,
          processedContacts: bulkState.processedContacts,
          sent: bulkState.sent,
          failed: bulkState.failed,
          breaksTaken: bulkState.breaksTaken,
          remainingContacts: bulkState.remainingContacts,
          progressPercent: bulkState.progressPercent
        });
      }
    });
  }

  private syncProgressFromBulkState(
    bulkStatus: BulkOperationStatus,
    progress: {
      totalContacts: number;
      processedContacts: number;
      sent: number;
      failed: number;
      breaksTaken: number;
      remainingContacts: number;
      progressPercent: number;
    }
  ): void {
    // Map bulk status to campaign status
    const statusMap: Record<BulkOperationStatus, CampaignStatus> = {
      0: 'idle',
      1: 'running',
      2: 'paused',
      3: 'completed',
      4: 'idle'
    };
    this.statusSignal.set(statusMap[bulkStatus]);

    // Calculate speed and estimated time
    const startTime = this.startTimeSignal();
    let speed = 0;
    let estimatedTime = 'Calculating...';

    if (startTime && progress.sent > 0) {
      const elapsedMinutes = (Date.now() - startTime.getTime()) / 60000;
      speed = Math.round(progress.sent / elapsedMinutes);
      if (speed > 0 && progress.remainingContacts > 0) {
        const remainingMinutes = progress.remainingContacts / speed;
        if (remainingMinutes < 1) {
          estimatedTime = 'Less than 1 min';
        } else if (remainingMinutes < 60) {
          estimatedTime = `${Math.round(remainingMinutes)} min`;
        } else {
          const hours = Math.floor(remainingMinutes / 60);
          const mins = Math.round(remainingMinutes % 60);
          estimatedTime = `${hours}h ${mins}m`;
        }
      }
    }

    if (bulkStatus === 3) {
      estimatedTime = 'Completed';
      // Check if there's a pending female campaign to start (with guard to prevent multiple triggers)
      if (this.pendingFemaleRequest && !this.femaleScheduled) {
        this.femaleScheduled = true;
        console.log(`[Campaign] Male campaign completed. Starting pending female campaign with ${this.pendingFemaleRequest.contacts.length} contacts...`);
        this.toastService.info(`Male contacts done. Starting female campaign (${this.pendingFemaleRequest.contacts.length} contacts)...`);
        // Use a slight delay to ensure state is fully updated, then start female campaign
        setTimeout(() => {
          this.femaleScheduled = false;
          if (this.pendingFemaleRequest) {
            this.startPendingFemaleCampaign();
          }
        }, 1500);
      } else if (!this.pendingFemaleRequest) {
        console.log('[Campaign] Campaign completed. No pending female campaign.');
      }
    } else if (bulkStatus === 4) {
      estimatedTime = 'Stopped';
      // Clear pending request if stopped
      if (this.pendingFemaleRequest) {
        console.log('[Campaign] Campaign stopped. Clearing pending female campaign.');
        this.toastService.warning('Campaign stopped. Female contacts will not be sent.');
      }
      this.pendingFemaleRequest = null;
      this.femaleScheduled = false;
    }

    // Update progress
    this.progressSignal.set({
      selectedContacts: progress.totalContacts,
      sent: progress.sent,
      delivered: progress.sent, // Assuming sent = delivered for now
      failed: progress.failed,
      pending: progress.remainingContacts,
      remaining: progress.remainingContacts,
      inProgress: bulkStatus === 1 ? 1 : 0,
      successRate: progress.totalContacts > 0 ? Math.round((progress.sent / progress.totalContacts) * 100) : 0,
      estimatedTime,
      speed,
      breaksTaken: progress.breaksTaken
    });
  }

  // Message Actions
  setMaleMessage(message: string): void {
    this.messageSignal.update(m => ({ ...m, maleMessage: message }));
  }

  setFemaleMessage(message: string): void {
    this.messageSignal.update(m => ({ ...m, femaleMessage: message }));
  }

  addAttachment(attachment: Omit<Attachment, 'id'>): void {
    const newAttachment: Attachment = {
      ...attachment,
      id: Date.now()
    };
    this.messageSignal.update(m => ({
      ...m,
      attachments: [...m.attachments, newAttachment]
    }));
  }

  removeAttachment(id: number): void {
    this.messageSignal.update(m => ({
      ...m,
      attachments: m.attachments.filter(a => a.id !== id)
    }));
  }

  clearAttachments(): void {
    this.messageSignal.update(m => ({ ...m, attachments: [] }));
  }

  // Browser Actions
  setBrowserType(type: BrowserType): void {
    this.whatsappService.setBrowserType(type);
  }

  initBrowser(browserType?: BrowserType): void {
    this.statusSignal.set('initializing');
    this.whatsappService.initBrowser(browserType).subscribe({
      next: (response) => {
        if (response.success) {
          this.toastService.success('Browser initialized successfully');
          this.statusSignal.set('idle');
        } else {
          this.toastService.error(response.message || 'Failed to initialize browser');
          this.statusSignal.set('idle');
        }
      },
      error: (error) => {
        this.toastService.error('Failed to initialize browser');
        this.statusSignal.set('idle');
      }
    });
  }

  // Campaign Actions
  // Store pending female contacts for sequential processing
  private pendingFemaleRequest: SendBulkRequest | null = null;
  // Guard to prevent multiple female campaign triggers
  private femaleScheduled = false;

  startCampaign(): void {
    // Validations are done in component, but keep as safety check
    if (!this.whatsappService.isReady()) {
      return;
    }

    // Get ALL selected contacts - no status filtering
    // HasIssues and NotValid contacts already have disabled checkboxes, so they can't be selected
    const selectedContacts = this.contactsService.selectedContacts();
    if (selectedContacts.length === 0) {
      this.toastService.warning('No contacts selected. Please select contacts to start the campaign.');
      return;
    }

    // Validate all selected contacts have valid gender (M or F required)
    const invalidGenderContacts: string[] = [];
    const genderCounts = { M: 0, F: 0, invalid: 0 };

    selectedContacts.forEach(c => {
      const g = (c.gender || '').toUpperCase().trim();
      if (g === 'M') {
        genderCounts.M++;
      } else if (g === 'F') {
        genderCounts.F++;
      } else {
        genderCounts.invalid++;
        invalidGenderContacts.push(`${c.firstName || c.arabicName || c.englishName || 'Unknown'} (${c.number})`);
      }
    });

    console.log(`[Campaign] Gender breakdown: Male=${genderCounts.M}, Female=${genderCounts.F}, Invalid=${genderCounts.invalid}`);
    console.log(`[Campaign] Total selected: ${selectedContacts.length}`);

    // Block campaign if any contacts have invalid gender
    if (genderCounts.invalid > 0) {
      const maxShow = 3;
      const contactList = invalidGenderContacts.slice(0, maxShow).join(', ');
      const moreText = invalidGenderContacts.length > maxShow ? ` and ${invalidGenderContacts.length - maxShow} more` : '';
      this.toastService.error(`${genderCounts.invalid} contact(s) have invalid gender (must be M or F): ${contactList}${moreText}`);
      return;
    }

    const msg = this.messageSignal();
    const timing = this.timingSignal();

    // Validate messages before starting campaign
    const maleValidation = this.validateMessage(msg.maleMessage);
    const femaleValidation = this.validateMessage(msg.femaleMessage);

    // Check if at least one message is provided
    const hasMaleMessage = msg.maleMessage.trim().length > 0;
    const hasFemaleMessage = msg.femaleMessage.trim().length > 0;

    if (!hasMaleMessage && !hasFemaleMessage) {
      this.toastService.error('Please enter at least one message before starting campaign');
      return;
    }

    // Validate male message if provided
    if (hasMaleMessage && !maleValidation.isValid) {
      this.toastService.error(`Male message invalid: ${maleValidation.errors.join('. ')}`);
      return;
    }

    // Validate female message if provided
    if (hasFemaleMessage && !femaleValidation.isValid) {
      this.toastService.error(`Female message invalid: ${femaleValidation.errors.join('. ')}`);
      return;
    }

    // If both messages are provided, they must be different
    if (hasMaleMessage && hasFemaleMessage) {
      if (msg.maleMessage.trim() === msg.femaleMessage.trim()) {
        this.toastService.error('Male and Female messages must be different. Use only one message field if sending same content to all.');
        return;
      }
    }

    // Prepare attachments
    const attachments: AttachmentDto[] = msg.attachments
      .filter(a => a.base64)
      .map(a => ({
        base64: a.base64!,
        fileName: a.name,
        mediaType: this.getMediaType(a.type),
        caption: undefined
      }));

    // DEBUG: Log message state
    console.log(`[Campaign] Message check: hasMale=${hasMaleMessage}, hasFemale=${hasFemaleMessage}`);
    console.log(`[Campaign] Male message length: ${msg.maleMessage.length}, Female message length: ${msg.femaleMessage.length}`);

    // Determine if we need gender-based separation:
    // ONLY split by gender if BOTH messages exist AND they are DIFFERENT
    // If only one message is provided, send that message to ALL contacts
    const needsGenderSeparation = hasMaleMessage && hasFemaleMessage && msg.maleMessage !== msg.femaleMessage;
    console.log(`[Campaign] needsGenderSeparation: ${needsGenderSeparation}`);

    // Set start time for speed calculation
    this.startTimeSignal.set(new Date());
    this.statusSignal.set('running');
    // Reset female campaign guard for new campaign
    this.femaleScheduled = false;

    // Initialize progress
    const totalContacts = selectedContacts.length;
    this.progressSignal.set({
      selectedContacts: totalContacts,
      sent: 0,
      delivered: 0,
      failed: 0,
      pending: totalContacts,
      remaining: totalContacts,
      inProgress: 1,
      successRate: 0,
      estimatedTime: 'Calculating...',
      speed: 0,
      breaksTaken: 0
    });

    // Build common settings
    const delaySettings: DelaySettingsDto = {
      minDelaySeconds: timing.messageDelayMin,
      maxDelaySeconds: timing.messageDelayMax
    };

    const breakSettings: BreakSettingsDto | undefined = timing.randomBreaksEnabled ? {
      enabled: true,
      minBreakAfterMessages: timing.messagesCountMin,
      maxBreakAfterMessages: timing.messagesCountMax,
      minBreakMinutes: timing.breakDurationMin,
      maxBreakMinutes: timing.breakDurationMax
    } : undefined;

    // If BOTH messages exist and are DIFFERENT, we need to handle gender-based messages
    // Since backend takes single message per request, we combine all contacts and let backend
    // pick the right message based on gender using maleMessage and femaleMessage fields
    if (needsGenderSeparation) {
      // Build contact DTOs for all selected contacts
      const contacts: ContactDto[] = selectedContacts.map(c => ({
        id: c.id,
        phone: c.number,
        name: c.firstName || c.arabicName || c.englishName,
        arabicName: c.arabicName,
        englishName: c.englishName,
        gender: c.gender
      }));

      // Count by gender for logging
      const maleCount = selectedContacts.filter(c => (c.gender || '').toUpperCase() === 'M').length;
      const femaleCount = selectedContacts.filter(c => (c.gender || '').toUpperCase() === 'F').length;
      console.log(`[Campaign] Sending to ALL contacts: ${maleCount} male, ${femaleCount} female, ${contacts.length} total`);

      // Clear any pending request
      this.pendingFemaleRequest = null;

      // Send all contacts in one request with both messages - backend will pick based on gender
      const request: SendBulkRequest = {
        contacts,
        message: msg.maleMessage,  // Default message
        maleMessage: msg.maleMessage,
        femaleMessage: msg.femaleMessage,
        attachments: attachments.length > 0 ? attachments : undefined,
        delaySettings,
        breakSettings
      };

      console.log(`[Campaign] Starting campaign with ${contacts.length} contacts (male msg + female msg)`);

      this.whatsappService.startBulkOperation(request).subscribe({
        next: (response) => {
          if (response.success) {
            this.toastService.success(`Campaign started for ${contacts.length} contacts (${maleCount} male, ${femaleCount} female)`);
          } else {
            this.toastService.error(response.message || 'Failed to start campaign');
            this.statusSignal.set('idle');
          }
        },
        error: (error) => {
          this.toastService.error('Failed to start campaign');
          this.statusSignal.set('idle');
        }
      });
    } else {
      // Same message for all - single bulk request (NO gender separation)
      // Backend handles {opt1-opt2-opt3} randomization and {arabic_name}, {english_name} variables
      console.log(`[Campaign] NO gender separation - sending same message to ALL ${selectedContacts.length} contacts`);

      const contacts: ContactDto[] = selectedContacts.map(c => ({
        id: c.id,
        phone: c.number,
        name: c.firstName || c.arabicName || c.englishName,
        arabicName: c.arabicName,
        englishName: c.englishName,
        gender: c.gender
      }));

      const message = msg.maleMessage || msg.femaleMessage;
      console.log(`[Campaign] Using message: ${message ? message.substring(0, 50) + '...' : '(no message)'}`);

      const request: SendBulkRequest = {
        contacts,
        message: message || undefined,
        attachments: attachments.length > 0 ? attachments : undefined,
        delaySettings,
        breakSettings
      };

      this.whatsappService.startBulkOperation(request).subscribe({
        next: (response) => {
          if (response.success) {
            console.log(`[Campaign] Campaign started successfully for ALL ${contacts.length} contacts`);
            this.toastService.success('Campaign started successfully');
          } else {
            console.error('[Campaign] Failed to start campaign:', response.message);
            this.toastService.error(response.message || 'Failed to start campaign');
            this.statusSignal.set('idle');
          }
        },
        error: (error) => {
          console.error('[Campaign] Error starting campaign:', error);
          this.toastService.error('Failed to start campaign');
          this.statusSignal.set('idle');
        }
      });
    }
  }

  // Start pending female campaign after male completes
  startPendingFemaleCampaign(): void {
    if (!this.pendingFemaleRequest) {
      console.log('[Campaign] startPendingFemaleCampaign called but no pending request');
      return;
    }

    const request = this.pendingFemaleRequest;
    const contactCount = request.contacts.length;
    this.pendingFemaleRequest = null;

    console.log(`[Campaign] Starting female campaign with ${contactCount} contacts`);
    this.toastService.info(`Starting campaign for ${contactCount} female contacts...`);

    // Reset start time for the female campaign
    this.startTimeSignal.set(new Date());
    this.statusSignal.set('running');

    this.whatsappService.startBulkOperation(request).subscribe({
      next: (response) => {
        if (response.success) {
          console.log(`[Campaign] Female campaign started successfully`);
          this.toastService.success(`Female contacts campaign started (${contactCount} contacts)`);
        } else {
          console.error('[Campaign] Failed to start female campaign:', response.message);
          this.toastService.error(response.message || 'Failed to start female campaign');
          this.statusSignal.set('idle');
        }
      },
      error: (error) => {
        console.error('[Campaign] Error starting female campaign:', error);
        this.toastService.error('Failed to start female campaign');
        this.statusSignal.set('idle');
      }
    });
  }

  // Check if there's a pending female campaign
  hasPendingFemaleCampaign(): boolean {
    return this.pendingFemaleRequest !== null;
  }

  private getMediaType(type: string): 'image' | 'video' | 'document' {
    if (type.startsWith('image/')) return 'image';
    if (type.startsWith('video/')) return 'video';
    return 'document';
  }

  pauseCampaign(): void {
    this.whatsappService.pauseBulkOperation().subscribe({
      next: (response) => {
        if (response.success) {
          this.toastService.info('Campaign paused');
        } else {
          this.toastService.error(response.message || 'Failed to pause campaign');
        }
      },
      error: () => {
        this.toastService.error('Failed to pause campaign');
      }
    });
  }

  resumeCampaign(): void {
    this.whatsappService.resumeBulkOperation().subscribe({
      next: (response) => {
        if (response.success) {
          this.toastService.success('Campaign resumed');
        } else {
          this.toastService.error(response.message || 'Failed to resume campaign');
        }
      },
      error: () => {
        this.toastService.error('Failed to resume campaign');
      }
    });
  }

  stopCampaign(): void {
    this.statusSignal.set('stopping');
    this.whatsappService.stopBulkOperation().subscribe({
      next: (response) => {
        if (response.success) {
          this.toastService.info('Campaign stopped');
          this.statusSignal.set('idle');
        } else {
          this.toastService.error(response.message || 'Failed to stop campaign');
          this.statusSignal.set('idle');
        }
      },
      error: () => {
        this.toastService.error('Failed to stop campaign');
        this.statusSignal.set('idle');
      }
    });
  }

  resetProgress(): void {
    this.progressSignal.set({
      selectedContacts: 0,
      sent: 0,
      delivered: 0,
      failed: 0,
      pending: 0,
      remaining: 0,
      inProgress: 0,
      successRate: 0,
      estimatedTime: 'Calculating...',
      speed: 0,
      breaksTaken: 0
    });
    this.startTimeSignal.set(null);
    this.whatsappService.resetBulkState();
  }

  updateProgress(updates: Partial<CampaignProgress>): void {
    this.progressSignal.update(p => ({ ...p, ...updates }));
  }

  // Check WhatsApp status
  checkWhatsAppStatus(): void {
    this.whatsappService.checkStatus().subscribe();
  }

  // Close browser
  closeBrowser(): void {
    this.whatsappService.closeBrowser().subscribe({
      next: (response) => {
        if (response.success) {
          this.toastService.info('Browser closed');
        }
      }
    });
  }

  // Timing Actions
  updateTiming(updates: Partial<TimingSettings>): void {
    this.timingSignal.update(t => ({ ...t, ...updates }));
  }

  resetTiming(): void {
    this.timingSignal.set({
      messageDelayMin: 30,  // Minimum 30 seconds enforced
      messageDelayMax: 60,
      randomBreaksEnabled: true,  // Always enabled
      messagesCountMin: 13,
      messagesCountMax: 20,  // Maximum 35 messages enforced
      breakDurationMin: 3,  // 3 minutes minimum break
      breakDurationMax: 9,
      decimalRandomEnabled: true,  // Always enabled
      precision: 0.1
    });
  }

  // Template Integration
  applyTemplate(maleMessage: string, femaleMessage?: string): void {
    this.messageSignal.update(m => ({
      ...m,
      maleMessage,
      femaleMessage: femaleMessage || maleMessage
    }));
  }

  setTemplateName(name: string): void {
    this.templateNameSignal.set(name);
  }

  setTemplateDescription(description: string): void {
    this.templateDescriptionSignal.set(description);
  }

  clearTemplateInfo(): void {
    this.templateNameSignal.set('');
    this.templateDescriptionSignal.set('');
    this.editingTemplateIdSignal.set(null);
  }

  // Template Editing
  setEditingTemplateId(id: number | null): void {
    this.editingTemplateIdSignal.set(id);
  }

  loadTemplateForEdit(id: number, name: string, description: string, maleMessage: string, femaleMessage: string): void {
    this.editingTemplateIdSignal.set(id);
    this.templateNameSignal.set(name);
    this.templateDescriptionSignal.set(description);
    this.setMaleMessage(maleMessage);
    this.setFemaleMessage(femaleMessage);
  }

  isEditingTemplate(): boolean {
    return this.editingTemplateIdSignal() !== null;
  }

  // Message validation for campaign start
  private validateMessage(message: string): { isValid: boolean; errors: string[] } {
    const errors: string[] = [];

    // Count all placeholders matching {something}
    const placeholderRegex = /\{[^}]+\}/g;
    const placeholders = message.match(placeholderRegex) || [];

    // Check for name variables
    const hasArabicName = message.includes('{arabic_name}');
    const hasEnglishName = message.includes('{english_name}');
    const hasNameVariable = hasArabicName || hasEnglishName;

    // Count other placeholders (not name placeholders) - randomization placeholders like {opt1-opt2}
    const otherPlaceholders = placeholders.filter(p => !this.namePlaceholders.includes(p));

    // Validation checks
    if (message.length > 0) {
      // Must have at least one: {arabic_name} OR {english_name}
      if (!hasNameVariable) {
        errors.push('Must include {arabic_name} or {english_name}');
      }
      // Check minimum randomization placeholders required
      if (otherPlaceholders.length < this.requiredOtherPlaceholders) {
        errors.push(`Minimum ${this.requiredOtherPlaceholders} randomization placeholders required (found ${otherPlaceholders.length})`);
      }
    }

    const isValid = message.length === 0 || (
      hasNameVariable &&
      otherPlaceholders.length >= this.requiredOtherPlaceholders
    );

    return { isValid, errors };
  }
}
