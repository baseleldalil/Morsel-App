import { Component, inject, OnInit, OnDestroy, signal, HostListener } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ContactsService, ToastService } from '../../services';
import { ContactFilter, Contact, UpdateContactRequest } from '../../models';
import * as XLSX from 'xlsx';

@Component({
  selector: 'app-contacts-panel',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './contacts-panel.html',
  styleUrl: './contacts-panel.css'
})
export class ContactsPanelComponent implements OnInit, OnDestroy {
  // Close dropdown when clicking outside
  @HostListener('document:click', ['$event'])
  onDocumentClick(event: MouseEvent): void {
    const target = event.target as HTMLElement;
    if (!target.closest('.checkbox-wrapper')) {
      this.showSelectOptions = false;
    }
  }
  private contactsService = inject(ContactsService);
  private toastService = inject(ToastService);
  private statisticsRefreshInterval: ReturnType<typeof setInterval> | null = null;
  private contactsRefreshInterval: ReturnType<typeof setInterval> | null = null;

  // Expose signals to template - use paginatedContacts for display
  readonly contacts = this.contactsService.paginatedContacts;  // Client-side paginated
  readonly statistics = this.contactsService.statistics;
  readonly selectedFilter = this.contactsService.selectedFilter;
  readonly isLoading = this.contactsService.isLoading;
  readonly isImporting = this.contactsService.isImporting;
  readonly filteredContacts = this.contactsService.filteredContacts;  // All filtered (for count)
  readonly currentPage = this.contactsService.currentPage;
  readonly pageSize = this.contactsService.pageSize;
  readonly totalPages = this.contactsService.totalPages;
  readonly totalContacts = this.contactsService.totalContacts;  // Filtered count
  readonly selectedCount = this.contactsService.selectedCount;
  readonly visibleSelectedCount = this.contactsService.visibleSelectedCount;
  readonly filteredSelectedCount = this.contactsService.filteredSelectedCount;

  readonly filters: ContactFilter[] = [
    'All', 'Pending', 'Delivered', 'Issues', 'Respond', 'NotInt', 'Failed'
  ];

  readonly filterLabels: Record<ContactFilter, string> = {
    'All': 'All',
    'Pending': 'Pending',
    'Sent': 'Sent',
    'Delivered': 'Delivered',
    'Issues': 'Issues',
    'Respond': 'Respond',
    'NotInt': 'Not Int.',
    'Failed': 'Failed'
  };

  readonly filterIcons: Record<ContactFilter, string> = {
    'All': '≡',
    'Pending': '⏳',
    'Sent': '✓',
    'Delivered': '✓✓',
    'Issues': '⚠',
    'Respond': '💬',
    'NotInt': '🚫',
    'Failed': '✗'
  };

  selectAllChecked = false;
  showSelectOptions = false;

  // Inline editing state
  editingContactId = signal<number | null>(null);
  editForm = signal<UpdateContactRequest>({
    Name: '',
    ArabicName: '',
    EnglishName: '',
    Phone: '',
    Gender: ''
  });
  isSaving = signal<boolean>(false);

  ngOnInit(): void {
    this.contactsService.fetchStatistics();
    this.contactsService.fetchContacts();
    this.startAutoRefresh();
  }

  ngOnDestroy(): void {
    this.stopAutoRefresh();
  }

  private startAutoRefresh(): void {
    // Refresh statistics every 3 seconds
    this.statisticsRefreshInterval = setInterval(() => {
      this.contactsService.fetchStatistics();
    }, 3000);

    // Refresh contacts every 4 seconds
    this.contactsRefreshInterval = setInterval(() => {
      this.contactsService.fetchContacts();
    }, 4000);
  }

  private stopAutoRefresh(): void {
    if (this.statisticsRefreshInterval) {
      clearInterval(this.statisticsRefreshInterval);
      this.statisticsRefreshInterval = null;
    }
    if (this.contactsRefreshInterval) {
      clearInterval(this.contactsRefreshInterval);
      this.contactsRefreshInterval = null;
    }
  }

