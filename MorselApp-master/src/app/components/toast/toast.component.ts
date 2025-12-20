import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ToastService } from '../../services/toast.service';

@Component({
  selector: 'app-toast',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="toast-container">
      @for (toast of toastService.toasts(); track toast.id) {
        <div class="toast" [class]="'toast-' + toast.type" (click)="toastService.remove(toast.id)">
          <div class="toast-icon">
            @switch (toast.type) {
              @case ('success') { <span>&#10003;</span> }
              @case ('error') { <span>&#10007;</span> }
              @case ('warning') { <span>&#9888;</span> }
              @case ('info') { <span>&#8505;</span> }
            }
          </div>
          <div class="toast-content">
            @if (toast.title) {
              <div class="toast-title">{{ toast.title }}</div>
            }
            <div class="toast-message">{{ toast.message }}</div>
          </div>
          <button class="toast-close" (click)="toastService.remove(toast.id); $event.stopPropagation()">
            &times;
          </button>
        </div>
      }
    </div>
  `,
  styles: [`
    .toast-container {
      position: fixed;
      top: 20px;
      right: 20px;
      z-index: 9999;
      display: flex;
      flex-direction: column;
      gap: 10px;
      max-width: 400px;
    }

    .toast {
      display: flex;
      align-items: flex-start;
      gap: 12px;
      padding: 14px 16px;
      border-radius: 10px;
      box-shadow: 0 4px 20px rgba(0, 0, 0, 0.15);
      cursor: pointer;
      animation: slideIn 0.3s ease-out;
      min-width: 300px;
    }

    @keyframes slideIn {
      from {
        transform: translateX(100%);
        opacity: 0;
      }
      to {
        transform: translateX(0);
        opacity: 1;
      }
    }

    .toast-success {
      background: linear-gradient(135deg, #10b981 0%, #059669 100%);
      color: white;
    }

    .toast-error {
      background: linear-gradient(135deg, #ef4444 0%, #dc2626 100%);
      color: white;
    }

    .toast-warning {
      background: linear-gradient(135deg, #f59e0b 0%, #d97706 100%);
      color: white;
    }

    .toast-info {
      background: linear-gradient(135deg, #3b82f6 0%, #2563eb 100%);
      color: white;
    }

    .toast-icon {
      font-size: 20px;
      line-height: 1;
      flex-shrink: 0;
    }

    .toast-content {
      flex: 1;
    }

    .toast-title {
      font-weight: 600;
      font-size: 14px;
      margin-bottom: 4px;
    }

    .toast-message {
      font-size: 13px;
      opacity: 0.95;
      line-height: 1.4;
    }

    .toast-close {
      background: none;
      border: none;
      color: white;
      font-size: 20px;
      cursor: pointer;
      opacity: 0.7;
      padding: 0;
      line-height: 1;
      transition: opacity 0.2s;
    }

    .toast-close:hover {
      opacity: 1;
    }
  `]
})
export class ToastComponent {
  toastService = inject(ToastService);
}
