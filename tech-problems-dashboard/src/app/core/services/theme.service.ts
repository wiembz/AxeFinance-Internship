import { Injectable } from '@angular/core';
import { BehaviorSubject } from 'rxjs';

export type Theme = 'light' | 'dark' | 'auto';

export interface ThemeState {
  theme: Theme;
  isDark: boolean;
  isSystemDark: boolean;
}

@Injectable({
  providedIn: 'root'
})
export class ThemeService {
  private readonly STORAGE_KEY = 'theme-preference';
  private readonly DARK_MODE_CLASS = 'dark-theme';
  private readonly LIGHT_MODE_CLASS = 'light-theme';

  private themeSubject = new BehaviorSubject<ThemeState>({
    theme: this.getStoredTheme(),
    isDark: false,
    isSystemDark: false
  });

  public theme$ = this.themeSubject.asObservable();

  private mediaQuery = window.matchMedia('(prefers-color-scheme: dark)');

  constructor() {
    this.initializeTheme();
    this.setupSystemThemeListener();
  }

  get currentTheme(): Theme {
    return this.themeSubject.value.theme;
  }

  get isDarkMode(): boolean {
    return this.themeSubject.value.isDark;
  }

  get isLightMode(): boolean {
    return !this.themeSubject.value.isDark;
  }

  get themeState(): ThemeState {
    return this.themeSubject.value;
  }

  setTheme(theme: Theme): void {
    const isSystemDark = this.mediaQuery.matches;
    const isDark = theme === 'dark' || (theme === 'auto' && isSystemDark);

    const newState: ThemeState = {
      theme,
      isDark,
      isSystemDark
    };

    this.themeSubject.next(newState);
    this.applyTheme(newState);
    this.saveThemeToStorage(theme);
  }

  toggleTheme(): void {
    const currentTheme = this.currentTheme;

    if (currentTheme === 'auto') {
      // Auto -> Light -> Dark -> Auto
      this.setTheme('light');
    } else if (currentTheme === 'light') {
      this.setTheme('dark');
    } else {
      this.setTheme('auto');
    }
  }

  switchToLight(): void {
    this.setTheme('light');
  }

  switchToDark(): void {
    this.setTheme('dark');
  }

  switchToAuto(): void {
    this.setTheme('auto');
  }

  getThemeIcon(theme?: Theme): string {
    const currentTheme = theme || this.currentTheme;

    switch (currentTheme) {
      case 'light':
        return 'sun';
      case 'dark':
        return 'moon';
      case 'auto':
        return 'auto';
      default:
        return 'sun';
    }
  }

  getThemeLabel(theme?: Theme): string {
    const currentTheme = theme || this.currentTheme;

    switch (currentTheme) {
      case 'light':
        return 'Light Mode';
      case 'dark':
        return 'Dark Mode';
      case 'auto':
        return 'Auto (System)';
      default:
        return 'Light Mode';
    }
  }

  private initializeTheme(): void {
    const storedTheme = this.getStoredTheme();
    this.setTheme(storedTheme);
  }

  private setupSystemThemeListener(): void {
    this.mediaQuery.addEventListener('change', (e) => {
      const currentState = this.themeSubject.value;
      const newState: ThemeState = {
        ...currentState,
        isSystemDark: e.matches,
        isDark: currentState.theme === 'auto' ? e.matches : currentState.isDark
      };

      // Only update if theme is set to auto
      if (currentState.theme === 'auto') {
        this.themeSubject.next(newState);
        this.applyTheme(newState);
      } else {
        // Just update the system preference tracking
        this.themeSubject.next({ ...currentState, isSystemDark: e.matches });
      }
    });
  }

  private applyTheme(state: ThemeState): void {
    const body = document.body;
    const html = document.documentElement;

    // Remove existing theme classes
    body.classList.remove(this.DARK_MODE_CLASS, this.LIGHT_MODE_CLASS);
    html.classList.remove(this.DARK_MODE_CLASS, this.LIGHT_MODE_CLASS);

    // Add appropriate theme class
    const themeClass = state.isDark ? this.DARK_MODE_CLASS : this.LIGHT_MODE_CLASS;
    body.classList.add(themeClass);
    html.classList.add(themeClass);

    // Set data attributes for CSS selectors
    html.setAttribute('data-theme', state.theme);
    html.setAttribute('data-color-scheme', state.isDark ? 'dark' : 'light');

    // Update meta theme-color for mobile browsers
    this.updateMetaThemeColor(state.isDark);

    // Trigger theme change event for components that need to react
    this.triggerThemeChangeEvent(state);
  }

  private triggerThemeChangeEvent(state: ThemeState): void {
    // Dispatch custom event for components to listen to
    const themeChangeEvent = new CustomEvent('themeChange', {
      detail: { theme: state.theme, isDark: state.isDark }
    });
    window.dispatchEvent(themeChangeEvent);

    // Add transition class for smooth theme switching
    document.body.classList.add('theme-transitioning');
    setTimeout(() => {
      document.body.classList.remove('theme-transitioning');
    }, 300);
  }

  private updateMetaThemeColor(isDark: boolean): void {
    let metaThemeColor = document.querySelector('meta[name="theme-color"]');

    if (!metaThemeColor) {
      metaThemeColor = document.createElement('meta');
      metaThemeColor.setAttribute('name', 'theme-color');
      document.head.appendChild(metaThemeColor);
    }

    // Set theme color based on mode
    const themeColor = isDark ? '#1a1a1a' : '#ffffff';
    metaThemeColor.setAttribute('content', themeColor);
  }

  private getStoredTheme(): Theme {
    try {
      const stored = localStorage.getItem(this.STORAGE_KEY) as Theme;
      return stored && ['light', 'dark', 'auto'].includes(stored) ? stored : 'auto';
    } catch {
      return 'auto';
    }
  }

  private saveThemeToStorage(theme: Theme): void {
    try {
      localStorage.setItem(this.STORAGE_KEY, theme);
    } catch {
      console.warn('Failed to save theme preference to localStorage');
    }
  }

  // Utility method to check if a specific theme is active
  isThemeActive(theme: Theme): boolean {
    return this.currentTheme === theme;
  }

  // Method to get the effective theme (resolves 'auto' to actual light/dark)
  getEffectiveTheme(): 'light' | 'dark' {
    const state = this.themeSubject.value;
    return state.isDark ? 'dark' : 'light';
  }

  // Method to get theme-specific CSS variables
  getThemeVariables(): Record<string, string> {
    const isDark = this.isDarkMode;

    if (isDark) {
      return {
        '--bg-surface': '#1a1a1a',
        '--bg-light': '#2d2d2d',
        '--bg-lighter': '#3d3d3d',
        '--text-primary': '#ffffff',
        '--text-secondary': '#e5e5e5',
        '--border-light': '#404040',
        '--border-medium': '#525252'
      };
    } else {
      return {
        '--bg-surface': '#ffffff',
        '--bg-light': '#f8f9fa',
        '--bg-lighter': '#fafbfc',
        '--text-primary': '#1a202c',
        '--text-secondary': '#2d3748',
        '--border-light': '#e2e8f0',
        '--border-medium': '#cbd5e0'
      };
    }
  }
}
