import { animate, state, style, transition, trigger } from '@angular/animations';
import { CommonModule } from '@angular/common';
import { Component, HostListener, OnDestroy, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { Subject } from 'rxjs';
import { AuthService } from '../../core/auth/auth.service';
import { SidebarService } from '../../core/services/sidebar.service';
import { ThemeService } from '../../core/services/theme.service';
import { ThemeToggleComponent } from '../../shared/components/theme-toggle/theme-toggle.component';

@Component({
  selector: 'app-header',
  standalone: true,
  imports: [CommonModule, RouterLink, FormsModule, ThemeToggleComponent],
  templateUrl: './header.component.html',
  styleUrls: ['./header.component.scss'],
  animations: [
    trigger('fadeInOut', [
      state('in', style({ opacity: 1, transform: 'translateY(0)' })),
      transition(':enter', [
        style({ opacity: 0, transform: 'translateY(-10px)' }),
        animate('200ms ease-out')
      ]),
      transition(':leave', [
        animate('150ms ease-in', style({ opacity: 0, transform: 'translateY(-10px)' }))
      ])
    ])
  ]
})
export class HeaderComponent implements OnInit, OnDestroy {
  private destroy$ = new Subject<void>();
  searchQuery = '';
  showUserMenu = false;
  notificationCount = 0; // This would normally come from a service

  constructor(
    public auth: AuthService,
    private router: Router,
    public sidebarService: SidebarService,
    public themeService: ThemeService
  ) {}

  ngOnInit(): void {
    // Listen for clicks outside user menu to close it
    this.setupClickOutsideHandler();

    // Mock notification count - replace with actual service call
    this.notificationCount = Math.floor(Math.random() * 10);
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  onSearch(event: Event): void {
    const target = event.target as HTMLInputElement;
    this.searchQuery = target.value;

    // Debounced search logic would go here
    if (this.searchQuery.length > 2) {
      console.log('Searching for:', this.searchQuery);
      // TODO: Implement actual search functionality
    }
  }

  onSearchSubmit(): void {
    if (this.searchQuery.trim()) {
      console.log('Search submitted:', this.searchQuery);
      // TODO: Navigate to search results or emit search event
      // this.router.navigate(['/search'], { queryParams: { q: this.searchQuery } });
    }
  }

  clearSearch(): void {
    this.searchQuery = '';
    // TODO: Clear search results or emit clear event
    console.log('Search cleared');
  }

  toggleUserMenu(): void {
    this.showUserMenu = !this.showUserMenu;
  }

  getInitials(username: string): string {
    if (!username) return 'U';

    const parts = username.split(' ');
    if (parts.length >= 2) {
      return (parts[0][0] + parts[1][0]).toUpperCase();
    }
    return username.substring(0, 2).toUpperCase();
  }

  logout(): void {
    this.showUserMenu = false;
    this.auth.logout();
    this.router.navigate(['/login']);
  }

  private setupClickOutsideHandler(): void {
    // This would be better implemented with a directive, but for simplicity:
    document.addEventListener('click', (event) => {
      const target = event.target as HTMLElement;
      const userSection = document.querySelector('.user-section');

      if (this.showUserMenu && userSection && !userSection.contains(target)) {
        this.showUserMenu = false;
      }
    });
  }

  @HostListener('window:keydown.escape')
  onEscapeKey(): void {
    if (this.showUserMenu) {
      this.showUserMenu = false;
    }
  }
}
