export interface InitiatePaymentRequest {
  shipmentId: string;
}

export interface InitiatePaymentResponse {
  razorpayOrderId: string;
  amount: number;
  currency: string;
}

export interface PaymentStatusResponse {
  shipmentId: string;
  paymentStatus: string;
}
