import { Component, inject, output, signal, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TemplatesService, CampaignService, ToastService } from '../../services';
import { Template, TemplateTab, CreateCampaignTemplateRequest } from '../../models';

@Component({
  selector: 'app-templates-panel',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './templates-panel.html',
  styleUrl: './templates-panel.css'
})
export class TemplatesPanelComponent implements OnInit {
  private templatesService = inject(TemplatesService);
  private campaignService = inject(CampaignService);
  private toastService = inject(ToastService);

  // Output for template selection
  templateSelected = output<Template>();

  // Expose signals
  readonly templates = this.templatesService.filteredTemplates;
  readonly activeTab = this.templatesService.activeTab;
  readonly searchQuery = this.templatesService.searchQuery;
  readonly systemCount = this.templatesService.systemTemplatesCount;
  readonly userCount = this.templatesService.userTemplatesCount;
  readonly isLoading = this.templatesService.isLoading;
  readonly isSaving = this.templatesService.isSaving;

  // Pagination signals
  readonly currentPage = this.templatesService.currentPage;
  readonly totalPages = this.templatesService.totalPages;
  readonly totalCount = this.templatesService.totalCount;

  // Local state
  searchValue = '';

  // Save Template Modal
  showSaveModal = signal<boolean>(false);
  saveForm = signal<{ name: string; description: string; maleContent: string; femaleContent: string }>({
    name: '',
    description: '',
    maleContent: '',
    femaleContent: ''
  });

  // Delete Confirmation Modal
  showDeleteModal = signal<boolean>(false);
  templateToDelete = signal<Template | null>(null);

  ngOnInit(): void {
    this.templatesService.fetchTemplates();
  }

  setActiveTab(tab: TemplateTab): void {
    this.templatesService.setActiveTab(tab);
  }

  onSearchChange(value: string): void {
    this.searchValue = value;
    this.templatesService.setSearch(value);
  }

  // Load template into campaign editor - Direct load without confirmation
  loadTemplate(template: Template): void {
    this.applyTemplate(template);
  }

  private applyTemplate(template: Template): void {
    const maleMsg = template.maleMessage || template.content || '';
    const femaleMsg = template.femaleMessage || template.maleMessage || template.content || '';
    const description = template.description || '';

    // If user template, enable edit mode
    if (template.type === 'user') {
      this.campaignService.loadTemplateForEdit(template.id, template.title, description, maleMsg, femaleMsg);
      this.toastService.info(`Editing template: ${template.title}`);
    } else {
      // System template - just load without edit mode
      this.campaignService.setTemplateName(template.title);
      this.campaignService.setTemplateDescription(description);
      this.campaignService.setMaleMessage(maleMsg);
      this.campaignService.setFemaleMessage(femaleMsg);
      this.campaignService.setEditingTemplateId(null);
      this.toastService.success('Template loaded successfully');
    }

    this.templateSelected.emit(template);
  }

  // Open save template modal - pre-fill with current campaign messages
  openSaveModal(): void {
    const maleMessage = this.campaignService.maleMessage();
    const femaleMessage = this.campaignService.femaleMessage();
    this.saveForm.set({
      name: '',
      description: '',
      maleContent: maleMessage || '',
      femaleContent: femaleMessage || ''
    });
    this.showSaveModal.set(true);
  }

  closeSaveModal(): void {
    this.showSaveModal.set(false);
  }

  updateSaveForm(field: 'name' | 'description' | 'maleContent' | 'femaleContent', value: string): void {
    this.saveForm.update(form => ({
      ...form,
      [field]: value
    }));
  }

  // Save current campaign as template
  saveAsTemplate(): void {
    const form = this.saveForm();

    if (!form.name.trim()) {
      this.toastService.error('Template name is required');
      return;
    }

    if (!form.maleContent && !form.femaleContent) {
      this.toastService.error('Please enter at least one message (male or female)');
      return;
    }

    const request: CreateCampaignTemplateRequest = {
      name: form.name.trim(),
      description: form.description.trim(),
      content: form.maleContent || form.femaleContent || '',
      maleContent: form.maleContent || '',
      femaleContent: form.femaleContent || form.maleContent || ''
    };

    this.templatesService.createTemplate(request).subscribe({
      next: () => {
        this.toastService.success('Template saved successfully');
        this.closeSaveModal();
        // Switch to My Templates tab
        this.templatesService.setActiveTab('user');
      },
      error: (error) => {
        const errorMessage = error.error?.message || 'Failed to save template';
        this.toastService.error(errorMessage);
      }
    });
  }

  // Delete template
  confirmDelete(event: Event, template: Template): void {
    event.stopPropagation();
    this.templateToDelete.set(template);
    this.showDeleteModal.set(true);
  }

  cancelDelete(): void {
    this.showDeleteModal.set(false);
    this.templateToDelete.set(null);
  }

  deleteTemplate(): void {
    const template = this.templateToDelete();
    if (!template) return;

    this.templatesService.deleteTemplate(template.id).subscribe({
      next: () => {
        this.toastService.success('Template deleted');
        this.cancelDelete();
      },
      error: (error) => {
        const errorMessage = error.error?.message || 'Failed to delete template';
        this.toastService.error(errorMessage);
      }
    });
  }

  refreshTemplates(): void {
    this.templatesService.refreshTemplates();
  }

  // Pagination methods
  goToPage(page: number): void {
    if (page >= 1 && page <= this.totalPages()) {
      this.templatesService.setPage(page);
    }
  }

  nextPage(): void {
    if (this.currentPage() < this.totalPages()) {
      this.goToPage(this.currentPage() + 1);
    }
  }

  prevPage(): void {
    if (this.currentPage() > 1) {
      this.goToPage(this.currentPage() - 1);
    }
  }

  formatDate(date: Date): string {
    return new Date(date).toLocaleDateString('en-US', {
      month: '2-digit',
      day: '2-digit',
      year: 'numeric'
    });
  }

  truncateText(text: string, maxLength: number = 80): string {
    if (!text) return '';
    if (text.length <= maxLength) return text;
    return text.substring(0, maxLength) + '...';
  }
}
