import { DatePipe, DecimalPipe } from '@angular/common';
import { Component, inject } from '@angular/core';
import { ClaimsApi } from '../claims-api';

@Component({
  selector: 'app-officer',
  imports: [DecimalPipe, DatePipe],
  templateUrl: './officer.html',
})
export class Officer {
  private readonly api = inject(ClaimsApi);

  // Both start loading as soon as the page opens.
  protected readonly summary = this.api.dashboardSummaryResource();
  protected readonly claims = this.api.claimsResource();

  protected refresh() {
    this.summary.reload();
    this.claims.reload();
  }
}
