import { Component, inject, signal, computed, ViewChild, ElementRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { CampaignService, TemplatesService, ToastService } from '../../services';
import { CreateCampaignTemplateRequest, UpdateCampaignTemplateRequest } from '../../models';

export interface MessageValidation {
  placeholderCount: number;
  emptyPlaceholderCount: number;
  hasArabicName: boolean;
  hasEnglishName: boolean;
  isValid: boolean;
  errors: string[];
}

@Component({
  selector: 'app-message-composer',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './message-composer.html',
  styleUrl: './message-composer.css'
})
export class MessageComposerComponent {
  private campaignService = inject(CampaignService);
  private templatesService = inject(TemplatesService);
  private toastService = inject(ToastService);

  // ViewChild references for textareas
  @ViewChild('maleTextarea') maleTextarea!: ElementRef<HTMLTextAreaElement>;
  @ViewChild('femaleTextarea') femaleTextarea!: ElementRef<HTMLTextAreaElement>;

  // Track last focused textarea ('male' or 'female')
  lastFocusedTextarea: 'male' | 'female' = 'male';
  lastCursorPosition: number = 0;

  // Template save state
  isSavingTemplate = signal<boolean>(false);

  // Expose signals - use CampaignService for shared template editing state
  readonly maleMessage = this.campaignService.maleMessage;
  readonly femaleMessage = this.campaignService.femaleMessage;
  readonly maleMessageLength = this.campaignService.maleMessageLength;
  readonly femaleMessageLength = this.campaignService.femaleMessageLength;
  readonly attachments = this.campaignService.attachments;
  readonly templateName = this.campaignService.templateName;
  readonly templateDescription = this.campaignService.templateDescription;
  readonly editingTemplateId = this.campaignService.editingTemplateId;

  readonly maxLength = 5000;
  readonly requiredOtherPlaceholders = 3;
  // Name placeholders - one of these is required
  readonly namePlaceholders = ['{arabic_name}', '{english_name}'];
  isPreviewExpanded = true;
  showEmojiPicker = false;
  selectedCategory = 'smileys';

  // Validation computed signals
  readonly maleValidation = computed<MessageValidation>(() => {
    return this.validateMessage(this.maleMessage());
  });

  readonly femaleValidation = computed<MessageValidation>(() => {
    return this.validateMessage(this.femaleMessage());
  });

  readonly isMessagesValid = computed<boolean>(() => {
    const maleMsg = this.maleMessage();
    const femaleMsg = this.femaleMessage();

    // If both messages are empty, return false
    if (!maleMsg && !femaleMsg) return false;

    // Validate only non-empty messages
    const maleValid = !maleMsg || this.maleValidation().isValid;
    const femaleValid = !femaleMsg || this.femaleValidation().isValid;

    // If both messages provided, they must be different
    const messagesAreDifferent = !maleMsg || !femaleMsg || maleMsg.trim() !== femaleMsg.trim();

    return maleValid && femaleValid && messagesAreDifferent;
  });

  // Check if both messages are the same (for showing warning)
  readonly messagesAreSame = computed<boolean>(() => {
    const maleMsg = this.maleMessage().trim();
    const femaleMsg = this.femaleMessage().trim();
    return maleMsg.length > 0 && femaleMsg.length > 0 && maleMsg === femaleMsg;
  });

  private validateMessage(message: string): MessageValidation {
    const errors: string[] = [];

    // Count all placeholders matching {something}
    const placeholderRegex = /\{[^}]+\}/g;
    const placeholders = message.match(placeholderRegex) || [];
    const placeholderCount = placeholders.length;

    // Check for name variables
    const hasArabicName = message.includes('{arabic_name}');
    const hasEnglishName = message.includes('{english_name}');
    const hasNameVariable = hasArabicName || hasEnglishName;

    // Count other placeholders (not name placeholders) - these are randomization placeholders like {opt1-opt2}
    const otherPlaceholders = placeholders.filter(p => !this.namePlaceholders.includes(p));
    const otherPlaceholderCount = otherPlaceholders.length;

    // Validation checks
    if (message.length > 0) {
      // Must have at least one: {arabic_name} OR {english_name}
      if (!hasNameVariable) {
        errors.push('Must include {arabic_name} or {english_name}');
      }
      // Check minimum other placeholders required (randomization placeholders)
      if (otherPlaceholderCount < this.requiredOtherPlaceholders) {
        errors.push(`Minimum ${this.requiredOtherPlaceholders} randomization placeholders required (found ${otherPlaceholderCount})`);
      }
    }

    const isValid = message.length === 0 || (
      hasNameVariable &&
      otherPlaceholderCount >= this.requiredOtherPlaceholders
    );

    return {
      placeholderCount,
      emptyPlaceholderCount: otherPlaceholderCount,
      hasArabicName,
      hasEnglishName,
      isValid,
      errors
    };
  }

  // WhatsApp-style emoji categories
  readonly emojiCategories = [
    { name: 'smileys', icon: '😀', emojis: ['😀', '😃', '😄', '😁', '😅', '😂', '🤣', '😊', '😇', '🙂', '🙃', '😉', '😌', '😍', '🥰', '😘', '😗', '😙', '😚', '😋', '😛', '😜', '🤪', '😝', '🤑', '🤗', '🤭', '🤫', '🤔', '🤐', '🤨', '😐', '😑', '😶', '😏', '😒', '🙄', '😬', '🤥', '😌', '😔', '😪', '🤤', '😴', '😷', '🤒', '🤕', '🤢', '🤮', '🤧', '🥵', '🥶', '🥴', '😵', '🤯', '🤠', '🥳', '😎', '🤓', '🧐'] },
    { name: 'gestures', icon: '👋', emojis: ['👋', '🤚', '🖐️', '✋', '🖖', '👌', '🤌', '🤏', '✌️', '🤞', '🤟', '🤘', '🤙', '👈', '👉', '👆', '🖕', '👇', '☝️', '👍', '👎', '✊', '👊', '🤛', '🤜', '👏', '🙌', '👐', '🤲', '🤝', '🙏', '✍️', '💅', '🤳', '💪', '🦾', '🦿', '🦵', '🦶', '👂', '🦻', '👃', '🧠', '🫀', '🫁', '🦷', '🦴', '👀', '👁️', '👅', '👄'] },
    { name: 'hearts', icon: '❤️', emojis: ['❤️', '🧡', '💛', '💚', '💙', '💜', '🖤', '🤍', '🤎', '💔', '❣️', '💕', '💞', '💓', '💗', '💖', '💘', '💝', '💟', '☮️', '✝️', '☪️', '🕉️', '☸️', '✡️', '🔯', '🕎', '☯️', '☦️', '🛐', '⛎', '♈', '♉', '♊', '♋', '♌', '♍', '♎', '♏', '♐', '♑', '♒', '♓', '🆔', '⚛️'] },
    { name: 'celebration', icon: '🎉', emojis: ['🎉', '🎊', '🎈', '🎂', '🎁', '🎀', '🎗️', '🏆', '🏅', '🥇', '🥈', '🥉', '⚽', '🏀', '🏈', '⚾', '🥎', '🎾', '🏐', '🏉', '🥏', '🎱', '🪀', '🏓', '🏸', '🏒', '🏑', '🥍', '🏏', '🪃', '🥅', '⛳', '🪁', '🏹', '🎣', '🤿', '🥊', '🥋', '🎽', '🛹', '🛼', '🛷', '⛸️', '🥌', '🎿', '⛷️', '🏂'] },
    { name: 'nature', icon: '🌸', emojis: ['🌸', '💮', '🏵️', '🌹', '🥀', '🌺', '🌻', '🌼', '🌷', '🌱', '🪴', '🌲', '🌳', '🌴', '🌵', '🌾', '🌿', '☘️', '🍀', '🍁', '🍂', '🍃', '🪹', '🪺', '🍄', '🌰', '🦀', '🦞', '🦐', '🦑', '🌍', '🌎', '🌏', '🌐', '🪨', '🌑', '🌒', '🌓', '🌔', '🌕', '🌖', '🌗', '🌘', '🌙', '🌚', '🌛', '🌜', '☀️', '🌝', '🌞', '⭐', '🌟', '🌠', '☁️', '⛅', '🌤️', '🌥️', '🌦️', '🌧️', '🌨️', '🌩️', '🌪️', '🌫️', '🌬️', '🌈', '☔', '⚡', '❄️', '☃️', '⛄', '🔥', '💧', '🌊'] },
    { name: 'food', icon: '🍕', emojis: ['🍕', '🍔', '🍟', '🌭', '🥪', '🌮', '🌯', '🫔', '🥙', '🧆', '🥚', '🍳', '🥘', '🍲', '🫕', '🥣', '🥗', '🍿', '🧈', '🧂', '🥫', '🍱', '🍘', '🍙', '🍚', '🍛', '🍜', '🍝', '🍠', '🍢', '🍣', '🍤', '🍥', '🥮', '🍡', '🥟', '🥠', '🥡', '🦀', '🦞', '🦐', '🦑', '🦪', '🍦', '🍧', '🍨', '🍩', '🍪', '🎂', '🍰', '🧁', '🥧', '🍫', '🍬', '🍭', '🍮', '🍯', '🍼', '🥛', '☕', '🫖', '🍵', '🍶', '🍾', '🍷', '🍸', '🍹', '🍺', '🍻', '🥂', '🥃', '🫗', '🥤', '🧋', '🧃', '🧉', '🧊'] },
    { name: 'objects', icon: '💼', emojis: ['💼', '👜', '👝', '🛍️', '🎒', '🩴', '👞', '👟', '🥾', '🥿', '👠', '👡', '🩰', '👢', '👑', '👒', '🎩', '🎓', '🧢', '🪖', '⛑️', '💄', '💍', '💎', '🔇', '🔈', '🔉', '🔊', '📢', '📣', '📯', '🔔', '🔕', '🎼', '🎵', '🎶', '🎙️', '🎚️', '🎛️', '🎤', '🎧', '📻', '🎷', '🪗', '🎸', '🎹', '🎺', '🎻', '🪕', '🥁', '🪘', '📱', '📲', '☎️', '📞', '📟', '📠', '🔋', '🔌', '💻', '🖥️', '🖨️', '⌨️', '🖱️', '🖲️', '💽', '💾', '💿', '📀', '🧮', '🎥', '🎞️', '📽️', '🎬', '📺', '📷', '📸', '📹', '📼'] },
    { name: 'symbols', icon: '✅', emojis: ['✅', '❌', '❓', '❔', '❗', '❕', '⭕', '🚫', '💯', '🔴', '🟠', '🟡', '🟢', '🔵', '🟣', '🟤', '⚫', '⚪', '🟥', '🟧', '🟨', '🟩', '🟦', '🟪', '🟫', '⬛', '⬜', '◼️', '◻️', '◾', '◽', '▪️', '▫️', '🔶', '🔷', '🔸', '🔹', '🔺', '🔻', '💠', '🔘', '🔳', '🔲', '🏁', '🚩', '🎌', '🏴', '🏳️', '🏳️‍🌈', '🏳️‍⚧️', '🏴‍☠️', '🇸🇦', '🇪🇬', '🇦🇪', '🇯🇴', '🇱🇧', '🇸🇾', '🇮🇶', '🇰🇼', '🇶🇦', '🇧🇭', '🇴🇲', '🇾🇪', '🇵🇸', '🇲🇦', '🇹🇳', '🇩🇿', '🇱🇾', '🇸🇩'] }
  ];

  getCurrentEmojis(): string[] {
    const category = this.emojiCategories.find(c => c.name === this.selectedCategory);
    return category ? category.emojis : [];
  }

  selectCategory(name: string): void {
    this.selectedCategory = name;
  }

  toggleEmojiPicker(): void {
    this.showEmojiPicker = !this.showEmojiPicker;
  }

  insertEmoji(emoji: string): void {
    const currentMale = this.maleMessage();
    const currentFemale = this.femaleMessage();
    this.campaignService.setMaleMessage(currentMale + emoji);
    this.campaignService.setFemaleMessage(currentFemale + emoji);
  }

  // Sample preview messages
  readonly sampleMalePreview = `السلام عليكم، نتمنى لكم رمضان كريم 🌙❤️
دي فرصة حلوة اوي عشان نشكر كل
دلوقتي شركة هايد بارك-Hyde park دازلة بـ مشروع جديد في التجمع السادس 🏠 المشروع ده هنعمل فلل و اراضي سكني و دي اول مرة هيتزلوا فيها بأراضي
و دلوقتي هما بيجمعوا اشتراكات و ناريين بأسعار حلوة جداً`;

  readonly sampleFemalePreview = `السلام عليكم، نتمنى لكِ رمضان كريم 🌙❤️
دي فرصة حلوة اوي عشان نشكر كل
دلوقتي شركة هايد بارك-Hyde park دازلة بـ مشروع جديد في التجمع السادس 🏠 المشروع ده هنعمل فلل و اراضي سكني و دي اول مرة هيتزلوا فيها بأراضي
و دلوقتي هما بيجمعوا اشتراكات و ناريين بأسعار حلوة جداً`;

  // Sample data for preview placeholders
  readonly samplePreviewData: Record<string, string> = {
    'arabic_name': 'أحمد محمد',
    'english_name': 'Ahmed Mohamed'
  };

  // Computed preview with placeholders replaced
  readonly malePreviewProcessed = computed(() => {
    const message = this.maleMessage();
    return message ? this.processMessageForPreview(message) : this.sampleMalePreview;
  });

  readonly femalePreviewProcessed = computed(() => {
    const message = this.femaleMessage();
    return message ? this.processMessageForPreview(message) : this.sampleFemalePreview;
  });

  // Process message to replace placeholders with sample data
  private processMessageForPreview(message: string): string {
    let processed = message;

    // Replace known placeholders with sample data
    processed = processed.replace(/\{arabic_name\}/g, this.samplePreviewData['arabic_name']);
    processed = processed.replace(/\{english_name\}/g, this.samplePreviewData['english_name']);

    // Replace other placeholders like {option1-option2-option3} with the first option
    processed = processed.replace(/\{([^}]+)\}/g, (match, content) => {
      // Check if it's a multi-option placeholder (contains -)
      if (content.includes('-')) {
        const options = content.split('-');
        // Show first option in preview
        return options[0].trim();
      }
      // Return original if not a known pattern
      return match;
    });

    return processed;
  }

  onMaleMessageChange(value: string): void {
    this.campaignService.setMaleMessage(value);
  }

  onFemaleMessageChange(value: string): void {
    this.campaignService.setFemaleMessage(value);
  }

  clearTemplateInfo(): void {
    this.campaignService.clearTemplateInfo();
  }

  togglePreview(): void {
    this.isPreviewExpanded = !this.isPreviewExpanded;
  }

  onTemplateTitleChange(value: string): void {
    this.campaignService.setTemplateName(value);
  }

  onTemplateDescriptionChange(value: string): void {
    this.campaignService.setTemplateDescription(value);
  }

  // Cancel editing
  cancelEdit(): void {
    this.campaignService.clearTemplateInfo();
    this.campaignService.setMaleMessage('');
    this.campaignService.setFemaleMessage('');
  }

  // Check if currently editing
  isEditing(): boolean {
    return this.editingTemplateId() !== null;
  }

  saveAsTemplate(): void {
    const title = this.templateName().trim();
    const description = this.templateDescription().trim();
    const maleMsg = this.maleMessage();
    const femaleMsg = this.femaleMessage();

    if (!title) {
      this.toastService.warning('Please enter a template title');
      return;
    }

    if (!maleMsg && !femaleMsg) {
      this.toastService.warning('Please enter at least one message');
      return;
    }

    this.isSavingTemplate.set(true);

    const editId = this.editingTemplateId();

    if (editId) {
      // Update existing template
      const request: UpdateCampaignTemplateRequest = {
        name: title,
        description: description,
        content: maleMsg || femaleMsg || '',
        maleContent: maleMsg || '',
        femaleContent: femaleMsg || maleMsg || ''
      };

      this.templatesService.updateTemplate(editId, request).subscribe({
        next: () => {
          this.toastService.success(`Template "${title}" updated successfully`);
          this.campaignService.clearTemplateInfo();
          this.isSavingTemplate.set(false);
        },
        error: (error) => {
          const errorMessage = error.error?.message || error.message || 'Failed to update template';
          this.toastService.error(errorMessage);
          this.isSavingTemplate.set(false);
        }
      });
    } else {
      // Create new template
      const request: CreateCampaignTemplateRequest = {
        name: title,
        description: description,
        content: maleMsg || femaleMsg || '',
        maleContent: maleMsg || '',
        femaleContent: femaleMsg || maleMsg || ''
      };

      this.templatesService.createTemplate(request).subscribe({
        next: () => {
          this.toastService.success(`Template "${title}" saved successfully`);
          this.campaignService.setTemplateName('');
          this.campaignService.setTemplateDescription('');
          this.isSavingTemplate.set(false);
        },
        error: (error) => {
          const errorMessage = error.error?.message || error.message || 'Failed to save template';
          this.toastService.error(errorMessage);
          this.isSavingTemplate.set(false);
        }
      });
    }
  }

  onFileSelect(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (input.files) {
      Array.from(input.files).forEach(file => {
        this.readFileAsBase64(file);
      });
    }
    // Reset input so same file can be selected again
    input.value = '';
  }

  private readFileAsBase64(file: File): void {
    const reader = new FileReader();
    reader.onload = () => {
      const result = reader.result as string;
      // Remove the data:mime;base64, prefix to get pure base64
      const base64 = result.split(',')[1];

      this.campaignService.addAttachment({
        name: file.name,
        size: file.size,
        type: file.type,
        base64: base64
      });
    };
    reader.onerror = () => {
      console.error('Error reading file:', file.name);
    };
    reader.readAsDataURL(file);
  }

  removeAttachment(id: number): void {
    this.campaignService.removeAttachment(id);
  }

  formatFileSize(bytes: number): string {
    if (bytes === 0) return '0 Bytes';
    const k = 1024;
    const sizes = ['Bytes', 'KB', 'MB', 'GB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + ' ' + sizes[i];
  }

  // Track focus on male textarea
  onMaleTextareaFocus(event: FocusEvent): void {
    this.lastFocusedTextarea = 'male';
  }

  // Track focus on female textarea
  onFemaleTextareaFocus(event: FocusEvent): void {
    this.lastFocusedTextarea = 'female';
  }

  // Save cursor position when textarea loses focus or on input
  saveCursorPosition(type: 'male' | 'female'): void {
    const textarea = type === 'male' ? this.maleTextarea : this.femaleTextarea;
    if (textarea?.nativeElement) {
      this.lastCursorPosition = textarea.nativeElement.selectionStart || 0;
      this.lastFocusedTextarea = type;
    }
  }

  insertVariable(variable: string): void {
    const variableMap: Record<string, string> = {
      'arabicName': '{arabic_name}',
      'englishName': '{english_name}'
    };
    const placeholder = variableMap[variable] || `{${variable}}`;

    // Get the textarea element and current message based on last focused
    const textarea = this.lastFocusedTextarea === 'male' ? this.maleTextarea : this.femaleTextarea;
    const currentMessage = this.lastFocusedTextarea === 'male' ? this.maleMessage() : this.femaleMessage();
    const setMessage = this.lastFocusedTextarea === 'male'
      ? (msg: string) => this.campaignService.setMaleMessage(msg)
      : (msg: string) => this.campaignService.setFemaleMessage(msg);

    if (textarea?.nativeElement) {
      const el = textarea.nativeElement;
      const start = el.selectionStart || this.lastCursorPosition || currentMessage.length;
      const end = el.selectionEnd || start;

      // Insert placeholder at cursor position
      const newMessage = currentMessage.substring(0, start) + placeholder + currentMessage.substring(end);
      setMessage(newMessage);

      // Restore focus and set cursor position after the inserted text
      setTimeout(() => {
        el.focus();
        const newCursorPos = start + placeholder.length;
        el.setSelectionRange(newCursorPos, newCursorPos);
      }, 0);
    } else {
      // Fallback: append to end
      setMessage(currentMessage + placeholder);
    }
  }
}