  refreshStatistics(): void {
    this.contactsService.refreshStatistics();
  }

  setFilter(filter: ContactFilter): void {
    this.contactsService.setFilter(filter);
  }

  toggleSelect(contactId: number): void {
    this.contactsService.toggleSelect(contactId);
    this.updateSelectAllState();
  }

  toggleSelectAll(): void {
    this.selectAllChecked = !this.selectAllChecked;
    this.contactsService.selectAllVisible(this.selectAllChecked);
  }

  toggleSelectOptions(): void {
    this.showSelectOptions = !this.showSelectOptions;
  }

  selectAllVisible(): void {
    this.contactsService.selectAllVisible(true);
    this.selectAllChecked = true;
    this.showSelectOptions = false;
    this.toastService.success(`Selected ${this.contacts().length} contacts on this page`);
  }

  deselectAllVisible(): void {
    this.contactsService.selectAllVisible(false);
    this.selectAllChecked = false;
    this.showSelectOptions = false;
  }

  selectAllFiltered(): void {
    this.contactsService.selectAllFiltered(true);
    this.showSelectOptions = false;
    this.toastService.success(`Selected all ${this.totalContacts()} filtered contacts`);
  }

  clearSelection(): void {
    this.contactsService.clearSelection();
    this.selectAllChecked = false;
    this.showSelectOptions = false;
    this.toastService.info('Selection cleared');
  }

  // Update header checkbox state based on visible selection
  updateSelectAllState(): void {
    const visible = this.contacts();
    if (visible.length === 0) {
      this.selectAllChecked = false;
      return;
    }
    const allSelected = visible.every(c => c.selected);
    this.selectAllChecked = allSelected;
  }

  // Check if all visible contacts are selected
  get allVisibleSelected(): boolean {
    const visible = this.contacts();
    return visible.length > 0 && visible.every(c => c.selected);
  }

  // Check if some (but not all) visible contacts are selected
  get someVisibleSelected(): boolean {
    const visible = this.contacts();
    const selectedCount = visible.filter(c => c.selected).length;
    return selectedCount > 0 && selectedCount < visible.length;
  }

  // Check if no visible contacts are selected
  get noneVisibleSelected(): boolean {
    return this.contacts().every(c => !c.selected);
  }

  setPage(page: number): void {
    this.contactsService.setPage(page);
  }

  setPageSize(event: Event): void {
    const size = +(event.target as HTMLSelectElement).value;
    this.contactsService.setPageSize(size);
  }

  onPageSizeChange(size: number): void {
    this.contactsService.setPageSize(size);
  }

  clearContacts(): void {
    this.contactsService.clearContacts();
  }

  exportContacts(): void {
    const contacts = this.filteredContacts();
    if (contacts.length === 0) {
      this.toastService.warning('No contacts to export');
      return;
    }

    // Prepare data for Excel
    const exportData = contacts.map(c => ({
      'First Name': c.firstName,
      'Arabic Name': c.arabicName,
      'English Name': c.englishName,
      'Phone Number': c.number,
      'Gender': c.gender === 'M' ? 'Male' : c.gender === 'F' ? 'Female' : 'Unknown',
      'Status': c.status
    }));

    // Create workbook and worksheet
    const worksheet = XLSX.utils.json_to_sheet(exportData);
    const workbook = XLSX.utils.book_new();
    XLSX.utils.book_append_sheet(workbook, worksheet, 'Contacts');

    // Auto-size columns
    const colWidths = [
      { wch: 20 }, // First Name
      { wch: 25 }, // Arabic Name
      { wch: 25 }, // English Name
      { wch: 15 }, // Phone Number
      { wch: 10 }, // Gender
      { wch: 15 }  // Status
    ];
    worksheet['!cols'] = colWidths;

    // Generate filename with date
    const date = new Date().toISOString().split('T')[0];
    const filterName = this.selectedFilter() !== 'All' ? `_${this.selectedFilter()}` : '';
    const filename = `contacts${filterName}_${date}.xlsx`;

    // Download file
    XLSX.writeFile(workbook, filename);
    this.toastService.success(`Exported ${contacts.length} contacts to ${filename}`);
  }

