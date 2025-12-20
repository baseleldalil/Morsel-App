export interface Contact {
  id: number;
  firstName: string;
  arabicName: string;
  englishName: string;
  number: string;
  gender: 'M' | 'F' | 'U';
  status: ContactStatus;
  selected?: boolean;
  issueDescription?: string | null;
  sendAttemptCount?: number;
}

export type ContactStatus =
  | 'Pending'
  | 'Sent'
  | 'Delivered'
  | 'Failed'
  | 'NotValid'
  | 'HasIssues'
  | 'Blocked'
  | 'NotInterested'
  | 'Responded';

export type ContactFilter =
  | 'All'
  | 'Pending'
  | 'Sent'
  | 'Delivered'
  | 'Issues'
  | 'Respond'
  | 'NotInt'
  | 'Failed';

// Request model for updating a contact (matches API PascalCase)
export interface UpdateContactRequest {
  Name: string;
  ArabicName: string;
  EnglishName: string;
  Phone: string;
  Gender: string;
}

// Response model for contact operations
export interface ContactResponse {
  message: string;
  contact?: Contact;
}

// Response model for status change operations
export interface StatusChangeResponse {
  message: string;
  contactId: number;
  newStatus: ContactStatus;
}

// API Contact model (from backend)
export interface ApiContact {
  id: number;
  first_name: string;
  arabic_name: string;
  english_name: string;
  formatted_phone: string;
  gender: 'M' | 'F' | 'U';
  created_at: string;
  updated_at: string;
  status: string;
  issue_description: string | null;
  last_message_sent_at: string | null;
  last_status_update_at: string | null;
  send_attempt_count: number;
}

// Response model for fetching contacts (paginated list)
export interface ContactsListResponse {
  contacts: ApiContact[];
  total: number;
  page: number;
  page_size: number;
  total_pages: number;
}
