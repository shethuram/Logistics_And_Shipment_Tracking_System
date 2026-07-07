export interface MetadataOptionDto {
  value: string;
  label: string;
}

export interface FormMetadataDto {
  packageTypes: MetadataOptionDto[];
  preferredWindows: MetadataOptionDto[];
  paymentMethods: MetadataOptionDto[];
  vehicleTypes: MetadataOptionDto[];
  shipmentStatuses: MetadataOptionDto[];
}
