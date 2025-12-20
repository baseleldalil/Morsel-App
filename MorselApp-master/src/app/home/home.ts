import { Component, inject } from '@angular/core';
import {
  ContactsPanelComponent,
  MessageComposerComponent,
  TemplatesPanelComponent,
  CampaignProgressComponent,
  AdvancedTimingComponent
} from '../components';
import { AuthService, LoaderService } from '../services';

@Component({
  selector: 'app-home',
  imports: [
    ContactsPanelComponent,
    MessageComposerComponent,
    TemplatesPanelComponent,
    CampaignProgressComponent,
    AdvancedTimingComponent
  ],
  templateUrl: './home.html',
  styleUrl: './home.css',
})
export class Home {
  private authService = inject(AuthService);
  loaderService = inject(LoaderService);

  readonly user = this.authService.user;

  logout() {
    this.authService.logout();
  }
}
