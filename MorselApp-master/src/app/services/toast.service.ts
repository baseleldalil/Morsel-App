import { Injectable, signal } from '@angular/core';

export interface Toast {
  id: number;
  message: string;
  type: 'success' | 'error' | 'warning' | 'info';
  title?: string;
  duration?: number;
}

@Injectable({
  providedIn: 'root'
})
export class ToastService {
  private toastsSignal = signal<Toast[]>([]);
  private nextId = 0;

  readonly toasts = this.toastsSignal.asReadonly();

  show(message: string, type: Toast['type'] = 'info', title?: string, duration: number = 5000): void {
    const toast: Toast = {
      id: this.nextId++,
      message,
      type,
      title,
      duration
    };

    this.toastsSignal.update(toasts => [...toasts, toast]);

    if (duration > 0) {
      setTimeout(() => this.remove(toast.id), duration);
    }
  }

  success(message: string, title?: string): void {
    this.show(message, 'success', title);
  }

  error(message: string, title?: string): void {
    this.show(message, 'error', title, 7000);
  }

  warning(message: string, title?: string): void {
    this.show(message, 'warning', title);
  }

  info(message: string, title?: string): void {
    this.show(message, 'info', title);
  }

  remove(id: number): void {
    this.toastsSignal.update(toasts => toasts.filter(t => t.id !== id));
  }

  clear(): void {
    this.toastsSignal.set([]);
  }
}
