import { Injectable, computed, signal } from '@angular/core';

/** Tracks how many HTTP requests are running, so the toolbar can show a progress bar. */
@Injectable({ providedIn: 'root' })
export class LoadingService {
  private readonly inFlight = signal(0);
  readonly isLoading = computed(() => this.inFlight() > 0);

  start(): void {
    this.inFlight.update((count) => count + 1);
  }

  stop(): void {
    this.inFlight.update((count) => Math.max(0, count - 1));
  }
}
