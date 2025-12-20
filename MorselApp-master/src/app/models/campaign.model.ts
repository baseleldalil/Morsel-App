export interface CampaignProgress {
  selectedContacts: number;
  sent: number;
  delivered: number;
  failed: number;
  pending: number;
  remaining: number;
  inProgress: number;
  successRate: number;
  estimatedTime: string;
  speed: number;
  breaksTaken?: number;
}

export interface CampaignMessage {
  maleMessage: string;
  femaleMessage: string;
  attachments: Attachment[];
}

export interface Attachment {
  id: number;
  name: string;
  size: number;
  type: string;
  url?: string;
  base64?: string;
}

export type CampaignStatus = 'idle' | 'initializing' | 'running' | 'paused' | 'completed' | 'stopping';

export interface TimingSettings {
  messageDelayMin: number;
  messageDelayMax: number;
  randomBreaksEnabled: boolean;
  messagesCountMin: number;
  messagesCountMax: number;
  breakDurationMin: number;
  breakDurationMax: number;
  decimalRandomEnabled: boolean;
  precision: number;
}

// WhatsApp API Types
export type BrowserType = 'Chrome' | 'Firefox';

export interface InitRequest {
  browserType: BrowserType;
}

export interface StatusResultDto {
  browserOpen: boolean;
  loggedIn: boolean;
  browserType: string | null;
  message: string | null;
}

export interface AttachmentDto {
  base64: string;
  fileName: string;
  mediaType: 'image' | 'video' | 'document';
  caption?: string | null;
}

export interface ContactDto {
  id?: number | null;
  phone: string;
  name?: string | null;
  arabicName?: string | null;
  englishName?: string | null;
  gender?: string | null;
}

export interface DelaySettingsDto {
  minDelaySeconds: number;
  maxDelaySeconds: number;
}

export interface BreakSettingsDto {
  enabled: boolean;
  minBreakAfterMessages: number;
  maxBreakAfterMessages: number;
  minBreakMinutes: number;
  maxBreakMinutes: number;
}

export interface SendBulkRequest {
  contacts: ContactDto[];
  message?: string | null;
  maleMessage?: string | null;
  femaleMessage?: string | null;
  attachments?: AttachmentDto[] | null;
  delaySettings?: DelaySettingsDto;
  breakSettings?: BreakSettingsDto;
}

export type BulkOperationStatus = 0 | 1 | 2 | 3 | 4; // Idle=0, Running=1, Paused=2, Completed=3, Stopped=4

export interface SendResultDto {
  phone: string | null;
  success: boolean;
  attachmentsSent: number;
  delayAppliedSeconds: number;
  error: string | null;
}

export interface BulkOperationState {
  operationId: string | null;
  status: BulkOperationStatus;
  totalContacts: number;
  processedContacts: number;
  sent: number;
  failed: number;
  breaksTaken: number;
  // Break tracking
  isOnBreak: boolean;
  breakStartedAt: string | null;
  breakEndsAt: string | null;
  breakTriggeredAtMessage: number;
  nextBreakAfterMessages: number;
  messagesSinceLastBreak: number;
  breakDurationMinutes: number;
  // Progress
  remainingContacts: number;
  progressPercent: number;
  startedAt: string | null;
  pausedAt: string | null;
  completedAt: string | null;
  message: string | null;
  results: SendResultDto[] | null;
}

export interface BulkControlResponse {
  success: boolean;
  message: string | null;
  state: BulkOperationState;
}

export interface ApiResponse<T> {
  success: boolean;
  message: string | null;
  data: T;
  error: string | null;
  timestamp: string;
}
