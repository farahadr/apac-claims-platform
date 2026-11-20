import { HttpClient, HttpErrorResponse, httpResource } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { API_BASE_URL } from './config';

// ---- Shapes of the JSON the API sends and receives (match ClaimDtos.cs) ----

export type ClaimType = 'Motor' | 'Property';

export type ClaimStatus =
  | 'Submitted'
  | 'Assigned'
  | 'UnderReview'
  | 'InfoRequested'
  | 'Approved'
  | 'Rejected'
  | 'Settled';

export interface CreateClaimRequest {
  type: ClaimType;
  market: string;
  description: string;
  estimatedAmount: number | null;
  incidentDate: string | null; // "2026-11-15"
  claimant: string;
}

export interface ClaimSummary {
  id: string;
  type: ClaimType;
  market: string;
  estimatedAmount: number;
  status: ClaimStatus;
  claimant: string;
  assignedOfficer: string | null;
  incidentDate: string;
  createdAt: string;
}

export interface ClaimDetails extends ClaimSummary {
  description: string;
  updatedAt: string;
}

export interface DashboardSummary {
  openClaims: number;
  totalExposure: number;
  openClaimsPerOfficer: { officer: string; openClaims: number }[];
}

// ---- The calls to the API, in one place ----

@Injectable({ providedIn: 'root' })
export class ClaimsApi {
  private readonly http = inject(HttpClient);

  // POST: sent once when the form is submitted.
  createClaim(request: CreateClaimRequest) {
    return this.http.post<ClaimDetails>(`${API_BASE_URL}/api/claims`, request);
  }

  // GETs: httpResource loads the data and tracks loading/error for us.
  // Call these from a component's fields; .reload() fetches again.
  claimsResource() {
    return httpResource<ClaimSummary[]>(() => `${API_BASE_URL}/api/claims`);
  }

  dashboardSummaryResource() {
    return httpResource<DashboardSummary>(() => `${API_BASE_URL}/api/dashboard/summary`);
  }
}

// Turns an HTTP error into messages a person can read.
export function errorMessages(err: HttpErrorResponse): string[] {
  if (err.status === 0) {
    return [`Cannot reach the API at ${API_BASE_URL}. Is it running?`];
  }
  // 400 from our validation: { errors: { field: ["message", ...] } }
  if (err.status === 400 && err.error?.errors) {
    return Object.values(err.error.errors as Record<string, string[]>).flat();
  }
  // Other problems: { detail: "..." }
  if (err.error?.detail) {
    return [err.error.detail];
  }
  return [`Something went wrong (HTTP ${err.status}).`];
}
