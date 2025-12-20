import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { CampaignService } from '../../services';

@Component({
  selector: 'app-advanced-timing',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './advanced-timing.html',
  styleUrl: './advanced-timing.css'
})
export class AdvancedTimingComponent {
  private campaignService = inject(CampaignService);

  // Expose signals
  readonly timing = this.campaignService.timing;

  isExpanded = true;
  precisionOptions = [0, 0.1, 0.01, 0.001];

  toggleExpanded(): void {
    this.isExpanded = !this.isExpanded;
  }

  updateDelayMin(value: number): void {
    // Enforce minimum 30 seconds
    const validValue = Math.max(30, value || 30);
    this.campaignService.updateTiming({ messageDelayMin: validValue });
  }

  updateDelayMax(value: number): void {
    // Enforce minimum 30 seconds
    const validValue = Math.max(30, value || 30);
    this.campaignService.updateTiming({ messageDelayMax: validValue });
  }

  enforceDelayMin(input: HTMLInputElement): void {
    const value = input.valueAsNumber;
    if (!value || value < 30) {
      input.value = '30';
      this.campaignService.updateTiming({ messageDelayMin: 30 });
    }
  }

  enforceDelayMax(input: HTMLInputElement): void {
    const value = input.valueAsNumber;
    if (!value || value < 30) {
      input.value = '30';
      this.campaignService.updateTiming({ messageDelayMax: 30 });
    }
  }

  updateMessagesMin(value: number): void {
    const validValue = Math.max(1, value || 1);
    this.campaignService.updateTiming({ messagesCountMin: validValue });
  }

  updateMessagesMax(value: number): void {
    // Enforce maximum 35 messages
    const validValue = Math.min(35, Math.max(1, value || 1));
    this.campaignService.updateTiming({ messagesCountMax: validValue });
  }

  updateBreakMin(value: number): void {
    // Enforce minimum 3 minutes
    const validValue = Math.max(3, value || 3);
    this.campaignService.updateTiming({ breakDurationMin: validValue });
  }

  updateBreakMax(value: number): void {
    // Enforce minimum 3 minutes
    const validValue = Math.max(3, value || 3);
    this.campaignService.updateTiming({ breakDurationMax: validValue });
  }

  updatePrecision(value: number): void {
    this.campaignService.updateTiming({ precision: value });
  }

  saveSettings(): void {
    console.log('Settings saved:', this.timing());
  }

  resetSettings(): void {
    this.campaignService.resetTiming();
  }

  refreshSettings(): void {
    console.log('Settings refreshed');
  }
}
