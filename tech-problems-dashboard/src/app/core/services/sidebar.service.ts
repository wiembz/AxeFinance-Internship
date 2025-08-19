import { Injectable } from '@angular/core';
import { BehaviorSubject, fromEvent } from 'rxjs';
import { debounceTime, map, startWith } from 'rxjs/operators';

export interface SidebarState {
  isCollapsed: boolean;
  isMobile: boolean;
  width: number;
  collapsedWidth: number;
}

@Injectable({
  providedIn: 'root'
})
export class SidebarService {
  private readonly STORAGE_KEY = 'sidebar-state';
  private readonly MOBILE_BREAKPOINT = 768;
  private readonly EXPANDED_WIDTH = 280;
  private readonly COLLAPSED_WIDTH = 70;

  private stateSubject = new BehaviorSubject<SidebarState>({
    isCollapsed: this.getStoredState(),
    isMobile: this.checkMobileDevice(),
    width: this.getStoredState() ? this.COLLAPSED_WIDTH : this.EXPANDED_WIDTH,
    collapsedWidth: this.COLLAPSED_WIDTH
  });

  public state$ = this.stateSubject.asObservable();
  public isCollapsed$ = this.state$.pipe(map(state => state.isCollapsed));
  public isMobile$ = this.state$.pipe(map(state => state.isMobile));

  // Window resize observable for responsive behavior
  private resize$ = fromEvent(window, 'resize').pipe(
    debounceTime(100),
    map(() => window.innerWidth < this.MOBILE_BREAKPOINT),
    startWith(this.checkMobileDevice())
  );

  constructor() {
    this.setupResizeListener();
    this.initializeState();
  }

  get isCollapsed(): boolean {
    return this.stateSubject.value.isCollapsed;
  }

  get isMobile(): boolean {
    return this.stateSubject.value.isMobile;
  }

  get currentWidth(): number {
    return this.stateSubject.value.width;
  }

  get state(): SidebarState {
    return this.stateSubject.value;
  }

  toggle(): void {
    const currentState = this.stateSubject.value;

    const newState = {
      ...currentState,
      isCollapsed: !currentState.isCollapsed,
      width: !currentState.isCollapsed ? this.COLLAPSED_WIDTH : this.EXPANDED_WIDTH
    };

    this.updateState(newState);
  }

  collapse(): void {
    const currentState = this.stateSubject.value;
    if (!currentState.isCollapsed) {
      this.updateState({
        ...currentState,
        isCollapsed: true,
        width: this.COLLAPSED_WIDTH
      });
    }
  }

  expand(): void {
    const currentState = this.stateSubject.value;
    if (currentState.isCollapsed) {
      this.updateState({
        ...currentState,
        isCollapsed: false,
        width: this.EXPANDED_WIDTH
      });
    }
  }

  // Force collapse on mobile devices
  forceCollapseOnMobile(): void {
    const currentState = this.stateSubject.value;
    if (currentState.isMobile && !currentState.isCollapsed) {
      this.collapse();
    }
  }

  // Auto expand on desktop if preference allows
  autoExpandOnDesktop(): void {
    const currentState = this.stateSubject.value;
    if (!currentState.isMobile && currentState.isCollapsed) {
      const storedPreference = this.getStoredState();
      if (!storedPreference) {
        this.expand();
      }
    }
  }

  // Reset to default state
  reset(): void {
    this.updateState({
      isCollapsed: false,
      isMobile: this.checkMobileDevice(),
      width: this.EXPANDED_WIDTH,
      collapsedWidth: this.COLLAPSED_WIDTH
    });
  }

  private updateState(newState: SidebarState): void {
    this.stateSubject.next(newState);
    this.saveStateToStorage(newState.isCollapsed);
    this.updateBodyClass(newState);
  }

  private initializeState(): void {
    const currentState = this.stateSubject.value;
    this.updateBodyClass(currentState);
  }

  private setupResizeListener(): void {
    this.resize$.subscribe(isMobile => {
      const currentState = this.stateSubject.value;

      if (isMobile !== currentState.isMobile) {
        const newState: SidebarState = {
          ...currentState,
          isMobile,
          // Auto-collapse on mobile, restore preference on desktop
          isCollapsed: isMobile ? true : this.getStoredState(),
          width: (isMobile || this.getStoredState()) ? this.COLLAPSED_WIDTH : this.EXPANDED_WIDTH
        };

        this.stateSubject.next(newState);
        this.updateBodyClass(newState);
      }
    });
  }

  private updateBodyClass(state: SidebarState): void {
    const body = document.body;

    // Remove existing classes
    body.classList.remove('sidebar-collapsed', 'sidebar-expanded', 'sidebar-mobile');

    // Add current state classes
    if (state.isCollapsed) {
      body.classList.add('sidebar-collapsed');
    } else {
      body.classList.add('sidebar-expanded');
    }

    if (state.isMobile) {
      body.classList.add('sidebar-mobile');
    }
  }

  private checkMobileDevice(): boolean {
    return window.innerWidth < this.MOBILE_BREAKPOINT;
  }

  private getStoredState(): boolean {
    try {
      const stored = localStorage.getItem(this.STORAGE_KEY);
      return stored ? JSON.parse(stored) : false;
    } catch {
      return false;
    }
  }

  private saveStateToStorage(isCollapsed: boolean): void {
    try {
      localStorage.setItem(this.STORAGE_KEY, JSON.stringify(isCollapsed));
    } catch {
      // Handle storage errors silently
      console.warn('Failed to save sidebar state to localStorage');
    }
  }
}
