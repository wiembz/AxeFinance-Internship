import { CommonModule } from '@angular/common';
import { Component, Input, OnDestroy, OnInit } from '@angular/core';
import { Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import { Theme, ThemeService, ThemeState } from '../../../core/services/theme.service';

@Component({
  selector: 'app-theme-toggle',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="theme-toggle" [class.compact]="compact">
      <!-- Compact Mode: Single Toggle Button -->
      <button 
        *ngIf="compact; else fullMode"
        type="button"
        class="theme-btn compact-btn"
        (click)="themeService.toggleTheme()"
        [attr.aria-label]="'Switch to next theme mode. Current: ' + themeService.getThemeLabel()"
        [attr.title]="'Current: ' + themeService.getThemeLabel() + '. Click to cycle themes.'">
        
        <svg class="theme-icon" width="20" height="20" viewBox="0 0 24 24" fill="currentColor">
          <!-- Sun Icon for Light Mode -->
          <g *ngIf="themeState.theme === 'light'" class="sun-icon">
            <circle cx="12" cy="12" r="4"/>
            <path d="m12 2v2m0 16v2M4.93 4.93l1.41 1.41m11.32 11.32l1.41 1.41M2 12h2m16 0h2M6.34 6.34l1.41-1.41M16.24 16.24l1.41-1.41"/>
          </g>
          
          <!-- Moon Icon for Dark Mode -->
          <path *ngIf="themeState.theme === 'dark'" class="moon-icon" d="M21 12.79A9 9 0 1 1 11.21 3 7 7 0 0 0 21 12.79z"/>
          
          <!-- Auto Icon for System Mode -->
          <g *ngIf="themeState.theme === 'auto'" class="auto-icon">
            <circle cx="12" cy="12" r="3" opacity="0.5"/>
            <path d="M12 2v2m0 16v2M4.93 4.93l1.41 1.41m11.32 11.32l1.41 1.41M2 12h2m16 0h2M6.34 6.34l1.41-1.41M16.24 16.24l1.41-1.41" opacity="0.7"/>
            <rect x="10" y="6" width="4" height="2" rx="1"/>
            <rect x="10" y="16" width="4" height="2" rx="1"/>
          </g>
        </svg>
        
        <span class="theme-label" *ngIf="showLabel">
          {{ themeService.getThemeLabel() }}
        </span>
      </button>

      <!-- Full Mode: Radio Button Group -->
      <ng-template #fullMode>
        <div class="theme-options" role="radiogroup" aria-label="Theme selection">
          <button
            *ngFor="let option of themeOptions"
            type="button"
            class="theme-option"
            [class.active]="themeState.theme === option.value"
            (click)="setTheme(option.value)"
            [attr.aria-pressed]="themeState.theme === option.value"
            role="radio"
            [attr.aria-label]="option.label">
            
            <svg class="option-icon" width="16" height="16" viewBox="0 0 24 24" fill="currentColor">
              <g [ngSwitch]="option.value">
                <!-- Sun Icon -->
                <g *ngSwitchCase="'light'">
                  <circle cx="12" cy="12" r="4"/>
                  <path d="m12 2v2m0 16v2M4.93 4.93l1.41 1.41m11.32 11.32l1.41 1.41M2 12h2m16 0h2M6.34 6.34l1.41-1.41M16.24 16.24l1.41-1.41"/>
                </g>
                
                <!-- Moon Icon -->
                <path *ngSwitchCase="'dark'" d="M21 12.79A9 9 0 1 1 11.21 3 7 7 0 0 0 21 12.79z"/>
                
                <!-- Auto Icon -->
                <g *ngSwitchCase="'auto'">
                  <circle cx="12" cy="12" r="3" opacity="0.5"/>
                  <path d="M12 2v2m0 16v2M4.93 4.93l1.41 1.41m11.32 11.32l1.41 1.41M2 12h2m16 0h2M6.34 6.34l1.41-1.41M16.24 16.24l1.41-1.41" opacity="0.3"/>
                  <rect x="10" y="6" width="4" height="2" rx="1"/>
                </g>
              </g>
            </svg>
            
            <span class="option-label">{{ option.label }}</span>
            
            <!-- Active Indicator -->
            <div class="active-indicator" *ngIf="themeState.theme === option.value"></div>
          </button>
        </div>
        
        <!-- Current System Preference Indicator -->
        <div class="system-info" *ngIf="themeState.theme === 'auto'">
          <span class="system-label">
            System: {{ themeState.isSystemDark ? 'Dark' : 'Light' }}
          </span>
        </div>
      </ng-template>
    </div>
  `,
  styleUrls: ['./theme-toggle.component.scss']
})
export class ThemeToggleComponent implements OnInit, OnDestroy {
  @Input() compact = false;
  @Input() showLabel = false;
  
  private destroy$ = new Subject<void>();
  themeState: ThemeState = { theme: 'auto', isDark: false, isSystemDark: false };

  themeOptions = [
    { value: 'light' as Theme, label: 'Light', icon: 'sun' },
    { value: 'dark' as Theme, label: 'Dark', icon: 'moon' },
    { value: 'auto' as Theme, label: 'Auto', icon: 'auto' }
  ];

  constructor(public themeService: ThemeService) {}

  ngOnInit(): void {
    this.themeService.theme$
      .pipe(takeUntil(this.destroy$))
      .subscribe(state => {
        this.themeState = state;
      });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  setTheme(theme: Theme): void {
    this.themeService.setTheme(theme);
  }
}
