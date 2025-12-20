export interface ImportContactsResponse {
  message: string;
  total_rows: number;
  valid_contacts: number;
  invalid_contacts: number;
}

export interface ImportOptions {
  allowInternational: boolean;
  skipDuplicates: boolean;
}
