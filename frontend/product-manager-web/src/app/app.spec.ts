import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';

import { App } from './app';
import { LoadingService } from './core/services/loading.service';

describe('App', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [App],
      providers: [provideRouter([]), provideHttpClient(), provideHttpClientTesting()],
    }).compileComponents();
  });

  it('shows the app name in the toolbar', async () => {
    const fixture = TestBed.createComponent(App);
    await fixture.whenStable();

    const element = fixture.nativeElement as HTMLElement;
    expect(element.textContent).toContain('Product Manager');
  });

  it('shows the progress bar only while loading', async () => {
    const fixture = TestBed.createComponent(App);
    const loading = TestBed.inject(LoadingService);

    await fixture.whenStable();
    const element = fixture.nativeElement as HTMLElement;
    expect(element.querySelector('mat-progress-bar')).toBeNull();

    loading.start();
    await fixture.whenStable();
    expect(element.querySelector('mat-progress-bar')).not.toBeNull();

    loading.stop();
    await fixture.whenStable();
    expect(element.querySelector('mat-progress-bar')).toBeNull();
  });
});
