import { TestBed } from '@angular/core/testing';

import { LoadingService } from './loading.service';

describe('LoadingService', () => {
  let service: LoadingService;

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [LoadingService] });
    service = TestBed.inject(LoadingService);
  });

  it('starts idle', () => {
    expect(service.isLoading()).toBe(false);
  });

  it('stays loading until all requests finish', () => {
    service.start();
    service.start();
    expect(service.isLoading()).toBe(true);

    service.stop();
    expect(service.isLoading()).toBe(true);

    service.stop();
    expect(service.isLoading()).toBe(false);
  });

  it('does not go below zero', () => {
    service.stop();

    service.start();
    expect(service.isLoading()).toBe(true);
  });
});
