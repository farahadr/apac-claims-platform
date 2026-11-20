import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { ClaimDetails, ClaimType, ClaimsApi, errorMessages } from '../claims-api';

@Component({
  selector: 'app-submit-claim',
  imports: [ReactiveFormsModule],
  templateUrl: './submit-claim.html',
})
export class SubmitClaim {
  private readonly api = inject(ClaimsApi);

  // Today in UTC ("2026-11-20"), the same "today" the API uses. Stops future dates in the picker.
  protected readonly today = new Date().toISOString().slice(0, 10);

  // One control per form field. The API does the real validation.
  protected readonly form = new FormGroup({
    type: new FormControl<ClaimType>('Motor', { nonNullable: true }),
    market: new FormControl('', { nonNullable: true }),
    description: new FormControl('', { nonNullable: true }),
    estimatedAmount: new FormControl<number | null>(null),
    incidentDate: new FormControl('', { nonNullable: true }),
    claimant: new FormControl('', { nonNullable: true }),
  });

  // Page state. A signal is a value that updates the page when it changes.
  protected readonly saving = signal(false);
  protected readonly errors = signal<string[]>([]);
  protected readonly created = signal<ClaimDetails | null>(null);

  protected submit() {
    this.saving.set(true);
    this.errors.set([]);

    const values = this.form.getRawValue();
    this.api
      .createClaim({ ...values, incidentDate: values.incidentDate || null })
      .subscribe({
        next: (claim) => {
          this.created.set(claim);
          this.saving.set(false);
        },
        error: (err: HttpErrorResponse) => {
          this.errors.set(errorMessages(err));
          this.saving.set(false);
        },
      });
  }

  protected submitAnother() {
    this.form.reset();
    this.created.set(null);
  }
}