  importContacts(): void {
    const input = document.createElement('input');
    input.type = 'file';
    input.accept = '.xlsx,.xls,.csv';
    input.onchange = (event: Event) => {
      const file = (event.target as HTMLInputElement).files?.[0];
      if (file) {
        this.handleFileImport(file);
      }
    };
    input.click();
  }

  private handleFileImport(file: File): void {
    this.contactsService.importContactsFromFile(file).subscribe({
      next: (response) => {
        this.toastService.success(
          `Total: ${response.total_rows} | Valid: ${response.valid_contacts} | Invalid: ${response.invalid_contacts}`,
          response.message
        );
      },
      error: (error) => {
        const errorMessage = error.error?.message || error.message || 'Failed to import contacts';
        this.toastService.error(errorMessage, 'Import Failed');
      }
    });
  }

  getStatusClass(status: string): string {
    const classes: Record<string, string> = {
      'Pending': 'status-pending',
      'Sent': 'status-sent',
      'Delivered': 'status-delivered',
      'Failed': 'status-failed',
      'NotValid': 'status-notvalid',
      'HasIssues': 'status-hasissues',
      'Blocked': 'status-blocked',
      'NotInterested': 'status-notinterested',
      'Responded': 'status-responded'
    };
    return classes[status] || '';
  }

  getGenderIcon(gender: string): string {
    return gender === 'M' ? '♂' : gender === 'F' ? '♀' : '?';
  }

  getPagesArray(): number[] {
    const total = this.totalPages();
    const current = this.currentPage();
    const pages: number[] = [];

    for (let i = Math.max(1, current - 2); i <= Math.min(total, current + 2); i++) {
      pages.push(i);
    }

    return pages;
  }

  // Inline editing methods
  startEdit(contact: Contact): void {
    this.editingContactId.set(contact.id);
    this.editForm.set({
      Name: contact.firstName,
      ArabicName: contact.arabicName,
      EnglishName: contact.englishName,
      Phone: contact.number,
      Gender: contact.gender === 'U' ? '' : contact.gender
    });
  }

  cancelEdit(): void {
    this.editingContactId.set(null);
    this.editForm.set({
      Name: '',
      ArabicName: '',
      EnglishName: '',
      Phone: '',
      Gender: ''
    });
  }

  saveEdit(): void {
    const contactId = this.editingContactId();
    if (!contactId) return;

    const form = this.editForm();

    // Normalize gender to uppercase for validation
    const genderUpper = (form.Gender || '').toUpperCase().trim();

    // Validate Gender is set (M or F required, case-insensitive)
    if (!genderUpper || (genderUpper !== 'M' && genderUpper !== 'F')) {
      this.toastService.error('Please set gender (M or F) before saving');
      return;
    }

    // Validate Phone number is set
    if (!form.Phone || form.Phone.trim() === '') {
      this.toastService.error('Please set phone number before saving');
      return;
    }

    // Ensure gender is uppercase before saving
    const normalizedForm = { ...form, Gender: genderUpper };

    this.isSaving.set(true);
    this.contactsService.updateContact(contactId, normalizedForm).subscribe({
      next: () => {
        this.toastService.success('Contact updated successfully');
        this.cancelEdit();
        this.isSaving.set(false);
      },
      error: (error) => {
        const errorMessage = error.error?.error || error.error?.message || 'Failed to update contact';
        this.toastService.error(errorMessage);
        this.isSaving.set(false);
      }
    });
  }

  updateEditField(field: keyof UpdateContactRequest, value: string): void {
    this.editForm.update(form => ({
      ...form,
      [field]: value
    }));
  }

  onGenderInput(value: string): void {
    const upper = value.toUpperCase();
    // Only accept M or F
    if (upper === 'M' || upper === 'F' || upper === '') {
      this.updateEditField('Gender', upper);
    }
  }

