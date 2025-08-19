import { animate, state, style, transition, trigger } from '@angular/animations';
import { CommonModule } from '@angular/common';
import { Component, HostListener, OnDestroy, OnInit } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import { SidebarService } from '../../core/services/sidebar.service';
import { ThemeService } from '../../core/services/theme.service';
import { FooterComponent } from '../footer/footer.component';
import { HeaderComponent } from '../header/header.component';
import { SidebarComponent } from '../sidebar/sidebar.component';

@Component({
  selector: 'app-main-layout',
  standalone: true,
  imports: [CommonModule, RouterOutlet, HeaderComponent, SidebarComponent, FooterComponent],
  templateUrl: './main-layout.component.html',
  styleUrls: ['./main-layout.component.scss'],
  animations: [
    trigger('fadeAnimation', [
      state('in', style({ opacity: 1 })),
      transition(':enter', [
        style({ opacity: 0 }),
        animate('200ms ease-in')
      ]),
      transition(':leave', [
        animate('200ms ease-out', style({ opacity: 0 }))
      ])
    ])
  ]
})
export class MainLayoutComponent implements OnInit, OnDestroy {
  private destroy$ = new Subject<void>();
  private boundHandleSidebarToggle: EventListener;
  isMobile = false;

  constructor(
    public sidebarService: SidebarService,
    private themeService: ThemeService
  ) {
    // Bind the event handler once
    this.boundHandleSidebarToggle = this.handleSidebarToggle.bind(this) as EventListener;
  }

  ngOnInit(): void {
    this.checkScreenSize();
    
    // Initialize theme service (this will apply the stored theme)
    // The theme service automatically initializes in its constructor
    
    // Subscribe to sidebar state changes
    this.sidebarService.state$
      .pipe(takeUntil(this.destroy$))
      .subscribe(state => {
        this.isMobile = state.isMobile;
        
        // Update body classes for global styling
        if (this.isMobile && !state.isCollapsed) {
          document.body.classList.add('sidebar-open');
        } else {
          document.body.classList.remove('sidebar-open');
        }
        
        // Update main layout classes for smoother transitions
        this.updateLayoutClasses(state.isCollapsed);
      });
      
    // Listen for custom sidebar toggle events for enhanced synchronization
    window.addEventListener('sidebarToggle', this.boundHandleSidebarToggle);
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
    document.body.classList.remove('sidebar-open', 'sidebar-collapsed', 'sidebar-expanded', 'sidebar-mobile');
    
    // Clean up event listener
    window.removeEventListener('sidebarToggle', this.boundHandleSidebarToggle);
  }

  @HostListener('window:resize', ['$event'])
  onResize(): void {
    this.checkScreenSize();
  }

  private checkScreenSize(): void {
    const wasWobile = this.isMobile;
    this.isMobile = window.innerWidth < 768;
    
    // The sidebar service now handles responsive behavior automatically
    // This is kept for any component-specific responsive logic
    if (this.isMobile !== wasWobile) {
      // Component-specific mobile/desktop transition logic can go here
    }
  }

  // Enhanced synchronization methods
  private updateLayoutClasses(isCollapsed: boolean): void {
    const layoutElement = document.querySelector('.layout');
    if (layoutElement) {
      if (isCollapsed) {
        layoutElement.classList.add('sidebar-collapsed');
      } else {
        layoutElement.classList.remove('sidebar-collapsed');
      }
    }
  }

  private handleSidebarToggle(event: Event): void {
    // Handle custom sidebar toggle events for enhanced synchronization
    const customEvent = event as CustomEvent;
    const { isCollapsed, timestamp } = customEvent.detail;
    console.log(`Sidebar toggled: ${isCollapsed ? 'collapsed' : 'expanded'} at ${new Date(timestamp).toLocaleTimeString()}`);
    
    // Add any additional synchronization logic here
    // For example, you could trigger analytics, save preferences, etc.
  }
}
