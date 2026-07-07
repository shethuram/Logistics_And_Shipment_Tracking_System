import { TestBed } from '@angular/core/testing';

import { PublicTracking } from './public-tracking';

describe('PublicTracking', () => {
  let service: PublicTracking;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(PublicTracking);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
