import { CommonModule } from '@angular/common';
import { Component, OnDestroy, OnInit } from '@angular/core';
import { NavigationEnd, Router, RouterLink } from '@angular/router';
import { Subject } from 'rxjs';
import { filter, takeUntil } from 'rxjs/operators';
import { SidebarService } from '../../core/services/sidebar.service';
import { ThemeService } from '../../core/services/theme.service';

@Component({
  selector: 'app-sidebar',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './sidebar.component.html',
  styleUrls: ['./sidebar.component.scss']
})
export class SidebarComponent implements OnInit, OnDestroy {
  private destroy$ = new Subject<void>();
  isDarkMode = false;
  currentRoute = '';

  // Clean navigation items
  navigationItems = [
    {
      label: 'Dashboard',
      route: '/dashboard',
      icon: 'dashboard',
      badge: null as number | null,
      primary: false
    },
    {
      label: 'Departments',
      route: '/admin/departments',
      icon: 'departments',
      badge: null as number | null,
      primary: false
    },
    {
      label: 'All Projects',
      route: '/admin/projects',
      icon: 'projects',
      badge: null as number | null,
      primary: false
    },
    {
      label: 'All Problems',
      route: '/problems',
      icon: 'problems',
      badge: null as number | null,
      primary: false
    },
    {
      label: 'Submit Problem',
      route: '/problems/submit',
      icon: 'submit',
      badge: null as number | null,
      primary: true
    }

  ];

  constructor(
    public sidebarService: SidebarService,
    private themeService: ThemeService,
    public router: Router
  ) {}

  ngOnInit(): void {
    // Subscribe to theme changes
    this.themeService.theme$
      .pipe(takeUntil(this.destroy$))
      .subscribe(themeState => {
        this.isDarkMode = themeState.isDark;
      });

    // Track route changes
    this.router.events
      .pipe(
        filter(event => event instanceof NavigationEnd),
        takeUntil(this.destroy$)
      )
      .subscribe((event: NavigationEnd) => {
        this.currentRoute = event.url;
      });

    // Initialize current route
    this.currentRoute = this.router.url;
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  // Toggle sidebar
  onToggleSidebar(): void {
    this.sidebarService.toggle();
  }

  // Handle navigation item click
  onItemClick(item: any): void {
    console.log('Navigation clicked:', item.label);
  }

  // Check if route is active
  isRouteActive(route: string): boolean {
    return this.currentRoute === route || this.currentRoute.startsWith(route + '/');
  }

  // Get icon SVG path based on icon type
  getIconSvg(iconType: string): string {
    const icons: { [key: string]: string } = {
      dashboard: 'M3 13h8V3H3v10zm0 8h8v-6H3v6zm10 0h8V11h-8v10zm0-18v6h8V3h-8z',
      departments: 'M16 4c0-1.11.89-2 2-2s2 .89 2 2-.89 2-2 2-2-.89-2-2zm4 18v-6h2.5l-2.54-7.63A3 3 0 0 0 17.22 7H14.5c-1.17 0-2.24.7-2.74 1.76L9.46 16H12v6h8zM12.5 11.5c.83 0 1.5-.67 1.5-1.5s-.67-1.5-1.5-1.5S11 9.17 11 10s.67 1.5 1.5 1.5zM5.5 6c1.11 0 2-.89 2-2s-.89-2-2-2-2 .89-2 2 .89 2 2 2zm2 16v-6H10l-2.24-7.63A3 3 0 0 0 5.02 7H2.5c-1.17 0-2.24.7-2.74 1.76L2.04 16H4.5v6h3z',
      problems: 'M12 2C6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10S17.52 2 12 2zm-2 15l-5-5 1.41-1.41L10 14.17l7.59-7.59L19 8l-9 9z',
      submit: 'M19 13h-6v6h-2v-6H5v-2h6V5h2v6h6v2z',
      projects: 'M20 6h-2.18c.11-.31.18-.65.18-1a3 3 0 0 0-6 0c0 .35.07.69.18 1H4c-1.11 0-2 .89-2 2v11c0 1.11.89 2 2 2h16c1.11 0 2-.89 2-2V8c0-1.11-.89-2-2-2zm-5-2c.55 0 1 .45 1 1s-.45 1-1 1-1-.45-1-1 .45-1 1-1zM4 8h16v11H4V8z'
    };
    return icons[iconType] || icons['dashboard'];
  }
}