  // Status change methods
  resendContact(contactId: number): void {
    // Find the contact to validate
    const contact = this.contacts().find(c => c.id === contactId);
    if (!contact) {
      this.toastService.error('Contact not found');
      return;
    }

    // Check if already Pending
    if (contact.status === 'Pending') {
      this.toastService.warning('Contact is already in Pending status');
      return;
    }

    // Normalize gender for validation (case-insensitive)
    const genderUpper = (contact.gender || '').toUpperCase().trim();

    // Validate gender is set (M or F required, case-insensitive)
    if (!genderUpper || (genderUpper !== 'M' && genderUpper !== 'F')) {
      this.toastService.error('Please set gender (M or F) before resending');
      return;
    }

    // Validate phone number is set
    if (!contact.number || contact.number.trim() === '') {
      this.toastService.error('Please set phone number before resending');
      return;
    }

    this.contactsService.resendContact(contactId).subscribe({
      next: () => {
        this.toastService.success('Contact reset to Pending for resend');
      },
      error: (error) => {
        const errorMessage = error.error?.error || error.error?.message || 'Failed to resend';
        this.toastService.error(errorMessage);
      }
    });
  }

  markNotInterested(contactId: number): void {
    this.contactsService.markNotInterested(contactId).subscribe({
      next: () => {
        this.toastService.success('Contact marked as not interested');
      },
      error: (error) => {
        const errorMessage = error.error?.error || error.error?.message || 'Failed to update status';
        this.toastService.error(errorMessage);
      }
    });
  }

  markResponded(contactId: number): void {
    this.contactsService.markResponded(contactId).subscribe({
      next: () => {
        this.toastService.success('Contact marked as responded');
      },
      error: (error) => {
        const errorMessage = error.error?.error || error.error?.message || 'Failed to update status';
        this.toastService.error(errorMessage);
      }
    });
  }

  deleteContact(contactId: number): void {
    this.contactsService.deleteContact(contactId).subscribe({
      next: () => {
        this.toastService.success('Contact deleted');
      },
      error: (error) => {
        const errorMessage = error.error?.error || error.error?.message || 'Failed to delete contact';
        this.toastService.error(errorMessage);
      }
    });
  }

  // Bulk delete state
  isDeleting = signal<boolean>(false);
  showDeleteConfirm = signal<boolean>(false);

  // Resend all failed state
  isResendingAllFailed = signal<boolean>(false);

  confirmBulkDelete(): void {
    const count = this.selectedCount();
    if (count === 0) {
      this.toastService.warning('No contacts selected');
      return;
    }
    this.showDeleteConfirm.set(true);
  }

  cancelBulkDelete(): void {
    this.showDeleteConfirm.set(false);
  }

  bulkDeleteContacts(): void {
    const selectedContacts = this.contactsService.selectedContacts();
    if (selectedContacts.length === 0) {
      this.toastService.warning('No contacts selected');
      return;
    }

    const ids = selectedContacts.map(c => c.id);
    this.isDeleting.set(true);
    this.showDeleteConfirm.set(false);

    this.contactsService.deleteContacts(ids).subscribe({
      next: (result) => {
        this.toastService.success(`Successfully deleted ${result.deleted} contacts`);
        this.isDeleting.set(false);
        this.clearSelection();
      },
      error: (error) => {
        const errorMessage = error.error?.error || error.error?.message || 'Failed to delete contacts';
        this.toastService.error(errorMessage);
        this.isDeleting.set(false);
      }
    });
  }

  isEditing(contactId: number): boolean {
    return this.editingContactId() === contactId;
  }

  // Resend all failed contacts
  resendAllFailed(): void {
    const failedCount = this.statistics().failed;
    if (failedCount === 0) {
      this.toastService.warning('No failed contacts to resend');
      return;
    }

    this.isResendingAllFailed.set(true);
    this.contactsService.resendAllFailed().subscribe({
      next: (response) => {
        this.toastService.success(`${response.resent_count} failed contacts reset to Pending`);
        this.isResendingAllFailed.set(false);
      },
      error: (error) => {
        const errorMessage = error.error?.error || error.error?.message || 'Failed to resend contacts';
        this.toastService.error(errorMessage);
        this.isResendingAllFailed.set(false);
      }
    });
  }
}
