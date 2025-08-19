import { CommonModule } from '@angular/common';
import { Component, OnDestroy, OnInit } from '@angular/core';
import { Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import { ThemeService } from '../../core/services/theme.service';

interface SystemStatus {
  api: boolean;
  database: boolean;
  auth: boolean;
}

interface BuildInfo {
  version: string;
  date: string;
  commit?: string;
}

@Component({
  selector: 'app-footer',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './footer.component.html',
  styleUrls: ['./footer.component.scss']
})
export class FooterComponent implements OnInit, OnDestroy {
  private destroy$ = new Subject<void>();
  currentYear = new Date().getFullYear();
  totalProblems = 0;
  activeDepartments = 0;
  isDarkMode = false;

  systemStatus: SystemStatus = {
    api: true,
    database: true,
    auth: true
  };

  buildInfo: BuildInfo = {
    version: '1',
    date: new Date().toLocaleDateString('en-US', {
      year: 'numeric',
      month: 'short',
      day: 'numeric'
    })
  };

  constructor(private themeService: ThemeService) {}

  ngOnInit(): void {
    this.loadStatistics();
    this.checkSystemStatus();

    // Subscribe to theme changes
    this.themeService.theme$
      .pipe(takeUntil(this.destroy$))
      .subscribe(themeState => {
        this.isDarkMode = themeState.isDark;
      });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  private loadStatistics(): void {
    // Mock data - replace with actual service calls
    this.totalProblems = Math.floor(Math.random() * 1000) + 500;
    this.activeDepartments = Math.floor(Math.random() * 20) + 5;
  }

  private checkSystemStatus(): void {
    // Mock status check - replace with actual health checks
    this.systemStatus = {
      api: Math.random() > 0.1, // 90% uptime simulation
      database: Math.random() > 0.05, // 95% uptime simulation
      auth: Math.random() > 0.02 // 98% uptime simulation
    };
  }
}
