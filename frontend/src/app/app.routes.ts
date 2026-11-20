import { Routes } from '@angular/router';
import { Officer } from './officer/officer';
import { SubmitClaim } from './submit-claim/submit-claim';

// Which page to show for which URL.
export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'submit' },
  { path: 'submit', component: SubmitClaim, title: 'Submit a claim' },
  { path: 'officer', component: Officer, title: 'Claims officer' },
  { path: '**', redirectTo: 'submit' }, // unknown URL → back to the form
];
