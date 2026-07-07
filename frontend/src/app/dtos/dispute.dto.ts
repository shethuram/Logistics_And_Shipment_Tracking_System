import { DisputeStatus } from '../models/enums';

export interface RaiseDisputeRequest {
  shipmentId: string;
  complaintText: string;
}

export interface RaiseDisputeResponse {
  id: string;
  shipmentId: string;
  status: DisputeStatus;
  createdAt: string;
}

export interface DisputeResponse {
  id: string;
  shipmentId: string;
  orderId: string;
  complaintText: string;
  status: DisputeStatus;
  resolutionNotes: string | null;
  resolvedAt: string | null;
  createdAt: string;
}
